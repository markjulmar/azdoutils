using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using Wit = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Julmar.AzDOUtilities
{
    partial class AzDOService
    {
        static readonly string[] RelationshipLinkText =
        {
            "System.LinkTypes.Dependency",
            "System.LinkTypes.Related",
            "System.LinkTypes.Hierarchy-Forward",
            "System.LinkTypes.Hierarchy-Reverse",
            "Microsoft.VSTS.Common.Affects-Forward",
            "Microsoft.VSTS.Common.Affects-Reverse",
            "System.LinkTypes.Duplicate-Forward",
            "System.LinkTypes.Duplicate-Reverse",
            "Microsoft.VSTS.TestCase.SharedParameterReferencedBy",
            "Microsoft.VSTS.Common.TestedBy-Forward",
            "Microsoft.VSTS.Common.TestedBy-Reverse",
            "Microsoft.VSTS.TestCase.SharedStepReferencedBy",
            "Microsoft.VSTS.Common.ProducedFor.Forward",
            "Microsoft.VSTS.Common.ConsumesFrom.Reverse",
            "System.LinkTypes.Remote.Related",
            "Hyperlink",
            "ArtifactLink",
            null,
            null
        };

        private readonly Lazy<VssCredentials> credentials;
        private readonly Lazy<WorkItemTrackingHttpClient> httpClient;
        private readonly Lazy<ProjectHttpClient> projectClient;
        internal TraceHelpers log;

        internal AzDOService(string uri, string accessToken)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentException("Missing access token", nameof(accessToken));
            }

            this.credentials = new Lazy<VssCredentials>(() => new VssBasicCredential(string.Empty, accessToken));
            this.httpClient = new Lazy<WorkItemTrackingHttpClient>(() => new WorkItemTrackingHttpClient(new Uri(uri), credentials.Value));
            this.projectClient = new Lazy<ProjectHttpClient>(() => new ProjectHttpClient(new Uri(uri), credentials.Value));
        }

        static Relationship FindRelationship(string linkType)
        {
            int pos = Array.IndexOf(RelationshipLinkText, linkType);
            return pos >= 0 ? (Relationship)pos : Relationship.Other;
        }

        static int? ParseIdFromRelationship(string url)
        {
            int? pos = url?.LastIndexOf("/", StringComparison.Ordinal);
            if(pos.HasValue && pos.Value >0)
            {
                string num = url.Substring(pos.Value + 1);
                if (int.TryParse(num, out int result))
                    return result;
            }
            return null;
        }

        private async Task<IEnumerable<Wit>> InternalGetWitsByIdChunked(List<int> ids, string[] fields, DateTime? asOf,
                                                    WorkItemExpand? expand, WorkItemErrorPolicy? errorPolicy, CancellationToken cancellationToken)
        {
            const int nSize = 150;
            var workItems = new List<Wit>();

            if (errorPolicy == null)
                errorPolicy = ErrorPolicy;

            // Chunk the retrieval.
            for (int i = 0; i < ids.Count; i += nSize)
            {
                var range = ids.GetRange(i, Math.Min(nSize, ids.Count - i));
                var results = await WorkItemClient.GetWorkItemsAsync(range, fields, asOf, expand, errorPolicy, userState: null, cancellationToken)
                                                  .ConfigureAwait(false);
                workItems.AddRange(results);
            }

            return workItems;
        }

        async Task<IReadOnlyList<RelationLinks>> InternalGetRelatedIdsAsync(int id, string relationshipType, DateTime? asOf, CancellationToken cancellationToken)
        {
            var list = new List<RelationLinks>();

            var workItem = await WorkItemClient.GetWorkItemAsync(id, null, asOf, WorkItemExpand.Relations, userState: null, cancellationToken)
                                               .ConfigureAwait(false);
            if (workItem.Relations != null)
            {
                list.AddRange(workItem.Relations
                                      .Where(r => relationshipType == null || r.Rel == relationshipType)
                                      .Select(r => new RelationLinks {
                                          Title = r.Title,
                                          Attributes = r.Attributes,
                                          Type = FindRelationship(r.Rel),
                                          RelatedId = ParseIdFromRelationship(r.Url),
                                          RawRelationshipType = r.Rel,
                                          Url = r.Url
                                      }));
            }

            return list;
        }

        private async Task<IEnumerable<Wit>> InternalGetWorkItemsAsync(string query, string[] fields, int? top, bool? timePrecision,
                                                        WorkItemExpand? expand, WorkItemErrorPolicy? errorPolicy,
                                                        CancellationToken cancellationToken)
        {
            log?.WriteLine(LogLevel.Query, query);

            Wiql wiql = new Wiql { Query = query };
            if (errorPolicy == null)
                errorPolicy = ErrorPolicy;

            // Return a list of URLs + Ids for matching workItems.
            WorkItemQueryResult queryResult = await WorkItemClient.QueryByWiqlAsync(wiql, timePrecision, top, userState: null, cancellationToken)
                                                          .ConfigureAwait(false);
            if (queryResult.WorkItems.Any())
            {
                var ids = queryResult.WorkItems.Select(wi => wi.Id).ToList();

                // Get the actual work items from the IDs; chunked.
                return await InternalGetWitsByIdChunked(ids, fields, queryResult.AsOf, expand, errorPolicy, cancellationToken)
                                 .ConfigureAwait(false);
            }

            return Enumerable.Empty<Wit>();
        }

        private async Task<Wit> AddAsync(JsonPatchDocument patchDocument, string projectName, string workItemType, bool? validateOnly, bool? bypassRules, CancellationToken cancellationToken)
        {
            if (this.ValidateOnly)
                validateOnly = true;

            log?.Dump(patchDocument);

            return await WorkItemClient.CreateWorkItemAsync(patchDocument, projectName, workItemType, validateOnly, bypassRules, userState: null, cancellationToken)
                               .ConfigureAwait(false);
        }

        private async Task<Wit> UpdateAsync(int id, JsonPatchDocument patchDocument, bool? validateOnly, bool? bypassRules,
                                            bool? supressNotifications, WorkItemExpand? expand, CancellationToken cancellationToken)
        {
            if (this.ValidateOnly)
                validateOnly = true;

            log?.Dump(patchDocument);

            return await WorkItemClient.UpdateWorkItemAsync(patchDocument, id, validateOnly, bypassRules, supressNotifications, expand, userState: null, cancellationToken)
                               .ConfigureAwait(false);
        }

        private async Task<QueryHierarchyItem> InternalGetStoredQueriesAsync(string projectName, QueryHierarchyItem parent, bool? includeDeleted, CancellationToken cancellationToken)
        {
            if (parent.Children == null)
            {
                parent = await WorkItemClient.GetQueryAsync(projectName, parent.Path, QueryExpand.All, depth: 2, includeDeleted, userState: null, cancellationToken);
            }

            for (int i = 0; i < parent.Children.Count; i++)
            {
                QueryHierarchyItem item = parent.Children[i];
                if (item.HasChildren == true)
                {
                    parent.Children[i] = await InternalGetStoredQueriesAsync(projectName, item, includeDeleted, cancellationToken);
                }
            }

            return parent;
        }

        private async Task InternalAddRelationshipAsync(string linkType, WorkItem owner, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken)
        {
            if (owner is null)
                throw new ArgumentNullException(nameof(owner));

            if (owner.IsNew)
                throw new ArgumentException("Cannot add related items to new WorkItem.", nameof(owner));

            if (relatedItems?.Any(wi => wi.IsNew) == true)
                throw new ArgumentException("Cannot add NEW related items to a WorkItem.", nameof(owner));

            var patchDocument = owner.CreatePatchDocument() ?? new JsonPatchDocument();
            foreach (var relatedItem in relatedItems)
            {
                patchDocument.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = linkType,
                        url = StripRevisionFromUrl(relatedItem.Url),
                        attributes = new { comment = $"Added related item {relatedItem.Id}" }
                    }
                });
            }

            var wit = await UpdateAsync(owner.Id.Value, patchDocument, this.ValidateOnly, bypassRules: true,
                                        supressNotifications: null, WorkItemExpand.Fields, cancellationToken)
                              .ConfigureAwait(false);
            log?.WriteLine(LogLevel.PatchDocument, $"WorkItem {owner.Id} updated from Rev {owner.Revision} to {wit.Rev}");
            owner.Initialize(wit);
        }

        private static string StripRevisionFromUrl(string url)
        {
            const string revisions = "/revisions/";
            if (url.Contains(revisions))
            {
                int index = url.LastIndexOf(revisions, StringComparison.Ordinal);
                url = url.Substring(0, index);
            }
            return url;
        }

        private async Task InternalRemoveRelationshipAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken)
        {
            if (owner is null)
                throw new ArgumentNullException(nameof(owner));
            if (owner.IsNew)
                throw new ArgumentException("Cannot remove related items from new WorkItem.", nameof(owner));
            if (relatedItems?.Any(wi => wi.IsNew) == true)
                throw new ArgumentException("Cannot remove NEW related items from a WorkItem.", nameof(owner));

            var operations = new List<JsonPatchOperation>();
            var items = (await InternalGetRelatedIdsAsync(owner.Id.Value, null, asOf: null, cancellationToken))
                                    .Where(rid => rid.RelatedId.HasValue).Select(rid => rid.RelatedId.Value).ToList();
            foreach (var item in relatedItems)
            {
                int index = items.IndexOf(item.Id.Value);
                if (index >= 0)
                {
                    operations.Add(new JsonPatchOperation
                    {
                        Operation = Operation.Remove,
                        Path = $"/relations/{index}"
                    });
                }
            }

            if (operations.Count > 0)
            {
                var patchDocument = new JsonPatchDocument();
                patchDocument.AddRange(operations);
                var wit = await UpdateAsync(owner.Id.Value, patchDocument, validateOnly: false, bypassRules: false,
                                  supressNotifications: null, WorkItemExpand.None, cancellationToken)
                                    .ConfigureAwait(false);
                log?.WriteLine(LogLevel.PatchDocument, $"WorkItem {owner.Id} updated from Rev {owner.Revision} to {wit.Rev}");
                owner.Initialize(wit);
            }
        }
    }
}
