using System.Reflection;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Julmar.AzDOUtilities;

/// <summary>
/// Implementation of the AzureDevOps service object. This is the public interface
/// representation of our service.
/// </summary>
sealed partial class AzDOService : IAzureDevOpsService
{
    private int maxBatchSize = 100;

    /// <summary>
    /// Max batch size
    /// </summary>
    public int MaxBatchSize
    {
        get => maxBatchSize;
        set
        {
            if (value is <= 0 or > 200)
                throw new ArgumentOutOfRangeException(nameof(value));
            maxBatchSize = value;
        }
    }

    /// <summary>
    /// Client for the WIT tracking
    /// </summary>
    public WorkItemTrackingHttpClient WorkItemClient => httpClient.Value;

    /// <summary>
    /// Connection to the Azure DevOps API
    /// </summary>
    public VssConnection Connection => connection.Value;

    /// <summary>
    /// Specific project we're tied to in Azure DevOps
    /// </summary>
    public ProjectHttpClient ProjectClient => projectClient.Value;

    /// <summary>
    /// The current error policy to apply
    /// </summary>
    public WorkItemErrorPolicy? ErrorPolicy { get; set; }

    /// <summary>
    /// True to only validate POST (change) requests, does not commit changes, but returns any errors
    /// </summary>
    public bool ValidateOnly { get; set; }
        
    /// <summary>
    /// The trace log handler. Set this to activate any tracing
    /// </summary>
    public Action<string>? TraceLog
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

    /// <summary>
    /// Tracing level
    /// </summary>
    public LogLevel TraceLevel
    {
        get => log?.TraceLevel ?? LogLevel.None;
        set
        {
            if (log == null && value != LogLevel.None)
                log = new TraceHelpers { TraceLevel = value };
            else if (log != null)
                log.TraceLevel = value;
        }
    }

    /// <summary>
    /// Retrieve all the areas for the given project
    /// </summary>
    /// <param name="projectName">Project</param>
    /// <param name="depth">Parent/child depth</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Areas</returns>
    public async Task<IReadOnlyList<WorkItemClassificationNode>> GetAreasAsync(string projectName, int? depth, CancellationToken cancellationToken)
    {
        if (projectName == null) throw new ArgumentNullException(nameof(projectName));
        using var mc = log?.Enter(new object?[] { projectName, depth, cancellationToken });

        var nodes = new List<WorkItemClassificationNode>();
        var project = await ProjectClient.GetProject(projectName).ConfigureAwait(false);
        var currentIteration = await WorkItemClient.GetClassificationNodeAsync(project.Name, TreeStructureGroup.Areas,
            path: null, depth, userState: null, cancellationToken).ConfigureAwait(false);
        AddChildIterations(nodes, currentIteration);

        return nodes.AsReadOnly();
    }

    /// <summary>
    /// Retrieve all the iterations for the given project
    /// </summary>
    /// <param name="projectName">Project</param>
    /// <param name="depth">Parent/child depth</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Areas</returns>
    public async Task<IReadOnlyList<WorkItemClassificationNode>> GetIterationsAsync(string projectName, int? depth, CancellationToken cancellationToken)
    {
        if (projectName == null) throw new ArgumentNullException(nameof(projectName));
        using var mc = log?.Enter(new object?[] { projectName, depth, cancellationToken });

        var nodes = new List<WorkItemClassificationNode>();
        var project = await ProjectClient.GetProject(projectName).ConfigureAwait(false);
        var currentIteration = await WorkItemClient.GetClassificationNodeAsync(project.Name, TreeStructureGroup.Iterations,
            path: null, depth, userState: null, cancellationToken).ConfigureAwait(false);
        AddChildIterations(nodes, currentIteration);

        return nodes.AsReadOnly();
    }

    /// <summary>
    /// Recursive method to walk a set of node iterations and build a list of linear nodes.
    /// </summary>
    /// <param name="nodes">List to build</param>
    /// <param name="currentIteration">Starting point</param>
    private static void AddChildIterations(ICollection<WorkItemClassificationNode> nodes, WorkItemClassificationNode currentIteration)
    {
        nodes.Add(currentIteration);
        if (currentIteration.Children != null)
        {
            foreach (var child in currentIteration.Children)
                AddChildIterations(nodes, child);
        }
    }

