using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Julmar.AzDOUtilities.Interfaces
{
    public interface IAzureDevOpsRawService : IAzureDevOpsServiceCommon
    {
        Task<IEnumerable<WorkItemUpdate>> GetItemUpdatesAsync(int id, int? top = null, int? skip = null);
        Task<IEnumerable<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem>> QueryAsync(string query, string[] fields);
        Task<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> GetAsync(int id);
        Task<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> AddAsync(JsonPatchDocument patchDocument, string projectName, string workItemType, bool? validateOnly = null, bool? bypassRules = null);
        Task<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> UpdateAsync(int id, JsonPatchDocument patchDocument, bool? validateOnly = null, bool? bypassRules = null);
        Task<bool> DeleteAsync(int id);
    }
}