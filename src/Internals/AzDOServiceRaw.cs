using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using Wit = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Julmar.AzDOUtilities;

/// <summary>
/// The underlying raw service handler for Azure DevOps. These members
/// are all internal to the library - the public interface is in the other
/// half of this class (AzDOService.cs).
/// </summary>
partial class AzDOService
{
    private readonly Lazy<WorkItemTrackingHttpClient> httpClient;
    private readonly Lazy<VssConnection> connection;
    private readonly Lazy<ProjectHttpClient> projectClient;
    
    /// <summary>
    /// Internal logging support
    /// </summary>
    internal TraceHelpers? log;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="uri">Azure DevOps instance URL</param>
    /// <param name="accessToken">Access token</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    internal AzDOService(string uri, string accessToken)
    {
        if (uri is null) throw new ArgumentNullException(nameof(uri));
        if (string.IsNullOrEmpty(accessToken)) throw new ArgumentException("Missing access token", nameof(accessToken));

        var credentials = new VssBasicCredential(string.Empty, accessToken);

        this.connection = new Lazy<VssConnection>(() => new VssConnection(new Uri(uri), credentials));
        this.httpClient = new Lazy<WorkItemTrackingHttpClient>(() => new WorkItemTrackingHttpClient(new Uri(uri), credentials));
        this.projectClient = new Lazy<ProjectHttpClient>(() => new ProjectHttpClient(new Uri(uri), credentials));
    }

    /// <summary>
    /// Find a relationship by the link type
    /// </summary>
    /// <param name="linkType">Link type</param>
    /// <returns>Specific relationship</returns>
    internal static Relationship GetRelationshipFromLinkText(string? linkType)
    {
        foreach (var field in typeof(Relationship).GetFields())
        {
            if (field.GetCustomAttribute<XmlAttributeAttribute>()?.AttributeName == linkType) 
                return (Relationship) field.GetValue(null)!;
        }
        return Relationship.Other;
    }

    /// <summary>
    /// Get the identifier from a relationship URL
    /// </summary>
    /// <param name="url">URL</param>
    /// <returns>Identifier if present, null if missing.</returns>
    internal static int? ParseIdFromRelationship(string? url)
    {
        int? pos = url?.LastIndexOf("/", StringComparison.Ordinal);
        if(pos is > 0)
        {
            string num = url![(pos.Value + 1)..];
            if (int.TryParse(num, out int result))
                return result;
        }
        return null;
    }

