using System.Collections.Generic;
using System.Threading.Tasks;
using Julmar.AzDOUtilities.Interfaces;

namespace Julmar.AzDOUtilities
{
    public interface IAzureDevOpsService : IAzureDevOpsServiceCommon
    {
        Task AddChildrenAsync(WorkItem parent, params WorkItem[] children);
        Task AddRelatedAsync(WorkItem owner, params WorkItem[] relatedItems);
        Task AddRelationshipAsync(Relationship relationshipType, WorkItem owner, params WorkItem[] relatedItems);
        Task<IEnumerable<WorkItem>> GetRelatedAsync(WorkItem owner);
        Task<IEnumerable<WorkItem>> GetChildrenAsync(WorkItem parent);

        Task<IEnumerable<WorkItem>> QueryAsync(string query);
        Task<IEnumerable<T>> QueryAsync<T>(string query) where T : WorkItem, new();

        Task<WorkItem> GetAsync(int id);
        Task AddAsync(WorkItem workItem, string comment = null, bool? validateOnly = null, bool? bypassRules = null);
        Task UpdateAsync(WorkItem workItem, string comment = null, bool? validateOnly = null, bool? bypassRules = null);
        Task<bool> DeleteAsync(int id);
    }
}