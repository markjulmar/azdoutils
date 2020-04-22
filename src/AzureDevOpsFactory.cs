using System.Linq;

namespace Julmar.AzDOUtilities
{
    /// <summary>
    /// Factory to work with Azure DevOps objects.
    /// </summary>
    public static class AzureDevOpsFactory
    {
        /// <summary>
        /// Creates a new IAzureDevOpsService that provides access to the API surface.
        /// </summary>
        /// <param name="uri">URI of the Azure DevOps instance to work with</param>
        /// <param name="accessToken">Access token used to access the system.</param>
        /// <returns>Service object</returns>
        public static IAzureDevOpsService Create(string uri, string accessToken = null)
            => new AzDOService(uri, accessToken);

        /// <summary>
        /// Creates a LINQ queryable on top of a IAzureDevOpsService accessor.
        /// </summary>
        /// <typeparam name="TWorkItem">WorkItem type - must derive from WorkItem</typeparam>
        /// <param name="service">Service object</param>
        /// <param name="project">Optional Azure DevOps project. If supplied, the 'Team Project' clause will always be set.</param>
        /// <returns>Queryable collection</returns>
        public static IOrderedQueryable<TWorkItem> CreateQueryable<TWorkItem>(IAzureDevOpsService service, string project = null)
            where TWorkItem :  WorkItem
            => new Linq.Queryable<TWorkItem>(service, project);
    }
}
