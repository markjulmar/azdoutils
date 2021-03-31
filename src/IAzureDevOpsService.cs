using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Julmar.AzDOUtilities
{
    /// <summary>
    /// Service to interact with Azure DevOps REST API
    /// </summary>
    public interface IAzureDevOpsService
    {
        /// <summary>
        /// Connection to the underlying VSTS services
        /// </summary>
        public VssConnection Connection { get; }

        /// <summary>
        /// WorkItem client for raw access
        /// </summary>
        WorkItemTrackingHttpClient WorkItemClient { get; }

        /// <summary>
        /// Project client for raw access
        /// </summary>
        ProjectHttpClient ProjectClient { get; }

        /// <summary>
        /// Tracing delegate - assign to callback to get trace events.
        /// </summary>
        Action<string> TraceLog { get; set; }

        /// <summary>
        /// Tracing level
        /// </summary>
        LogLevel TraceLevel { get; set; }

        /// <summary>
        /// True to turn off writes for all APIs.
        /// </summary>
        bool ValidateOnly { get; set; }

        /// <summary>
        /// Default error policy.
        /// </summary>
        WorkItemErrorPolicy? ErrorPolicy { get; set; }

        /// <summary>
        /// Retrieve all the areas for the given project
        /// </summary>
        /// <param name="projectName">Project</param>
        /// <param name="depth">Parent/child depth</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Areas</returns>
        Task<IEnumerable<WorkItemClassificationNode>> GetAreasAsync(string projectName, int? depth = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieve all the iterations for the given project
        /// </summary>
        /// <param name="projectName">Project</param>
        /// <param name="depth">Parent/child depth</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Areas</returns>
        Task<IEnumerable<WorkItemClassificationNode>> GetIterationsAsync(string projectName, int? depth = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieve the accessible stored queries
        /// </summary>
        /// <param name="projectName">Optional project name to scope to</param>
        /// <param name="includeDeleted"><see langword="true"/>to include deleted queries in the recycle bin</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        /// <returns>Queries the current user has access to</returns>
        Task<IEnumerable<QueryHierarchyItem>> GetStoredQueriesAsync(string projectName, bool? includeDeleted = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">Query GUID</param>
        /// <param name="timePrecision">True to use Date/Time vs. just Date</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        /// <returns>Query results</returns>
        Task<WorkItemQueryResult> ExecuteStoredQueryAsync(Guid id, bool? timePrecision = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieve a single WorkItem
        /// </summary>
        /// <param name="id">ID to retrieve</param>
        /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        /// <returns>WorkItem object, null if it doesn't exist</returns>
        Task<WorkItem> GetAsync(int id, DateTime? asOf = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieve a set of WorkItems by ID
        /// </summary>
        /// <param name="ids">IDs to retrieve</param>
        /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        /// <returns>List of WorkItems matching Ids</returns>
        Task<IEnumerable<WorkItem>> GetAsync(IEnumerable<int> ids, DateTime? asOf = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a linked query and returns the relationships.
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="top"># of items to retrieve, pass null for all matching (up to 20000)</param>
        /// <param name="timePrecision">True to match Date/Time vs. just date</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A set of relationship links (source/target/relationship) based on the query</returns>
        Task<IEnumerable<WorkItemLink>> QueryLinkedRelationshipsAsync(string query, int? top = null, bool? timePrecision = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all the related WorkItem ids to a given WorkItem.
        /// </summary>
        /// <param name="id">ID of the WorkItem</param>
        /// <param name="relationship">Relationship to query for</param>
        /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        /// <returns>List of related WorkItem ids</returns>
        Task<IReadOnlyList<RelationLinks>> GetRelatedIdsAsync(int id, Relationship relationship, DateTime? asOf = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the parent WorkItem for the given child.
        /// </summary>
        /// <param name="child">Child WorkItem</param>
        /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        /// <returns>Parent, or <see langword="null"/> if none.</returns>
        Task<WorkItem> GetParentAsync(WorkItem child, DateTime? asOf = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the related WorkItems for the given item.
        /// </summary>
        /// <param name="owner">Owning WorkItem</param>
        /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        /// <returns>Related items, empty if none.</returns>
        Task<IEnumerable<WorkItem>> GetRelatedAsync(WorkItem owner, DateTime? asOf = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the child WorkItems for the given item.
        /// </summary>
        /// <param name="parent">Parent WorkItem</param>
        /// <param name="asOf">Optional date - if supplied, the state if the WorkItem will be as of the given date.</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        /// <returns>Related items, empty if none.</returns>
        Task<IEnumerable<WorkItem>> GetChildrenAsync(WorkItem parent, DateTime? asOf = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new child to a WorkItem
        /// </summary>
        /// <param name="parent">Parent</param>
        /// <param name="child">Child</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddChildAsync(WorkItem parent, WorkItem child, CancellationToken cancellationToken = default);


        /// <summary>
        /// Add a new child to a WorkItem
        /// </summary>
        /// <param name="parent">Parent</param>
        /// <param name="child">Child</param>
        /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddChildAsync(WorkItem parent, WorkItem child, bool bypassRules = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add multiple children to a WorkItem
        /// </summary>
        /// <param name="parent">Parent</param>
        /// <param name="children">Child work items</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddChildrenAsync(WorkItem parent, IEnumerable<WorkItem> children, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add multiple children to a WorkItem
        /// </summary>
        /// <param name="parent">Parent</param>
        /// <param name="children">Child work items</param>
        /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddChildrenAsync(WorkItem parent, IEnumerable<WorkItem> children, bool bypassRules = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new related item to a WorkItem
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relatedItem">Related Work Item</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddRelatedAsync(WorkItem owner, WorkItem relatedItem, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new related item to a WorkItem
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relatedItem">Related Work Item</param>
        /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddRelatedAsync(WorkItem owner, WorkItem relatedItem, bool bypassRules = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add multiple related items to a WorkItem
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relatedItems">Related Work Items</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddRelatedAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add multiple related items to a WorkItem
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relatedItems">Related Work Items</param>
        /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddRelatedAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, bool bypassRules = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new related item to a WorkItem
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relationshipType">Relationship type to create</param>
        /// <param name="relatedItem">Related Work Item</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, WorkItem relatedItem, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a new related item to a WorkItem
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relationshipType">Relationship type to create</param>
        /// <param name="relatedItem">Related Work Item</param>
        /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, WorkItem relatedItem, bool bypassRules = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add multiple related items to a WorkItem
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relationshipType">Relationship type to create</param>
        /// <param name="relatedItems">Related Work Items</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add multiple related items to a WorkItem
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relationshipType">Relationship type to create</param>
        /// <param name="relatedItems">Related Work Items</param>
        /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, IEnumerable<WorkItem> relatedItems, bool bypassRules = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove a related item (child, related)
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relatedItem">Related item to remove</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task RemoveRelationshipAsync(WorkItem owner, WorkItem relatedItem, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove a set of related items (child, related)
        /// </summary>
        /// <param name="owner">Owner object</param>
        /// <param name="relatedItems">Related items to remove</param>
        /// <param name="cancellationToken">Optional cancelation token</param>
        Task RemoveRelationshipAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a string-based query and return a set of Work Items matching the query
        /// </summary>
        /// <param name="query">Query text. Must be in the WIQL syntax. See https://docs.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops</param>
        /// <param name="top"># of items to retrieve, pass null for all matching (up to 20000)</param>
        /// <param name="timePrecision">True to match Date/Time vs. just date</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Matching Work Item objects. Note that the actual returned types will match registered types if possible.</returns>
        Task<IEnumerable<WorkItem>> QueryAsync(string query, int? top = null, bool? timePrecision = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a string-based query and return a set of Work Items matching the query
        /// </summary>
        /// <typeparam name="T">Type of WorkItem object to return</typeparam>
        /// <param name="query">Query text. Must be in the WIQL syntax. See https://docs.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops</param>
        /// <param name="top"># of items to retrieve, pass null for all matching (up to 20000)</param>
        /// <param name="timePrecision">True to match Date/Time vs. just date</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Matching Work Item objects.</returns>
        Task<IEnumerable<T>> QueryAsync<T>(string query, int? top = null, bool? timePrecision = null, CancellationToken cancellationToken = default) where T : WorkItem, new();

        /// <summary>
        /// Add a new Work Item to the system
        /// </summary>
        /// <param name="workItem">WorkItem to add. Can be a derived class. Must be a new object.</param>
        /// <param name="comment">Optional comment to add to the created item.</param>
        /// <param name="validateOnly">True to validate the creation only. If not supplied will use the global ValidateOnly property.</param>
        /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <remarks>The passed WorkItem is updated based on the response from the server.</remarks>
        Task AddAsync(WorkItem workItem, string comment = null, bool? validateOnly = null, bool? bypassRules = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing Work Item to the system
        /// </summary>
        /// <param name="workItem">WorkItem to update. Can be a derived class. Cannot be a new object.</param>
        /// <param name="comment">Optional comment to add to the created item.</param>
        /// <param name="validateOnly">True to validate the creation only. If not supplied will use the global ValidateOnly property.</param>
        /// <param name="bypassRules"><see langword="true"/>to bypass any WorkItem rules</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <remarks>The passed WorkItem is updated based on the response from the server.</remarks>
        Task UpdateAsync(WorkItem workItem, string comment = null, bool? validateOnly = null, bool? bypassRules = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a WorkItem from the system.
        /// </summary>
        /// <param name="id">ID of the WorkItem to remove.</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True/False success code.</returns>
        /// <remarks>The WorkItem is not permanently deleted but is placed in the recycle bin.</remarks>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}