    /// <summary>
    /// Make a call to Azure DevOps to retrieve a set of Wits based on a list of identifiers.
    /// </summary>
    /// <param name="ids">Identifiers to retrieve</param>
    /// <param name="fields">Fields to ask for</param>
    /// <param name="asOf">Date range</param>
    /// <param name="expand">Flag for WorkItem data expansion</param>
    /// <param name="errorPolicy">Error policy to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    private async Task<IEnumerable<Wit>> InternalGetWitsByIdChunked(List<int> ids, string[] fields, DateTime? asOf,
        WorkItemExpand? expand, WorkItemErrorPolicy? errorPolicy, CancellationToken cancellationToken)
    {
        if (ids == null) throw new ArgumentNullException(nameof(ids));

        errorPolicy ??= ErrorPolicy;

        int nSize = 200; // Max size
        var workItems = new List<Wit>();

        // Chunk the retrieval.
        for (int i = 0; i < ids.Count; i += nSize)
        {
            List<Wit>? results = null;
            try
            {
                var range = ids.GetRange(i, Math.Min(nSize, ids.Count - i));
                using var mc = log?.Enter(LogLevel.RawApis, new object?[] { range, fields, asOf, expand, errorPolicy, null, cancellationToken }, "GetWorkItemsAsync");
                {
                    results = await WorkItemClient
                        .GetWorkItemsAsync(range, fields, asOf, expand, errorPolicy, userState: null, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (VssServiceResponseException ex)
            {
                if (nSize == 1 || ex.HttpStatusCode != HttpStatusCode.NotFound) throw;

                log?.WriteLine(LogLevel.RawApis, $"{ex.GetType().Name}:{ex.HttpStatusCode} with {nSize} count. Retry.");

                // Try again with a smaller range.
                results = null;
                nSize = Math.Max(1, nSize/2);
                i -= nSize;
            }

            if (results != null)
                workItems.AddRange(results);
        }

        Debug.Assert(workItems.Count == ids.Count);
        return workItems;
    }

    /// <summary>
    /// Make a call to the Azure DevOps API to retrieve relationships for a given WIT identifier
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <param name="relationshipType">Relationship type to retrieve</param>
    /// <param name="asOf">Date range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    async Task<IReadOnlyList<RelationLinks>> InternalGetRelatedIdsAsync(int id, string? relationshipType, DateTime? asOf, CancellationToken cancellationToken)
    {
        var list = new List<RelationLinks>();

        using var mc = log?.Enter(LogLevel.RawApis, new object?[] { id, null, asOf, WorkItemExpand.Relations, null, cancellationToken }, "GetWorkItemsAsync");

        var workItem = await WorkItemClient
                                .GetWorkItemAsync(id, null, asOf, WorkItemExpand.Relations, userState: null, cancellationToken)
                                .ConfigureAwait(false);
        if (workItem.Relations != null)
        {
            list.AddRange(workItem.Relations
                .Where(r => string.IsNullOrEmpty(relationshipType) || r.Rel == relationshipType)
                .Select(r => new RelationLinks {
                    Title = r.Title,
                    Attributes = r.Attributes,
                    Type = GetRelationshipFromLinkText(r.Rel),
                    RelatedId = ParseIdFromRelationship(r.Url),
                    RawRelationshipType = r.Rel,
                    Url = r.Url
                }));
        }

        return list;
    }

    /// <summary>
    /// Make a call to Azure DevOps to retrieve a set of WITs based on a query
    /// </summary>
    /// <param name="query">Query to execute</param>
    /// <param name="fields">Fields to retrieve</param>
    /// <param name="top">Starting position</param>
    /// <param name="timePrecision"></param>
    /// <param name="expand"></param>
    /// <param name="errorPolicy"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<IEnumerable<Wit>> InternalGetWorkItemsAsync(string query, string[] fields, int? top, bool? timePrecision,
        WorkItemExpand? expand, WorkItemErrorPolicy? errorPolicy, CancellationToken cancellationToken)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        log?.WriteLine(LogLevel.Query, query);

        var wiql = new Wiql { Query = RemoveFieldsFromQuery(query) };
        errorPolicy ??= ErrorPolicy;

        using var mc = log?.Enter(LogLevel.RawApis, new object?[] { wiql.Query, timePrecision, top, null, cancellationToken }, "QueryByWiqlAsync");

        // Return a list of URLs + Ids for matching workItems.
        WorkItemQueryResult queryResult = await WorkItemClient
                    .QueryByWiqlAsync(wiql, timePrecision, top, userState: null, cancellationToken)
                    .ConfigureAwait(false);
        if (queryResult.WorkItems?.Any() == true)
        {
            var ids = queryResult.WorkItems.Select(wi => wi.Id).ToList();

            // Get the actual work items from the IDs; chunked.
            return await InternalGetWitsByIdChunked(ids, fields, queryResult.AsOf, expand, errorPolicy, cancellationToken)
                .ConfigureAwait(false);
        }

        return Enumerable.Empty<Wit>();
    }

    /// <summary>
    /// Turn "SELECT [x],[y],[z],* FROM ..." into "SELECT [id] FROM". WIQL ignores the specific fields and only returns
    /// the ID each time.
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    private static string RemoveFieldsFromQuery(string query)
    {
        int pos = query.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
        return pos == -1 ? query : $"SELECT [{WorkItemField.Id}] " + query[pos..];
    }

    /// <summary>
    /// Add a WIT using the Azure DevOps API
    /// </summary>
    /// <param name="patchDocument">Path document to add</param>
    /// <param name="projectName"></param>
    /// <param name="workItemType"></param>
    /// <param name="validateOnly"></param>
    /// <param name="bypassRules"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Wit> AddAsync(JsonPatchDocument patchDocument, string projectName, string workItemType, bool? validateOnly, bool? bypassRules, CancellationToken cancellationToken)
    {
        if (patchDocument == null) throw new ArgumentNullException(nameof(patchDocument));
        if (projectName == null) throw new ArgumentNullException(nameof(projectName));
        if (workItemType == null) throw new ArgumentNullException(nameof(workItemType));
        if (this.ValidateOnly) validateOnly = true;

        using var mc = log?.Enter(LogLevel.RawApis, new object?[] { patchDocument, projectName, workItemType, validateOnly, bypassRules, null, cancellationToken }, "CreateWorkItemAsync");
        log?.Dump(patchDocument);
        return await WorkItemClient
            .CreateWorkItemAsync(patchDocument, projectName, workItemType, validateOnly, bypassRules, userState: null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Call the Azure DevOps API to update a WIT.
    /// </summary>
    /// <param name="id">WIT identifier</param>
    /// <param name="patchDocument">Patch document with the changes</param>
    /// <param name="validateOnly">True to validate only, no commit.</param>
    /// <param name="bypassRules">True to bypass the rules for the WIT type</param>
    /// <param name="supressNotifications"></param>
    /// <param name="expand"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Wit> UpdateAsync(int id, JsonPatchDocument patchDocument, bool? validateOnly, bool? bypassRules,
        bool? supressNotifications, WorkItemExpand? expand, CancellationToken cancellationToken)
    {
        if (patchDocument == null) throw new ArgumentNullException(nameof(patchDocument));
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (this.ValidateOnly) validateOnly = true;

        using var mc = log?.Enter(LogLevel.RawApis, new object?[] { patchDocument, id, validateOnly, bypassRules, supressNotifications, expand, null, cancellationToken }, "UpdateWorkItemAsync");
        log?.Dump(patchDocument);

        return await WorkItemClient
            .UpdateWorkItemAsync(patchDocument, id, validateOnly, bypassRules, supressNotifications, expand, userState: null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieve a list of the queries available to the project and authenticated user.
    /// </summary>
    /// <param name="projectName">Project to query</param>
    /// <param name="parent">Query folder</param>
    /// <param name="depth">Depth</param>
    /// <param name="includeDeleted">True to include deleted queries</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<QueryHierarchyItem> InternalGetStoredQueriesAsync(string projectName, QueryHierarchyItem parent, int? depth, bool? includeDeleted, CancellationToken cancellationToken)
    {
        if (projectName == null) throw new ArgumentNullException(nameof(projectName));
        if (parent == null) throw new ArgumentNullException(nameof(parent));

        if (parent.HasChildren == true && parent.Children == null)
        {
            using var mc = log?.Enter(LogLevel.RawApis, new object?[] { projectName, parent.Path, QueryExpand.All, depth, includeDeleted, null, cancellationToken }, "GetQueryAsync");
            try
            {
                parent = await WorkItemClient.GetQueryAsync(projectName, query: parent.Path, expand: QueryExpand.All,
                    depth: 2,
                    includeDeleted: includeDeleted, userState: null,
                    cancellationToken: cancellationToken);
            }
            catch (VssServiceResponseException ex)
            {
                if (ex.HttpStatusCode == HttpStatusCode.NotFound)
                {
                    parent.HasChildren = false;
                }
            }
            catch (VssServiceException)
            {
                parent.HasChildren = false;
            }
        }

        if (parent.HasChildren == true && parent.Children != null)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                QueryHierarchyItem item = parent.Children[i];
                if (item.HasChildren == true)
                {
                    parent.Children[i] =
                        await InternalGetStoredQueriesAsync(projectName, item, depth, includeDeleted, cancellationToken);
                }
            }
        }

        return parent;
    }

    /// <summary>
    /// Call Azure DevOps to add a relationship
    /// </summary>
    /// <param name="linkType">Relationship type</param>
    /// <param name="owner">Owner</param>
    /// <param name="relatedItems">Related items</param>
    /// <param name="bypassRules">True to bypass any rules</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    private async Task InternalAddRelationshipAsync(string linkType, WorkItem owner, IEnumerable<WorkItem> relatedItems, bool bypassRules, CancellationToken cancellationToken)
    {
        if (linkType == null) throw new ArgumentNullException(nameof(linkType));
        if (owner is null) throw new ArgumentNullException(nameof(owner));
        if (relatedItems == null) throw new ArgumentNullException(nameof(relatedItems));
        if (owner.IsNew) throw new ArgumentException("Cannot add related items to new WorkItem.", nameof(owner));

        var items = relatedItems.ToList();
        if (items.Any(wi => wi.IsNew))
            throw new ArgumentException("Cannot add NEW related items to a WorkItem.", nameof(owner));

        var patchDocument = owner.CreatePatchDocument() ?? new JsonPatchDocument();
        foreach (var relatedItem in items)
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

        var wit = await UpdateAsync(owner.Id!.Value, patchDocument, this.ValidateOnly, bypassRules: bypassRules,
                supressNotifications: null, WorkItemExpand.Fields, cancellationToken)
            .ConfigureAwait(false);
        log?.WriteLine(LogLevel.PatchDocument, $"WorkItem {owner.Id} updated from Rev {owner.Revision} to {wit.Rev}");
        owner.Initialize(wit);
    }

    /// <summary>
    /// Returns a URL with no revision marker on it.
    /// </summary>
    /// <param name="url">Url</param>
    /// <returns>Url without the revision</returns>
    private static string StripRevisionFromUrl(string url)
    {
        if (url == null) throw new ArgumentNullException(nameof(url));

        const string revisions = "/revisions/";
        if (url.Contains(revisions))
        {
            int index = url.LastIndexOf(revisions, StringComparison.Ordinal);
            url = url[..index];
        }
        return url;
    }

    /// <summary>
    /// Remove a set of relationships from a WIT
    /// </summary>
    /// <param name="owner">Work item</param>
    /// <param name="relatedItems">Relations to remove</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    private async Task InternalRemoveRelationshipAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken)
    {
        if (owner is null) throw new ArgumentNullException(nameof(owner));
        if (relatedItems == null) throw new ArgumentNullException(nameof(relatedItems));
        if (owner.IsNew) throw new ArgumentException("Cannot remove related items from new WorkItem.", nameof(owner));

        var relItems = relatedItems.ToList();
        if (relItems.Any(wi => wi.IsNew))
            throw new ArgumentException("Cannot remove NEW related items from a WorkItem.", nameof(owner));

        var items = (await InternalGetRelatedIdsAsync(owner.Id!.Value, null, asOf: null, cancellationToken))
                    .Where(rid => rid.RelatedId.HasValue)
                    .Select(rid => rid.RelatedId!.Value)
                    .ToList();

        var operations = (
            from item in relItems 
            select items.IndexOf(item.Id!.Value) into index 
            where index >= 0 
            select new JsonPatchOperation {Operation = Operation.Remove, Path = $"/relations/{index}"}
            ).ToList();

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