using System.Linq;

namespace AzDOUtilities
{
    public static class AzureDevOpsFactory
    {
        public static IAzureDevOpsService Create(string uri, string accessToken = null)
            => new AzDOService(uri, accessToken);

        public static IOrderedQueryable<TWorkItem> CreateQueryable<TWorkItem>(IAzureDevOpsService service, string project = null)
            where TWorkItem :  WorkItem
            => new Linq.Queryable<TWorkItem>(service, project);
    }
}
