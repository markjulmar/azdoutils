﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Julmar.AzDOUtilities
{
    sealed partial class AzDOService : IAzureDevOpsService
    {
        public WorkItemTrackingHttpClient WorkItemClient => httpClient.Value;
        public VssConnection Connection => connection.Value;
        public ProjectHttpClient ProjectClient => projectClient.Value;
        public WorkItemErrorPolicy? ErrorPolicy { get; set; }
        public bool ValidateOnly { get; set; }
        
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

        public async Task<IEnumerable<WorkItemClassificationNode>> GetAreasAsync(string projectName, int? depth, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { projectName, depth, cancellationToken });

            var nodes = new List<WorkItemClassificationNode>();
            var project = await ProjectClient.GetProject(projectName).ConfigureAwait(false);
            var currentIteration = await WorkItemClient.GetClassificationNodeAsync(project.Name, TreeStructureGroup.Areas,
                                                path: null, depth, userState: null, cancellationToken).ConfigureAwait(false);
            AddChildIterations(nodes, currentIteration);

            return nodes;
        }

        public async Task<IEnumerable<WorkItemClassificationNode>> GetIterationsAsync(string projectName, int? depth, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { projectName, depth, cancellationToken });

            var nodes = new List<WorkItemClassificationNode>();
            var project = await ProjectClient.GetProject(projectName).ConfigureAwait(false);
            var currentIteration = await WorkItemClient.GetClassificationNodeAsync(project.Name, TreeStructureGroup.Iterations,
                                                path: null, depth, userState: null, cancellationToken).ConfigureAwait(false);
            AddChildIterations(nodes, currentIteration);

            return nodes;
        }

        private void AddChildIterations(List<WorkItemClassificationNode> nodes, WorkItemClassificationNode currentIteration)
        {
            nodes.Add(currentIteration);
            if (currentIteration.Children != null)
            {
                foreach (var child in currentIteration.Children)
                {
                    AddChildIterations(nodes, child);
                }
            }
        }

        public Task<IReadOnlyList<RelationLinks>> GetRelatedIdsAsync(int id, Relationship relationshipType, DateTime? asOf, CancellationToken cancellationToken)
        {
            if (relationshipType == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationshipType))
            {
                throw new ArgumentOutOfRangeException(nameof(relationshipType));
            }

            using var mc = log?.Enter(LogLevel.RelatedApis, new object[] { id, asOf, cancellationToken });
            return InternalGetRelatedIdsAsync(id, RelationshipLinkText[(int)relationshipType], asOf, cancellationToken);
        }

        public async Task<IEnumerable<QueryHierarchyItem>> GetStoredQueriesAsync(string projectName, bool? includeDeleted, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { projectName, includeDeleted, cancellationToken });
            var results = await WorkItemClient.GetQueriesAsync(projectName, QueryExpand.All, depth: 2, includeDeleted, userState: null, cancellationToken);

            for (int i = 0; i < results.Count; i++)
            {
                QueryHierarchyItem item = results[i];
                if (item.HasChildren == true)
                {
                    results[i] = await InternalGetStoredQueriesAsync(projectName, item, includeDeleted, cancellationToken);
                }
            }

            return results;
        }

        public async Task<WorkItemQueryResult> ExecuteStoredQueryAsync(Guid id, bool? timePrecision, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { id, timePrecision, cancellationToken });
            return await WorkItemClient.QueryByIdAsync(id, timePrecision, userState: null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<WorkItem> GetAsync(int id, DateTime? asOf, CancellationToken cancellationToken)
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

        public async Task<IEnumerable<WorkItem>> GetAsync(IEnumerable<int> ids, DateTime? asOf, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { ids, asOf, cancellationToken });
            var wits = await InternalGetWitsByIdChunked(ids.ToList(), ReflectionHelpers.GetAllFields(this),
                                    asOf, WorkItemExpand.None, ErrorPolicy, cancellationToken).ConfigureAwait(false);
            return ReflectionHelpers.MapWorkItemTypes(wits);
        }

        public Task AddChildAsync(WorkItem parent, WorkItem child, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { parent, child, cancellationToken  });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Child], parent, new[] { child }, true, cancellationToken);
        }

        public Task AddChildAsync(WorkItem parent, WorkItem child, bool bypassRules, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { parent, child, bypassRules, cancellationToken });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Child], parent, new[] { child }, bypassRules, cancellationToken);
        }

        public Task AddChildrenAsync(WorkItem parent, IEnumerable<WorkItem> children, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { parent, children, cancellationToken });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Child], parent, children, true, cancellationToken);
        }

        public Task AddChildrenAsync(WorkItem parent, IEnumerable<WorkItem> children, bool bypassRules, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { parent, children, bypassRules, cancellationToken });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Child], parent, children, bypassRules, cancellationToken);
        }

        public Task AddRelatedAsync(WorkItem owner, WorkItem relatedItem, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { owner, relatedItem, cancellationToken });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Related], owner, new[] { relatedItem }, true, cancellationToken);
        }

        public Task AddRelatedAsync(WorkItem owner, WorkItem relatedItem, bool bypassRules, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { owner, relatedItem, bypassRules, cancellationToken });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Related], owner, new[] { relatedItem }, bypassRules, cancellationToken);
        }

        public Task AddRelatedAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { owner, relatedItems, cancellationToken });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Related], owner, relatedItems, true, cancellationToken);
        }

        public Task AddRelatedAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, bool bypassRules, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { owner, relatedItems, bypassRules, cancellationToken });
            return InternalAddRelationshipAsync(RelationshipLinkText[(int)Relationship.Related], owner, relatedItems, bypassRules, cancellationToken);
        }

        public Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, WorkItem relatedItem, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { relationshipType, owner, relatedItem, cancellationToken });

            if (relationshipType == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationshipType))
                throw new ArgumentOutOfRangeException(nameof(relationshipType));

            return InternalAddRelationshipAsync(RelationshipLinkText[(int)relationshipType], owner, new[] { relatedItem }, true, cancellationToken);
        }

        public Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, WorkItem relatedItem, bool bypassRules, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { relationshipType, owner, relatedItem, bypassRules, cancellationToken });

            if (relationshipType == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationshipType))
                throw new ArgumentOutOfRangeException(nameof(relationshipType));

            return InternalAddRelationshipAsync(RelationshipLinkText[(int)relationshipType], owner, new[] { relatedItem }, bypassRules, cancellationToken);
        }


        public Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { relationshipType, owner, relatedItems, cancellationToken });

            if (relationshipType == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationshipType))
                throw new ArgumentOutOfRangeException(nameof(relationshipType));

            return InternalAddRelationshipAsync(RelationshipLinkText[(int)relationshipType], owner, relatedItems, true, cancellationToken);
        }

        public Task AddRelationshipAsync(WorkItem owner, Relationship relationshipType, IEnumerable<WorkItem> relatedItems, bool bypassRules, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { relationshipType, owner, relatedItems, bypassRules, cancellationToken });

            if (relationshipType == Relationship.Other || !Enum.IsDefined(typeof(Relationship), relationshipType))
                throw new ArgumentOutOfRangeException(nameof(relationshipType));

            return InternalAddRelationshipAsync(RelationshipLinkText[(int)relationshipType], owner, relatedItems, bypassRules, cancellationToken);
        }


        public Task RemoveRelationshipAsync(WorkItem owner, WorkItem relatedItem, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { owner, relatedItem, cancellationToken });
            return InternalRemoveRelationshipAsync(owner, new[] { relatedItem }, cancellationToken);
        }

        public Task RemoveRelationshipAsync(WorkItem owner, IEnumerable<WorkItem> relatedItems, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { owner, relatedItems, cancellationToken });
            return InternalRemoveRelationshipAsync(owner, relatedItems, cancellationToken);
        }

        public async Task<IEnumerable<WorkItemLink>> QueryLinkedRelationshipsAsync(string query, int? top, bool? timePrecision, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { query, top, timePrecision, cancellationToken });

            var wiql = new Wiql { Query = query };

            var results = await WorkItemClient.QueryByWiqlAsync(wiql, timePrecision, top, userState: null, cancellationToken)
                                              .ConfigureAwait(false);
            return results.WorkItemRelations ?? Enumerable.Empty<WorkItemLink>();
        }

        public async Task<IEnumerable<WorkItem>> GetRelatedAsync(WorkItem owner, DateTime? asOf, CancellationToken cancellationToken)
        {
            if (owner is null)
                throw new ArgumentNullException(nameof(owner));
            if (owner.IsNew)
                throw new ArgumentException("No relationships on a new WorkItem");

            using var mc = log?.Enter(new object[] { owner.Id });

            var relatedIds = (await InternalGetRelatedIdsAsync(owner.Id.Value, RelationshipLinkText[(int)Relationship.Related], asOf, cancellationToken)
                                            .ConfigureAwait(false)).Where(rid => rid.RelatedId.HasValue).Select(rid => rid.RelatedId.Value).ToList();
            var wits = await InternalGetWitsByIdChunked(relatedIds, ReflectionHelpers.GetAllFields(this), asOf,
                                                        WorkItemExpand.None, ErrorPolicy, cancellationToken)
                                            .ConfigureAwait(false);
            return ReflectionHelpers.MapWorkItemTypes(wits);
        }

        public async Task<IEnumerable<WorkItem>> QueryAsync(string query, int? top, bool? timePrecision, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { query, top, timePrecision, cancellationToken });

            var workItems = await InternalGetWorkItemsAsync(query, ReflectionHelpers.GetAllFields(this),
                                                            top, timePrecision, WorkItemExpand.None, ErrorPolicy, cancellationToken)
                                        .ConfigureAwait(false);
            return ReflectionHelpers.MapWorkItemTypes(workItems);
        }

        internal async Task<IEnumerable<WorkItem>> QueryForTypeAsync(Type expectedType, string query, int? top, bool? timePrecision, CancellationToken cancellationToken)
        {
            // This version is used by LINQ to constrain the fields to what's on the type.
            // But we can't add constraints to QueryProvider<T> so we pass the type in here
            // and constrain it directly
            using var mc = log?.Enter(new object[] { query, top, timePrecision, cancellationToken }, nameof(QueryAsync));

            var typeFields = (expectedType == typeof(WorkItem))
                ? ReflectionHelpers.GetAllFields(this)
                : ReflectionHelpers.GetQueryFieldsForType(expectedType).FilterFields(this).ToArray();

            var workItems = await InternalGetWorkItemsAsync(query, typeFields,
                                                            top, timePrecision, WorkItemExpand.None, ErrorPolicy, cancellationToken)
                                        .ConfigureAwait(false);

            return ReflectionHelpers.MapWorkItemTypes(workItems);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query, int? top, bool? timePrecision, CancellationToken cancellationToken) where T : WorkItem, new()
        {
            using var mc = log?.Enter(new object[] { query, top, timePrecision, cancellationToken });

            var typeFields =  (typeof(T) == typeof(WorkItem))
                ? ReflectionHelpers.GetAllFields(this)
                : ReflectionHelpers.GetQueryFieldsForType(typeof(T)).FilterFields(this).ToArray();
            var workItems = await InternalGetWorkItemsAsync(query, typeFields,
                                                            top, timePrecision, WorkItemExpand.None, ErrorPolicy, cancellationToken)
                                        .ConfigureAwait(false);
            return workItems.Select(wi => ReflectionHelpers.FromWorkItem<T>(wi));
        }

        public async Task<WorkItem> GetParentAsync(WorkItem child, DateTime? asOf, CancellationToken cancellationToken)
        {
            if (child is null)
                throw new ArgumentNullException(nameof(child));
            if (child.IsNew)
                throw new ArgumentException("No children for a new WorkItem.");

            using var mc = log?.Enter(new object[] { child, asOf, cancellationToken });

            var parentRelationship = (await InternalGetRelatedIdsAsync(child.Id.Value, RelationshipLinkText[(int)Relationship.Parent],
                                                             asOf, cancellationToken).ConfigureAwait(false)).SingleOrDefault();
            if (parentRelationship?.RelatedId == null)
                return null;

            var wit = await WorkItemClient.GetWorkItemAsync(parentRelationship.RelatedId.Value, ReflectionHelpers.GetAllFields(this), asOf,
                                    WorkItemExpand.None, userState: null, cancellationToken).ConfigureAwait(false);

            return ReflectionHelpers.MapWorkItemTypes(new[] { wit }).Single();
        }

        public async Task<IEnumerable<WorkItem>> GetChildrenAsync(WorkItem parent, DateTime? asOf, CancellationToken cancellationToken)
        {
            if (parent is null)
                throw new ArgumentNullException(nameof(parent));

            if (parent.IsNew)
                throw new ArgumentException("No children for a new WorkItem.");

            using var mc = log?.Enter(new object[] { parent, asOf, cancellationToken });

            var childIds = (await InternalGetRelatedIdsAsync(parent.Id.Value, RelationshipLinkText[(int)Relationship.Child],
                                                             asOf, cancellationToken).ConfigureAwait(false))
                                        .Where(rid => rid.RelatedId.HasValue).Select(rid => rid.RelatedId.Value).ToList();
            var wits = await InternalGetWitsByIdChunked(childIds, ReflectionHelpers.GetAllFields(this), asOf,
                                                        WorkItemExpand.None, ErrorPolicy, cancellationToken).ConfigureAwait(false);

            return ReflectionHelpers.MapWorkItemTypes(wits);
        }

        public async Task AddAsync(WorkItem workItem, string comment, bool? validateOnly, bool? bypassRules, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { workItem, comment, validateOnly, bypassRules, cancellationToken });

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

            if (!string.IsNullOrWhiteSpace(comment))
            {
                workItem.AddCommentToHistory(comment);
            }

            var patchDocument = workItem.CreatePatchDocument();
            if (patchDocument == null)
            {
                throw new Exception("Patch document couldn't be created from Workitem.");
            }

            var wit = await this.AddAsync(patchDocument, projectName, workItemType, validateOnly, bypassRules, cancellationToken)
                                .ConfigureAwait(false);
            log?.WriteLine(LogLevel.PatchDocument, $"WorkItem {workItem.Id} updated from Rev {workItem.Revision} to {wit.Rev}");
            workItem.Initialize(wit);
        }

        public async Task UpdateAsync(WorkItem workItem, string comment, bool? validateOnly, bool? bypassRules, CancellationToken cancellationToken)
        {
            using var mc = log?.Enter(new object[] { workItem, comment, validateOnly, bypassRules, cancellationToken });

            if (workItem.IsNew)
                throw new ArgumentException("Cannot update a new WorkItem.", nameof(workItem));

            if (!workItem.HasChanges)
                return;

            if (!string.IsNullOrWhiteSpace(comment))
            {
                workItem.AddCommentToHistory(comment);
            }

            var patchDocument = workItem.CreatePatchDocument();
            if (patchDocument == null)
                return;

            var wit = await this.UpdateAsync(workItem.Id.Value, patchDocument, validateOnly, bypassRules, supressNotifications: null,
                                                WorkItemExpand.Fields, cancellationToken)
                                .ConfigureAwait(false);
            log?.WriteLine(LogLevel.PatchDocument, $"WorkItem {workItem.Id} updated from Rev {workItem.Revision} to {wit.Rev}");
            workItem.Initialize(wit);
        }

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
}
