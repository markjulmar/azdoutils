using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzDOUtilities.Interfaces
{
    // Common methods shared between higher-level IAzureDevOpsService
    // and lower IAzureDevOpsServiceRaw
    public interface IAzureDevOpsServiceCommon
    {
        Action<string> TraceLog { get; set; }
        LogLevel TraceLevel { get; set; }
        bool ValidateOnly { get; set; }

        Task<IEnumerable<TeamProjectReference>> GetProjectsAsync();
        Task<IEnumerable<WorkItemClassificationNode>> GetAreasAsync(string projectName);
        Task<IEnumerable<WorkItemClassificationNode>> GetIterationsAsync(string projectName);
        Task<IEnumerable<WorkItemTypeFieldInstance>> GetWorkItemFieldsAsync(string project, string workItemType);

        Task<int> GetParentIdAsync(int id);
        Task<IReadOnlyList<int>> GetRelatedIdsAsync(int id);
        Task<IReadOnlyList<int>> GetChildIdsAsync(int id);
    }
}