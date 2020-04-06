using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzDOUtilities.Interfaces;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace AzDOUtilities
{
    sealed partial class AzDOService : IAzureDevOpsRawService
    {
        private WorkItemTrackingHttpClient CreateWorkItemClient()
        {
            return new WorkItemTrackingHttpClient(new Uri(uri),
                new VssBasicCredential(string.Empty, accessToken));
        }

        private ProjectHttpClient CreateProjectClient()
        {
            return new ProjectHttpClient(new Uri(uri),
                new VssBasicCredential(string.Empty, accessToken));
        }

        public async Task<IEnumerable<TeamProjectReference>> GetProjectsAsync()
        {
            using var mc = log?.Enter(LogLevel.EnterExitRaw);
            using var client = CreateProjectClient();
            return await client.GetProjects();
        }

        public async Task<IEnumerable<WorkItemClassificationNode>> GetAreasAsync(string projectName)
        {
            using var mc = log?.Enter(projectName);
            List<WorkItemClassificationNode> nodes = new List<WorkItemClassificationNode>();

            using (var client = CreateProjectClient())
            using (var workItemTracking = CreateWorkItemClient())
            {
                TeamProjectReference project = await client.GetProject(projectName);
                WorkItemClassificationNode currentIteration = await workItemTracking.GetClassificationNodeAsync(
                            project.Name, TreeStructureGroup.Areas, depth: 50);
                AddChildIterations(nodes, currentIteration);
            }

            return nodes;
        }

        public async Task<IEnumerable<WorkItemClassificationNode>> GetIterationsAsync(string projectName)
        {
            using var mc = log?.Enter(LogLevel.EnterExitRaw, projectName);
            List<WorkItemClassificationNode> nodes = new List<WorkItemClassificationNode>();

            using (var client = CreateProjectClient())
            using (var workItemTracking = CreateWorkItemClient())
            {
                TeamProjectReference project = await client.GetProject(projectName);
                WorkItemClassificationNode currentIteration = await workItemTracking.GetClassificationNodeAsync(
                            project.Name, TreeStructureGroup.Iterations, depth: 20);
                AddChildIterations(nodes, currentIteration);
            }
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

        public async Task<IEnumerable<WorkItemTypeFieldInstance>> GetWorkItemFieldsAsync(string project, string workItemType)
        {
            using var mc = log?.Enter(LogLevel.EnterExitRaw, new object[] { project, workItemType });
            using var client = CreateWorkItemClient();
            return await client.GetWorkItemTypeFieldsAsync(project, workItemType, WorkItemTypeFieldsExpandLevel.All).ConfigureAwait(false);
        }

        static string StripRevisionFromUrl(string url)
        {
            const string revisions = "/revisions/";
            if (url.Contains(revisions))
            {
                int index = url.LastIndexOf(revisions, StringComparison.Ordinal);
                url = url.Substring(0, index);
            }
            return url;
        }

        public async Task<IReadOnlyList<int>> GetRelatedIdsAsync(int id)
        {
            using var mc = log?.Enter(LogLevel.RelatedApis, id);
            using var client = CreateWorkItemClient();
            Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem workItem = await client.GetWorkItemAsync(id, expand: WorkItemExpand.Relations).ConfigureAwait(false);
            if (workItem.Relations == null)
            {
                return new List<int>();
            }

            List<int> list = new List<int>();
            foreach (var relation in workItem.Relations)
            {
                //get the child links
                if (relation.Rel == "System.LinkTypes.Related")
                {
                    var lastIndex = relation.Url.LastIndexOf("/", StringComparison.Ordinal);
                    var itemId = relation.Url.Substring(lastIndex + 1);
                    list.Add(Convert.ToInt32(itemId));
                }
            }
            return list;
        }

        public async Task<int> GetParentIdAsync(int id)
        {
            using var mc = log?.Enter(LogLevel.EnterExitRaw, id);
            using var client = CreateWorkItemClient();
            Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem workItem = await client.GetWorkItemAsync(id, expand: WorkItemExpand.Relations).ConfigureAwait(false);
            var relation = workItem.Relations?.SingleOrDefault(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse");
            if (relation != null)
            {
                var lastIndex = relation.Url.LastIndexOf("/", StringComparison.Ordinal);
                var itemId = relation.Url.Substring(lastIndex + 1);
                return Convert.ToInt32(itemId);
            }

            return -1;
        }

        public async Task<IReadOnlyList<int>> GetChildIdsAsync(int id)
        {
            using var mc = log?.Enter(LogLevel.RelatedApis, id);
            using var client = CreateWorkItemClient();
            Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem workItem = await client.GetWorkItemAsync(id, expand: WorkItemExpand.Relations).ConfigureAwait(false);
            if (workItem.Relations == null)
                return new List<int>();

            List<int> list = new List<int>();
            foreach (var relation in workItem.Relations)
            {
                //get the child links
                if (relation.Rel == "System.LinkTypes.Hierarchy-Forward")
                {
                    var lastIndex = relation.Url.LastIndexOf("/", StringComparison.Ordinal);
                    var itemId = relation.Url.Substring(lastIndex + 1);
                    list.Add(Convert.ToInt32(itemId));
                }
            }
            return list;
        }

        private async Task<IEnumerable<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem>> GetWorkItemsAsync(WorkItemTrackingHttpClient client, List<int> ids, string[] fields, DateTime? asOf)
        {
            const int nSize = 200;
            List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> workItems = new List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem>();

            // Chunk the retrieval.
            for (int i = 0; i < ids.Count; i += nSize)
            {
                var range = ids.GetRange(i, Math.Min(nSize, ids.Count - i));
                var results = await client.GetWorkItemsAsync(range, fields, asOf).ConfigureAwait(false);

                workItems.AddRange(results);
            }

            return workItems;
        }

        public async Task<IEnumerable<WorkItemUpdate>> GetItemUpdatesAsync(int id, int? top = null, int? skip = null)
        {
            using var mc = log?.Enter(LogLevel.EnterExitRaw, new object[] { id, top, skip });
            using var client = CreateWorkItemClient();
            return await client.GetUpdatesAsync(id, top, skip);
        }

        public async Task<IEnumerable<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem>> QueryAsync(string query, string[] fields)
        {
            using var mc = log?.Enter(LogLevel.EnterExitRaw, new object[] { query, fields });
            Wiql wiql = new Wiql { Query = query };
            using var client = CreateWorkItemClient();

            // Return a list of URLs + Ids for matching workItems.
            WorkItemQueryResult queryResult = await client.QueryByWiqlAsync(wiql).ConfigureAwait(false);
            if (queryResult.WorkItems.Any())
            {
                var ids = queryResult.WorkItems.Select(wi => wi.Id).ToList();

                // Get the actual work items from the IDs; chunked.
                return await GetWorkItemsAsync(client, ids, fields, queryResult.AsOf).ConfigureAwait(false);
            }

            return Enumerable.Empty<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem>();
        }

        async Task<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> IAzureDevOpsRawService.GetAsync(int id)
        {
            using var mc = log?.Enter(LogLevel.EnterExitRaw, id);
            using var client = CreateWorkItemClient();
            return await client.GetWorkItemAsync(id, expand: WorkItemExpand.All).ConfigureAwait(false); ;
        }

        public async Task<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> AddAsync(JsonPatchDocument patchDocument, string projectName, string workItemType, bool? validateOnly, bool? bypassRules)
        {
            if (this.ValidateOnly)
                validateOnly = true;

            using var mc = log?.Enter(LogLevel.EnterExitRaw, new object[] { $"projectName:{projectName}", $"workItemType:{workItemType}", $"validateOnly:{validateOnly}", $"bypassRules:{bypassRules}" });
            log?.Dump(patchDocument);

            using var client = CreateWorkItemClient();
            return await client.CreateWorkItemAsync(patchDocument, projectName, workItemType, validateOnly, bypassRules);
        }

        public async Task<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> UpdateAsync(int id, JsonPatchDocument patchDocument, bool? validateOnly, bool? bypassRules)
        {
            if (this.ValidateOnly)
                validateOnly = true;

            using var mc = log?.Enter(LogLevel.EnterExitRaw, new object[] { $"id:{id}", $"validateOnly:{validateOnly}", $"bypassRules:{bypassRules}" });
            log?.Dump(patchDocument);

            using var client = CreateWorkItemClient();
            return await client.UpdateWorkItemAsync(patchDocument, id, validateOnly, bypassRules);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var mc = log?.Enter(LogLevel.EnterExitRaw, new object[] { $"id:{id}", $"validateOnly:{ValidateOnly}" });

            if (this.ValidateOnly)
                return false;

            using var client = CreateWorkItemClient();
            var wit = await client.DeleteWorkItemAsync(id, destroy: false).ConfigureAwait(false);
            return wit.Id == id;
        }
    }
}
