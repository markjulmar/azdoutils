# Azure DevOps Utilities

![.NET Core](https://github.com/markjulmar/azdoutils/workflows/.NET%20Core/badge.svg)

The AzDOUtilities library provides a lightweight wrapper around the Azure DevOps REST API. It gives access to a high-level API through the `IAzureDevOpsService` interface. The service works with typed wrapper objects (`AzDOUtilities.WorkItem`) with change tracking support.

It's packaged as a [NuGet package](https://www.nuget.org/packages/Julmar.AzDOUtilities/). You can add it to your project with the following command.

```bash
dotnet add package Julmar.AzDOUtilities --version 1.6.1-prerelease
```

## Release notes

| Version | Changes  |
|---------|----------|
| **1.6.1-pre** | Added `ValidFields` to `WorkItem` type to retrieve field names. Added `Connection` to provide underlying access to the `VssConnection` to retrieve other types. |
| **1.6-pre** | Added new `QueryLinkedRelationshipsAsync` method to retrieve work item links. |
| **1.5.2-pre** | Added new `GetAsync` to retrieve a set of Ids. |
| **1.5.1-pre** | Updated TeamFoundation package to latest. |
| **1.5-pre**  | Optimize the LINQ query parser to support `Take` and restrict the fields to the queried type if possible. |
| **1.4-pre**  | Some refactoring - removed the raw interface. |
| **1.1-pre**  | Added `Relationship` enum and new `IAzureDevOpsService.AddRelationshipAsync` method and moved to Julmar.AzDOUtilities. |
| **1.01-pre** | Optimized some calling paths for async. |
| **1.0-pre**  | Initial public release. |


## AzureDevOpsFactory

The `AzureDevOpsFactory` is the starting point for accessing Azure DevOps data with .NET. The `Create` method returns a `IAzureDevOpsService` object and takes a URL to the AzDO site and an access token.

```csharp
using Julmar.AzDOUtilities;

...

IAzureDevOpsService service = AzureDevOpsFactory.Create("https://myvsts.microsoft.com/", accessToken);

var workItems = await service.QueryAsync("SELECT [Id] FROM [WorkItems] WHERE [System.TeamProject] = 'MyProject'"
	                                   + " AND [System.State] = 'Closed'");
foreach (var wi in workItems)
{
	Console.WriteLine($"WorkItem: {wi.Id} - {wi.Title} assigned to {wi.AssignedTo}.");
}
```

## LINQ queries

There's a LINQ provider built into the library which allows you to work directly with the wrapper objects as `IQueryable<T>` types. Most expressions are translated directly to Azure DevOps queries, and what cannot be translated is handled locally on the returned items as part of the processing. This support let's you work with either the fluent method syntax or C# LINQ syntax.

To get a queryable collection, use the `AzureDevOpsFactory.CreateQuery<T>` method. It takes an `IAzureDevOpsService` instance and an optional project name. The provider will automatically supply the `[System.TeamProject]` part of the `WHERE` clause if you supply that parameter. If you pass `null` for that parameter, you will need to add the project to each `Where()` clause.

The `T` generic parameter must be a `AzDOUtilities.WorkItem` type. You can supply a specific wrapper to access a custom WorkItem type as part of your project, or use the base `WorkItem` type to pull multiple types.

```csharp
var db = AzureDevOpsFactory.CreateQuery<WorkItem>(service, "MyProject");

var query = db.Where(wi => wi.State == "Closed");
var workItems = query.ToList(); // query executed here.

...

var query = from wi in db
   	        where wi.State == "Closed"
   	        order by wi.Id
   	        select wi.Id;
var workItems = query.ToList(); // query executed here.
```

### Creating a custom WorkItem wrapper object

You can define your own WorkItem-type by deriving a class from the `AzDOUtilities.WorkItem` type and adding a `AzDOWorkItem` attribute to tie it to a specific WorkItem type in your Azure DevOps project. It must also have a public, default constructor.

You can tie custom fields to your properties using the `[AzDOUtilities.AzDOField]` attribute. Here's an example:

```csharp
[AzDOWorkItem("CustomWorkItem")]
public class CustomWorkItem : WorkItem
{
    [AzDOField("Custom.LabEnabled")]
    public bool LabEnabled { get; set; }
    [AzDOField("Custom.LabType")]
    public string LabType { get; set; }
    ...

    public CustomWorkItem()
    {
    	// Must have a public, default constructor so the lib can create.
    }
}

```

If you create a custom WorkItem type, you should also add an assembly-level attribute `AzDORegister` to one source file. This will expose your custom wrapper type to the library so that the `QueryAsync<WorkItem>` method can return objects based on your type. Note this is only necessary if you're mixing returned WorkItem types. Essentially, when a WorkItem is returned by the underlying REST API, the `WorkItemType` field will be matched to all registered types. If a match is found, the given wrapper will be created and populated with the returned data.

You only need a single register attribute for all your types as shown here.

```csharp
// Register our custom work item types with the lib.
[assembly: AzDORegister(typeof(CustomWorkItem), typeof(OtherWorkItem), ...)]
```

#### Field conversions

The library can convert intrinsic types without any help, but sometimes you want to express the property with something different than the field. You can do this by supplying a _converter_. Tell the library to use a converter by setting the `Converter` property on the `AzDOFieldAttribute`.

```csharp
[AzDOField("Custom.CertificationExam", Converter = typeof(CommaSeparatedConverter))]
public List<string> CertificationExams { get; set; }
```

> **Note:** `DateTime` values are automatically converted to and from local time from UTC.

The supplied `Type` object must implement `IFieldConverter` interface:

```csharp
public interface IFieldConverter
{
    object Convert(object value, Type toType);
    object ConvertBack(object value);
}
```

The `Convert` method is called with the raw AzureDevOps value and the destination (target property) type. The `ConvertBack` method is called with the property value to get an object that can be used to compare or update the WorkItem field to the property. Comparisons are done by comparing the original field value to the existing property value. By default, a direct (value) comparison is performed. If a converter is on the property, the `ConvertBack` is called first. Optionally, the converter can implement the `IFieldComparer` interface, in which case this is used to do a comparions of the two values.

```csharp
public interface IFieldComparer
{
    bool Compare(object initialValue, object currentValue);
}
```

#### Built-in converters

There are several built-in converters:

- `IdentityRefConverter`: this is applied to fields such as `AssignedTo` which return an identity to transform it to a `string`.
- `SemicolonSeparatedConverter`: this can be used to turn a semi-colon separated list (such as a picker) to a `List<string>` or `string[]`.
- `CommaSeparatedConverter`: this can be used to turn a comma separated list to a `List<string>` or `string[]`.	

### Selecting fields

There's no need to specify the fields to select with the higher-level API (LINQ or `IAzureDevOpsService`). Selecting `Id` by itself will still populate the full object. The library will scan the defined properties and `AzDOFieldAttribute` objects to determine the valid fields and automatically request those from Azure DevOps as part of the query.

> **Note:** this behavior means the fields you decorate your properties with _must_ be defined in your Azure DevOps project. A misnamed field will generate a runtime error. The field doesn't have to be in the WorkItem type - it just needs to be defined.