    /// <summary>
    /// Convert the Relationship enumeration into a specific relationship link text.
    /// </summary>
    /// <param name="relationship"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal static string GetRelationshipLinkText(Relationship relationship)
    {
        string linkType = relationship.GetType().GetField(relationship.ToString())!
            .GetCustomAttribute<XmlAttributeAttribute>()?.AttributeName ?? "";
        if (string.IsNullOrEmpty(linkType))
            throw new ArgumentOutOfRangeException(nameof(relationship), "Must specify a valid relationship type.");
        return linkType;
    }

    /// <summary>
    /// Retrieves all the related WorkItem ids to a given WorkItem.
    /// </summary>
    /// <param name="id">ID of the WorkItem</param>
    /// <param name="relationship">Relationship to query for</param>
    /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>List of related WorkItem ids</returns>
    public Task<IReadOnlyList<RelationLinks>> GetRelatedIdsAsync(int id, Relationship relationship, DateTime? asOf, CancellationToken cancellationToken)
    {
        if (relationship == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationship))
            throw new ArgumentOutOfRangeException(nameof(relationship));

        using var mc = log?.Enter(LogLevel.RelatedApis, new object?[] { id, asOf, cancellationToken });
        return InternalGetRelatedIdsAsync(id, GetRelationshipLinkText(relationship), asOf, cancellationToken);
    }

    /// <summary>
    /// Retrieve the accessible stored queries
    /// </summary>
    /// <param name="projectName">Optional project name to scope to</param>
    /// <param name="depth">Only retrieve personal queries</param>
    /// <param name="includeDeleted"><see langword="true"/>to include deleted queries in the recycle bin</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Queries the current user has access to</returns>
    public async Task<IReadOnlyList<QueryHierarchyItem>> GetStoredQueriesAsync(string projectName, int? depth, bool? includeDeleted, CancellationToken cancellationToken)
    {
        using var mc = log?.Enter(new object?[] { projectName, includeDeleted, cancellationToken });
        var results = await WorkItemClient.GetQueriesAsync(projectName, QueryExpand.All, depth: depth, includeDeleted, userState: null, cancellationToken);
        return results.AsReadOnly();
    }

    /// <summary>
    /// Retrieve the details for a specific stored query.
    /// </summary>
    /// <param name="projectName">Optional project name to scope to</param>
    /// <param name="item">Query to retrieve details for</param>
    /// <param name="depth">Only retrieve personal queries</param>
    /// <param name="includeDeleted"><see langword="true"/>to include deleted queries in the recycle bin</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns></returns>
    public async Task<QueryHierarchyItem> GetStoredQueryDetailsAsync(string projectName, QueryHierarchyItem item,
        int? depth, bool? includeDeleted, CancellationToken cancellationToken)
    {
        return await InternalGetStoredQueriesAsync(projectName, item, depth, includeDeleted, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id">Query GUID</param>
    /// <param name="timePrecision">True to use Date/Time vs. just Date</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Query results</returns>
    public async Task<WorkItemQueryResult> ExecuteStoredQueryAsync(Guid id, bool? timePrecision, CancellationToken cancellationToken)
    {
        using var mc = log?.Enter(new object?[] { id, timePrecision, cancellationToken });
        return await WorkItemClient.QueryByIdAsync(id, timePrecision, userState: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieve a single WorkItem
    /// </summary>
    /// <param name="id">ID to retrieve</param>
    /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>WorkItem object, null if it doesn't exist</returns>
    public async Task<WorkItem?> GetAsync(int id, DateTime? asOf, CancellationToken cancellationToken)
    {
        using var mc = log?.Enter(new object[] { id, cancellationToken });

        try
        {
            var wit = await WorkItemClient.GetWorkItemAsync(id, ReflectionHelpers.GetAllFields(this),
                asOf, WorkItemExpand.None, userState: null, cancellationToken).ConfigureAwait(false);
            return wit != null ? ReflectionHelpers.MapWorkItemTypes(new[] { wit }).Single() : null;
        }
        catch (VssServiceResponseException ex)
        {
            if (ex.HttpStatusCode != System.Net.HttpStatusCode.NotFound)
                throw;
        }

        return null;
    }

    /// <summary>
    /// Retrieve a set of WorkItems by ID
    /// </summary>
    /// <param name="ids">IDs to retrieve</param>
    /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>List of WorkItems matching Ids</returns>
    public async Task<IEnumerable<WorkItem>> GetAsync(IEnumerable<int> ids, DateTime? asOf, CancellationToken cancellationToken)
    {
        using var mc = log?.Enter(new object?[] { ids, asOf, cancellationToken });
        var wits = await InternalGetWitsByIdChunked(ids.ToList(), ReflectionHelpers.GetAllFields(this),
            asOf, WorkItemExpand.None, ErrorPolicy, cancellationToken).ConfigureAwait(false);
        return ReflectionHelpers.MapWorkItemTypes(wits);
    }

    /// <summary>
    /// Add a new child to a WorkItem
    /// </summary>
    /// <param name="parent">Parent</param>
    /// <param name="child">Child</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddChildAsync(WorkItem parent, WorkItem child, CancellationToken cancellationToken)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        if (child == null) throw new ArgumentNullException(nameof(child));

        using var mc = log?.Enter(new object[] { parent, child, cancellationToken  });
        return InternalAddRelationshipAsync(GetRelationshipLinkText(Relationship.Child), parent, new[] { child }, true, cancellationToken);
    }

    /// <summary>
    /// Add a new child to a WorkItem
    /// </summary>
    /// <param name="parent">Parent</param>
    /// <param name="child">Child</param>
    /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddChildAsync(WorkItem parent, WorkItem child, bool bypassRules, CancellationToken cancellationToken)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        if (child == null) throw new ArgumentNullException(nameof(child));

        using var mc = log?.Enter(new object[] { parent, child, bypassRules, cancellationToken });
        return InternalAddRelationshipAsync(GetRelationshipLinkText(Relationship.Child), parent, new[] { child }, bypassRules, cancellationToken);
    }

    /// <summary>
    /// Add multiple children to a WorkItem
    /// </summary>
    /// <param name="parent">Parent</param>
    /// <param name="children">Child work items</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddChildrenAsync(WorkItem parent, IEnumerable<WorkItem> children, CancellationToken cancellationToken)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        if (children == null) throw new ArgumentNullException(nameof(children));

        using var mc = log?.Enter(new object[] { parent, children, cancellationToken });
        return InternalAddRelationshipAsync(GetRelationshipLinkText(Relationship.Child), parent, children, true, cancellationToken);
    }

    /// <summary>
    /// Add multiple children to a WorkItem
    /// </summary>
    /// <param name="parent">Parent</param>
    /// <param name="children">Child work items</param>
    /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddChildrenAsync(WorkItem parent, IEnumerable<WorkItem> children, bool bypassRules, CancellationToken cancellationToken)
    {
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        if (children == null) throw new ArgumentNullException(nameof(children));

        using var mc = log?.Enter(new object[] { parent, children, bypassRules, cancellationToken });
        return InternalAddRelationshipAsync(GetRelationshipLinkText(Relationship.Child), parent, children, bypassRules, cancellationToken);
    }

    /// <summary>
    /// Add a new related item to a WorkItem
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relatedItem">Related Work Item</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddRelatedAsync(WorkItem owner, WorkItem relatedItem, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItem == null) throw new ArgumentNullException(nameof(relatedItem));

        using var mc = log?.Enter(new object[] { owner, relatedItem, cancellationToken });
        return InternalAddRelationshipAsync(GetRelationshipLinkText(Relationship.Related), owner, new[] { relatedItem }, true, cancellationToken);
    }

    /// <summary>
    /// Add a new related item to a WorkItem
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relatedItem">Related Work Item</param>
    /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddRelatedAsync(WorkItem owner, WorkItem relatedItem, bool bypassRules, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItem == null) throw new ArgumentNullException(nameof(relatedItem));

        using var mc = log?.Enter(new object[] { owner, relatedItem, bypassRules, cancellationToken });
        return InternalAddRelationshipAsync(GetRelationshipLinkText(Relationship.Related), owner, new[] { relatedItem }, bypassRules, cancellationToken);
    }

    /// <summary>
    /// Add multiple related items to a WorkItem
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relatedItems">Related Work Items</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddRelatedAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItems == null) throw new ArgumentNullException(nameof(relatedItems));

        using var mc = log?.Enter(new object[] { owner, relatedItems, cancellationToken });
        return InternalAddRelationshipAsync(GetRelationshipLinkText(Relationship.Related), owner, relatedItems, true, cancellationToken);
    }

    /// <summary>
    /// Add multiple related items to a WorkItem
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relatedItems">Related Work Items</param>
    /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddRelatedAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, bool bypassRules, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItems == null) throw new ArgumentNullException(nameof(relatedItems));

        using var mc = log?.Enter(new object[] { owner, relatedItems, bypassRules, cancellationToken });
        return InternalAddRelationshipAsync(GetRelationshipLinkText(Relationship.Related), owner, relatedItems, bypassRules, cancellationToken);
    }

    /// <summary>
    /// Add a new related item to a WorkItem
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relationshipType">Relationship type to create</param>
    /// <param name="relatedItem">Related Work Item</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, WorkItem relatedItem, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItem == null) throw new ArgumentNullException(nameof(relatedItem));

        using var mc = log?.Enter(new object[] { relationshipType, owner, relatedItem, cancellationToken });

        if (relationshipType == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationshipType))
            throw new ArgumentOutOfRangeException(nameof(relationshipType));

        return InternalAddRelationshipAsync(GetRelationshipLinkText(relationshipType), owner, new[] { relatedItem }, true, cancellationToken);
    }

    /// <summary>
    /// Add a new related item to a WorkItem
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relationshipType">Relationship type to create</param>
    /// <param name="relatedItem">Related Work Item</param>
    /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, WorkItem relatedItem, bool bypassRules, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItem == null) throw new ArgumentNullException(nameof(relatedItem));

        using var mc = log?.Enter(new object[] { relationshipType, owner, relatedItem, bypassRules, cancellationToken });

        if (relationshipType == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationshipType))
            throw new ArgumentOutOfRangeException(nameof(relationshipType));

        return InternalAddRelationshipAsync(GetRelationshipLinkText(relationshipType), owner, new[] { relatedItem }, bypassRules, cancellationToken);
    }


    /// <summary>
    /// Add multiple related items to a WorkItem
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relationshipType">Relationship type to create</param>
    /// <param name="relatedItems">Related Work Items</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItems == null) throw new ArgumentNullException(nameof(relatedItems));

        using var mc = log?.Enter(new object[] { relationshipType, owner, relatedItems, cancellationToken });

        if (relationshipType == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationshipType))
            throw new ArgumentOutOfRangeException(nameof(relationshipType));

        return InternalAddRelationshipAsync(GetRelationshipLinkText(relationshipType), owner, relatedItems, true, cancellationToken);
    }

    /// <summary>
    /// Add multiple related items to a WorkItem
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relationshipType">Relationship type to create</param>
    /// <param name="relatedItems">Related Work Items</param>
    /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, IEnumerable<WorkItem> relatedItems, bool bypassRules, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItems == null) throw new ArgumentNullException(nameof(relatedItems));

        using var mc = log?.Enter(new object[] { relationshipType, owner, relatedItems, bypassRules, cancellationToken });

        if (relationshipType == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationshipType))
            throw new ArgumentOutOfRangeException(nameof(relationshipType));

        return InternalAddRelationshipAsync(GetRelationshipLinkText(relationshipType), owner, relatedItems, bypassRules, cancellationToken);
    }


    /// <summary>
    /// Remove a related item (child, related)
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relatedItem">Related item to remove</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task RemoveRelationshipAsync(WorkItem owner, WorkItem relatedItem, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItem == null) throw new ArgumentNullException(nameof(relatedItem));

        using var mc = log?.Enter(new object[] { owner, relatedItem, cancellationToken });
        return InternalRemoveRelationshipAsync(owner, new[] { relatedItem }, cancellationToken);
    }

    /// <summary>
    /// Remove a set of related items (child, related)
    /// </summary>
    /// <param name="owner">Owner object</param>
    /// <param name="relatedItems">Related items to remove</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    public Task RemoveRelationshipAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (relatedItems == null) throw new ArgumentNullException(nameof(relatedItems));

        using var mc = log?.Enter(new object[] { owner, relatedItems, cancellationToken });
        return InternalRemoveRelationshipAsync(owner, relatedItems, cancellationToken);
    }

    /// <summary>
    /// Executes a linked query and returns the relationships.
    /// </summary>
    /// <param name="query">Query to execute</param>
    /// <param name="top"># of items to retrieve, pass null for all matching (up to 20000)</param>
    /// <param name="timePrecision">True to match Date/Time vs. just date</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A set of relationship links (source/target/relationship) based on the query</returns>
    public async Task<IEnumerable<WorkItemLink>> QueryLinkedRelationshipsAsync(string query, int? top, bool? timePrecision, CancellationToken cancellationToken)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        using var mc = log?.Enter(new object?[] { query, top, timePrecision, cancellationToken });

        var wiql = new Wiql { Query = query };

        var results = await WorkItemClient
            .QueryByWiqlAsync(wiql, timePrecision, top, userState: null, cancellationToken)
            .ConfigureAwait(false);
        return results.WorkItemRelations ?? Enumerable.Empty<WorkItemLink>();
    }

    /// <summary>
    /// Get the related WorkItems for the given item.
    /// </summary>
    /// <param name="owner">Owning WorkItem</param>
    /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Related items, empty if none.</returns>
    public async Task<IEnumerable<WorkItem>> GetRelatedAsync(WorkItem owner, DateTime? asOf, CancellationToken cancellationToken)
    {
        if (owner is null) throw new ArgumentNullException(nameof(owner));
        if (owner.IsNew) throw new ArgumentException("No relationships on a new WorkItem");

        using var mc = log?.Enter(new object[] { owner.Id! });

        var relatedIds = (await InternalGetRelatedIdsAsync(owner.Id!.Value, GetRelationshipLinkText(Relationship.Related), asOf, cancellationToken)
            .ConfigureAwait(false)).Where(rid => rid.RelatedId.HasValue).Select(rid => rid.RelatedId!.Value).ToList();
        var wits = await InternalGetWitsByIdChunked(relatedIds, ReflectionHelpers.GetAllFields(this), asOf,
                WorkItemExpand.None, ErrorPolicy, cancellationToken)
            .ConfigureAwait(false);
        return ReflectionHelpers.MapWorkItemTypes(wits);
    }

    /// <summary>
    /// Execute a string-based query and return a set of Work Items matching the query
    /// </summary>
    /// <param name="query">Query text. Must be in the WIQL syntax. See https://docs.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops</param>
    /// <param name="top"># of items to retrieve, pass null for all matching (up to 20000)</param>
    /// <param name="timePrecision">True to match Date/Time vs. just date</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Matching Work Item objects. Note that the actual returned types will match registered types if possible.</returns>
    public async Task<IEnumerable<WorkItem>> QueryAsync(string query, int? top, bool? timePrecision, CancellationToken cancellationToken)
    {
        using var mc = log?.Enter(new object?[] { query, top, timePrecision, cancellationToken });

        var workItems = await InternalGetWorkItemsAsync(query, ReflectionHelpers.GetAllFields(this),
                top, timePrecision, WorkItemExpand.None, ErrorPolicy, cancellationToken)
            .ConfigureAwait(false);
        return ReflectionHelpers.MapWorkItemTypes(workItems);
    }

    /// <summary>
    /// Query for a specific WIT type
    /// </summary>
    /// <param name="expectedType">Expected type</param>
    /// <param name="query">Query to execute</param>
    /// <param name="top"></param>
    /// <param name="timePrecision"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    internal async Task<IEnumerable<WorkItem>> QueryForTypeAsync(Type expectedType, string query, int? top, bool? timePrecision, CancellationToken cancellationToken)
    {
        if (expectedType == null) throw new ArgumentNullException(nameof(expectedType));
        if (query == null) throw new ArgumentNullException(nameof(query));

        // This version is used by LINQ to constrain the fields to what's on the type.
        // But we can't add constraints to QueryProvider<T> so we pass the type in here
        // and constrain it directly
        using var mc = log?.Enter(new object?[] { query, top, timePrecision, cancellationToken }, nameof(QueryAsync));

        var typeFields = (expectedType == typeof(WorkItem))
            ? ReflectionHelpers.GetAllFields(this)
            : ReflectionHelpers.GetQueryFieldsForType(expectedType).AvailableFields(this).ToArray();

        var workItems = await InternalGetWorkItemsAsync(query, typeFields,
                top, timePrecision, WorkItemExpand.None, ErrorPolicy, cancellationToken)
            .ConfigureAwait(false);

        return ReflectionHelpers.MapWorkItemTypes(workItems);
    }

    /// <summary>
    /// Execute a string-based query and return a set of Work Items matching the query
    /// </summary>
    /// <typeparam name="T">Type of WorkItem object to return</typeparam>
    /// <param name="query">Query text. Must be in the WIQL syntax. See https://docs.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops</param>
    /// <param name="top"># of items to retrieve, pass null for all matching (up to 20000)</param>
    /// <param name="timePrecision">True to match Date/Time vs. just date</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Matching Work Item objects.</returns>
    public async Task<IEnumerable<T>> QueryAsync<T>(string query, int? top, bool? timePrecision, CancellationToken cancellationToken) where T : WorkItem, new()
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        using var mc = log?.Enter(new object?[] { query, top, timePrecision, cancellationToken });

        var typeFields =  (typeof(T) == typeof(WorkItem))
            ? ReflectionHelpers.GetAllFields(this)
            : ReflectionHelpers.GetQueryFieldsForType(typeof(T)).AvailableFields(this).ToArray();
        var workItems = await InternalGetWorkItemsAsync(query, typeFields,
                top, timePrecision, WorkItemExpand.None, ErrorPolicy, cancellationToken)
            .ConfigureAwait(false);
        return workItems.Select(ReflectionHelpers.FromWorkItem<T>);
    }

    /// <summary>
    /// Get the parent WorkItem for the given child.
    /// </summary>
    /// <param name="child">Child WorkItem</param>
    /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Parent, or <see langword="null"/> if none.</returns>
    public async Task<WorkItem?> GetParentAsync(WorkItem child, DateTime? asOf, CancellationToken cancellationToken)
    {
        if (child is null) throw new ArgumentNullException(nameof(child));
        if (child.IsNew) throw new ArgumentException("No children for a new WorkItem.");

        using var mc = log?.Enter(new object?[] { child, asOf, cancellationToken });

        var parentRelationship = (await InternalGetRelatedIdsAsync(child.Id!.Value, GetRelationshipLinkText(Relationship.Parent),
            asOf, cancellationToken).ConfigureAwait(false)).SingleOrDefault();
        if (parentRelationship?.RelatedId == null)
            return null;

        var wit = await WorkItemClient.GetWorkItemAsync(parentRelationship.RelatedId.Value, ReflectionHelpers.GetAllFields(this), asOf,
            WorkItemExpand.None, userState: null, cancellationToken).ConfigureAwait(false);

        return ReflectionHelpers.MapWorkItemTypes(new[] { wit }).Single();
    }

    /// <summary>
    /// Get the child WorkItems for the given item.
    /// </summary>
    /// <param name="parent">Parent WorkItem</param>
    /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Related items, empty if none.</returns>
    public async Task<IEnumerable<WorkItem>> GetChildrenAsync(WorkItem parent, DateTime? asOf, CancellationToken cancellationToken)
    {
        if (parent is null) throw new ArgumentNullException(nameof(parent));
        if (parent.IsNew) throw new ArgumentException("No children for a new WorkItem.");

        using var mc = log?.Enter(new object?[] { parent, asOf, cancellationToken });

        var childIds = (await InternalGetRelatedIdsAsync(parent.Id!.Value, GetRelationshipLinkText(Relationship.Child),
                asOf, cancellationToken).ConfigureAwait(false))
            .Where(rid => rid.RelatedId.HasValue).Select(rid => rid.RelatedId!.Value).ToList();
        var wits = await InternalGetWitsByIdChunked(childIds, ReflectionHelpers.GetAllFields(this), asOf,
            WorkItemExpand.None, ErrorPolicy, cancellationToken).ConfigureAwait(false);

        return ReflectionHelpers.MapWorkItemTypes(wits);
    }

    /// <summary>
    /// Add a new Work Item to the system
    /// </summary>
    /// <param name="workItem">WorkItem to add. Can be a derived class. Must be a new object.</param>
    /// <param name="comment">Optional comment to add to the created item.</param>
    /// <param name="validateOnly">True to validate the creation only. If not supplied will use the global ValidateOnly property.</param>
    /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <remarks>The passed WorkItem is updated based on the response from the server.</remarks>
    public async Task AddAsync(WorkItem workItem, string? comment, bool? validateOnly, bool? bypassRules, CancellationToken cancellationToken)
    {
        if (workItem == null) throw new ArgumentNullException(nameof(workItem));
        if (!workItem.IsNew) throw new ArgumentException("Cannot add an existing workItem.", nameof(workItem));
        
        using var mc = log?.Enter(new object?[] { workItem, comment, validateOnly, bypassRules, cancellationToken });

        var projectName = workItem.Project;
        if (string.IsNullOrEmpty(projectName))
            throw new Exception("Must set ProjectName on WorkItem before adding to Azure DevOps.");

        var workItemType = workItem.WorkItemType;
        if (string.IsNullOrEmpty(workItemType))
            throw new Exception("Must set WorkItemType on WorkItem before adding to Azure DevOps.");

        if (!string.IsNullOrWhiteSpace(comment))
            workItem.AddCommentToHistory(comment);

        var patchDocument = workItem.CreatePatchDocument();
        if (patchDocument == null)
            throw new Exception("Patch document couldn't be created from WorkItem.");

        var wit = await this.AddAsync(patchDocument, projectName, workItemType, validateOnly, bypassRules, cancellationToken)
            .ConfigureAwait(false);
        log?.WriteLine(LogLevel.PatchDocument, $"WorkItem {workItem.Id} updated from Rev {workItem.Revision} to {wit.Rev}");
        workItem.Initialize(wit);
    }

    /// <summary>
    /// Update an existing Work Item to the system
    /// </summary>
    /// <param name="workItem">WorkItem to update. Can be a derived class. Cannot be a new object.</param>
    /// <param name="comment">Optional comment to add to the created item.</param>
    /// <param name="validateOnly">True to validate the creation only. If not supplied will use the global ValidateOnly property.</param>
    /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <remarks>The passed WorkItem is updated based on the response from the server.</remarks>
    public async Task UpdateAsync(WorkItem workItem, string? comment, bool? validateOnly, bool? bypassRules, CancellationToken cancellationToken)
    {
        if (workItem == null) throw new ArgumentNullException(nameof(workItem));
        if (workItem.IsNew) throw new ArgumentException("Cannot update a new WorkItem.", nameof(workItem));

        using var mc = log?.Enter(new object?[] { workItem, comment, validateOnly, bypassRules, cancellationToken });

        if (!workItem.HasChanges) return;

        if (!string.IsNullOrWhiteSpace(comment))
        {
            workItem.AddCommentToHistory(comment);
        }

        var patchDocument = workItem.CreatePatchDocument();
        if (patchDocument == null)
            return;

        var wit = await this.UpdateAsync(workItem.Id!.Value, patchDocument, validateOnly, bypassRules, supressNotifications: null,
                WorkItemExpand.Fields, cancellationToken)
            .ConfigureAwait(false);
        log?.WriteLine(LogLevel.PatchDocument, $"WorkItem {workItem.Id} updated from Rev {workItem.Revision} to {wit.Rev}");
        workItem.Initialize(wit);
    }

    /// <summary>
    /// Delete a WorkItem from the system.
    /// </summary>
    /// <param name="id">ID of the WorkItem to remove.</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>True/False success code.</returns>
    /// <remarks>The WorkItem is not permanently deleted but is placed in the recycle bin.</remarks>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        using var mc = log?.Enter(LogLevel.EnterExit, new object[] { id, cancellationToken });

        if (!this.ValidateOnly)
        {
            var wit = await WorkItemClient.DeleteWorkItemAsync(id, destroy: false, userState: null, cancellationToken)
                .ConfigureAwait(false);
            return wit.Id == id;
        }

        return false;
    }
}