using Julmar.AzDOUtilities;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using WorkItem = Julmar.AzDOUtilities.WorkItem;

// Get the token to use.
string tokenFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "vsts-rw-key.txt");
string token = File.ReadAllText(tokenFile);

// TODO: change based on test.
string url = "https://julmar.visualstudio.com/";
string project = "Test";

// Get the service
var service = AzureDevOpsFactory.Create(url, token);
var queryProvider = AzureDevOpsFactory.CreateQueryable<WorkItem>(service, project);

// Setup tracing
service.TraceLog = Console.WriteLine;
service.TraceLevel = LogLevel.Query | LogLevel.EnterExit | LogLevel.RawApis;

TryLinq(queryProvider);

void TryLinq(IOrderedQueryable<WorkItem> queryProvider)
{
    var items = queryProvider
                .Where(e => 
                            e.State == "Concept" 
                            && e.CreatedDate >= DateTime.Now.AddDays(-30)
                            && e.WorkItemType == "Epic")
                .Take(5)
                .ToList();

    foreach (var item in items)
    {
        DumpWorkItem(item);
    }
}

async Task DumpQueries(IAzureDevOpsService service)
{
    QueryHierarchyItem? query = null;

    var queries = await service.GetStoredQueriesAsync(project, 1);
    foreach (var item in queries)
    {
        Console.WriteLine(item.Name);
        if (item.IsPublic == false)
        {
            var detail = await service.GetStoredQueryDetailsAsync(project, item, 2);
            foreach (var child in detail.Children)
            {
                Console.WriteLine("\t" + child.Name);
            }

            query = detail.Children.First();
        }
    }

    if (query != null)
    {
        var results = await service.ExecuteStoredQueryAsync(query.Id);
        await DumpWorkItems(service, results.WorkItems);
    }
}

async Task DumpWorkItems(IAzureDevOpsService service, IEnumerable<WorkItemReference> workItems)
{
    foreach (var item in workItems)
    {
        var workItem = await service.GetAsync(item.Id);
        if (workItem != null)
            DumpWorkItem(workItem);
    }
}

void DumpWorkItem(WorkItem workItem)
{
    Console.WriteLine($"{workItem.Id} - \"{workItem.Title}\"");
}

async Task DumpAreaPaths(IAzureDevOpsService service)
{
    var areas = await service.GetAreasAsync(project, 2);
    foreach (var area in areas)
    {
        Console.WriteLine(area.Name);
        if (area.HasChildren == true && area.Children != null)
        {
            foreach (var child in area.Children)
            {
                Console.WriteLine("\t"+child.Name);
            }
        }
    }
}

async Task DumpIterations(IAzureDevOpsService service)
{
    var areas = await service.GetIterationsAsync(project, 2);
    foreach (var area in areas)
    {
        Console.WriteLine(area.Name);
        if (area.HasChildren == true && area.Children != null)
        {
            foreach (var child in area.Children)
            {
                Console.WriteLine("\t" + child.Name);
            }
        }
    }
}
