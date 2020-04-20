using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using Wit = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace AzDOUtilities
{
    sealed partial class AzDOService : IAzureDevOpsService
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
            "System.LinkTypes.Duplicate-Reverse"
        };

        private readonly string uri;
        private readonly string accessToken;
        internal TraceHelpers log;
        
        public Action<string> TraceLog
        {
            get => log?.LogHandler;
            set
            {
                if (log == null && value != null)
                    log = new TraceHelpers { LogHandler = value };
                else if (log != null)
                    log.LogHandler = value;
            }
        }

        public bool ValidateOnly { get; set; }

        public LogLevel TraceLevel
        {
            get => log == null ? LogLevel.None : log.TraceLevel;
            set
            {
                if (log == null && value != LogLevel.None)
                    log = new TraceHelpers { TraceLevel = value };
                else if (log != null)
                    log.TraceLevel = value;
            }
        }

        internal AzDOService(string uri, string accessToken)
        {
            this.uri = uri ?? throw new ArgumentNullException(nameof(uri));
            this.accessToken = accessToken ?? throw new ArgumentException("Missing Access Token");
        }

        public async Task<WorkItem> GetAsync(int id)
        {
            using var mc = log?.Enter(id);
            using var client = CreateWorkItemClient();
            var wit = await client.GetWorkItemAsync(id, ReflectionHelpers.AllFields, expand: WorkItemExpand.All).ConfigureAwait(false);
            return ReflectionHelpers.MapWorkItemTypes(new[] { wit }).Single();
        }

        public Task AddChildrenAsync(WorkItem parent, params WorkItem[] children)
        {
            using var mc = log?.Enter(new object[] { $"parent:{parent.Id}", $"children:{string.Join(';', children?.Select(wi => wi.Id.ToString()))}" });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Child], parent, children);
        }

        public Task AddRelatedAsync(WorkItem owner, params WorkItem[] relatedItems)
        {
            using var mc = log?.Enter(new object[] { $"owner:{owner.Id}", $"relatedItems:{string.Join(';', relatedItems?.Select(wi => wi.Id.ToString()))}" });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Related], owner, relatedItems);
        }

        public Task AddRelationshipAsync(Relationship relationshipType, WorkItem owner, WorkItem[] relatedItems)
        {
            using var mc = log?.Enter(new object[] { $"owner:{owner.Id}", $"relatedItems:{string.Join(';', relatedItems?.Select(wi => wi.Id.ToString()))}" });

            int pos = (int)relationshipType;
            if (pos < 0 || pos >= RelationshipLinkText.Length)
                throw new ArgumentOutOfRangeException(nameof(relationshipType));

            return InternalAddRelationshipAsync(RelationshipLinkText[pos], owner, relatedItems);
        }

        private async Task InternalAddRelationshipAsync(string linkType, WorkItem owner, params WorkItem[] relatedItems)
        {
            if (owner is null)
                throw new ArgumentNullException(nameof(owner));

            if (owner.IsNew)
                throw new ArgumentException("Cannot add related items to new WorkItem.", nameof(owner));

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

            var wit = await UpdateAsync(owner.Id.Value, patchDocument, false, true)
                              .ConfigureAwait(false);
            owner.Initialize(wit);
        }

        public async Task<IEnumerable<WorkItem>> GetRelatedAsync(WorkItem owner)
        {
            if (owner is null)
                throw new ArgumentNullException(nameof(owner));

            if (owner.IsNew)
                throw new ArgumentException("No relationships on a new WorkItem");

            using var mc = log?.Enter(new object[] { owner.Id });

            var relatedIds = (await GetRelatedIdsAsync(owner.Id.Value).ConfigureAwait(false)).ToList();
            IEnumerable<Wit> wits;
            using (var client = CreateWorkItemClient())
            {
                wits = await GetWorkItemsAsync(client, relatedIds, ReflectionHelpers.AllFields, null).ConfigureAwait(false);
            }

            return ReflectionHelpers.MapWorkItemTypes(wits);
        }

        public async Task<IEnumerable<WorkItem>> QueryAsync(string query)
        {
            log?.WriteLine(LogLevel.Query, $"AzDOService.ExecuteQueryAsync: {query}");

            Wiql wiql = new Wiql { Query = query };
            IEnumerable<Wit> workItems = null;

            using (var client = CreateWorkItemClient())
            {
                // Return a list of URLs + Ids for matching workItems.
                WorkItemQueryResult queryResult = await client.QueryByWiqlAsync(wiql).ConfigureAwait(false);
                if (queryResult.WorkItems.Any())
                {
                    var ids = queryResult.WorkItems.Select(wi => wi.Id).ToList();
                    workItems = await GetWorkItemsAsync(client, ids, ReflectionHelpers.AllFields, queryResult.AsOf).ConfigureAwait(false);
                }
            }
            return ReflectionHelpers.MapWorkItemTypes(workItems);
        }

        public async Task<IEnumerable<WorkItem>> GetChildrenAsync(WorkItem parent)
        {
            if (parent is null)
                throw new ArgumentNullException(nameof(parent));

            if (parent.IsNew)
                throw new ArgumentException("No children for a new WorkItem.");

            using var mc = log?.Enter(new object[] { parent.Id });

            var childIds = (await GetChildIdsAsync(parent.Id.Value).ConfigureAwait(false)).ToList();
            IEnumerable<Wit> workItems = null;

            using (var client = CreateWorkItemClient())
            {
                workItems = await GetWorkItemsAsync(client, childIds, ReflectionHelpers.AllFields, null).ConfigureAwait(false);
            }

            return ReflectionHelpers.MapWorkItemTypes(workItems);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query) where T : WorkItem, new()
        {
            log?.WriteLine(LogLevel.Query, $"AzDOService.ExecuteQueryAsync<{typeof(T)}>: {query}");
            var workItems = await this.QueryAsync(query, ReflectionHelpers.GetQueryFieldsForType(typeof(T))).ConfigureAwait(false);
            return workItems.Select(wi => WorkItem.FromWorkItem<T>(wi));
        }

        public async Task AddAsync(WorkItem workItem, string comment = null, bool? validateOnly = null, bool? bypassRules = null)
        {
            using var mc = log?.Enter($"AzDOService.AddAsync: {workItem.Title}");

            if (!workItem.IsNew)
            {
                throw new ArgumentException("Cannot add an existing workItem.", nameof(workItem));
            }

            var projectName = workItem.Project;
            if (string.IsNullOrEmpty(projectName))
            {
                throw new Exception("Must set ProjectName on WorkItem before adding to Azure DevOps.");
            }

            var workItemType = workItem.WorkItemType;
            if (string.IsNullOrEmpty(workItemType))
            {
                throw new Exception("Must set WorkItemType on WorkItem before adding to Azure DevOps.");
            }

            var patchDocument = workItem.CreatePatchDocument();
            if (patchDocument == null)
            {
                throw new Exception("Patch document couldn't be created from Workitem.");
            }

            if (!string.IsNullOrWhiteSpace(comment))
            {
                patchDocument.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/fields/System.History",
                    Value = comment
                });
            }

            var wit = await this.AddAsync(patchDocument, projectName, workItemType, validateOnly, bypassRules).ConfigureAwait(false);
            workItem.Initialize(wit);
        }

        public async Task UpdateAsync(WorkItem workItem, string comment = null, bool? validateOnly = null, bool? bypassRules = null)
        {
            using var mc = log?.Enter($"AzDOService.UpdateAsync: workItem:{workItem.Id}, comment:{comment}");

            if (workItem.IsNew)
                throw new ArgumentException("Cannot update a new WorkItem.", nameof(workItem));

            using var client = CreateWorkItemClient();
            var patchDocument = workItem.CreatePatchDocument();
            if (patchDocument == null)
                return;

            if (!string.IsNullOrWhiteSpace(comment))
            {
                patchDocument.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/fields/System.History",
                    Value = comment
                });
            }

            var wit = await this.UpdateAsync(workItem.Id.Value, patchDocument, validateOnly, bypassRules).ConfigureAwait(false);
            workItem.Initialize(wit);
        }
    }
}
