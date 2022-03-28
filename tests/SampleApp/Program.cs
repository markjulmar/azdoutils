using System.Collections;
using Julmar.AzDOUtilities;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using WorkItem = Julmar.AzDOUtilities.WorkItem;

// Get the token to use.
string tokenFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "vsts-rw-key.txt");
string token = File.ReadAllText(tokenFile);

// TODO: change based on test.
//string url = "https://fuzenutrition.visualstudio.com/";
//string project = "Fuze";

string url = "https://ceapex.visualstudio.com";
string project = "Microsoft Learn";

// Get the service
var service = AzureDevOpsFactory.Create(url, token);
var queryProvider = AzureDevOpsFactory.CreateQueryable<WorkItem>(service, project);

// Setup tracing
service.TraceLog = s =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    try
    {
        Console.WriteLine(s);
    }
    finally
    {
        Console.ResetColor();
    }
};

service.TraceLevel = LogLevel.EnterExit | LogLevel.LinqExpression | LogLevel.LinqQuery | LogLevel.Query |
                     LogLevel.RawApis | LogLevel.RelatedApis;

await TryQuery(service);
//TryLinq(queryProvider);

async Task TryQuery(IAzureDevOpsService service)
{
    var results = await service.QueryAsync(
        $@"SELECT [System.Id],[System.Title],[System.State] FROM WorkItems WHERE [System.TeamProject] = '{project}'" +
        @" AND [System.WorkItemType] = 'Module' AND ([System.State] = 'Closed'" +
        @" AND [Microsoft.VSTS.Common.ClosedDate] >= '12/26/2021 12:00:00 AM' )");

    foreach (var item in results)
    {
        Console.WriteLine(item);
        if (item.HasChanges)
        {
            foreach (var change in item.GatherChangeList())
            {
                Console.WriteLine("\t" + change);
            }
        }
    }
}

void TryLinq(IOrderedQueryable<WorkItem> queryProvider)
{
    var items = queryProvider
                .Where(e => e.State == WorkItemStates.New
                        && e.CreatedDate >= DateTime.Now.AddDays(-30))
                .Take(5)
                .ToList();

    foreach (var item in items)
    {
        DumpObject(item);
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
            DumpObject(workItem);
    }
}

void DumpObject(object? value, int level = 1)
{
    if (value == null)
    {
        Console.WriteLine("null");
        return;
    }

    Console.WriteLine(value.ToString());

    string prefix = new string(' ', level * 3);
    foreach (var prop in value.GetType().GetProperties())
    {
        object? child = prop.GetValue(value);
        if (child == null) continue;

        if (prop.PropertyType != typeof(string) && prop.PropertyType.GetInterfaces()
            .Any(t => t.IsGenericType
                      && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            Console.Write(prefix);
            Console.Write($"{prop.Name} = [ ");
            if (child is IEnumerable enumerable)
            {
                int count = 0;
                foreach (object? ev in enumerable)
                {
                    if (count>0) Console.Write(',');
                    Console.Write(ev.ToString());
                    count++;
                }
            }
            else Console.Write(child);
            Console.WriteLine(" ]");
        }
        else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
        {
            Console.Write(prefix);
            Console.WriteLine($"{prop.Name} =");
            DumpObject(child, level+1);
        }
        else
        {
            Console.Write(prefix);
            Console.WriteLine($"{prop.Name} = {child}");
        }
    }

    Console.WriteLine();
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
