using System.Reflection;
using MyPo.Api;
using MyPo.Shared.Helpers;

var appBuilder = WebApplication.CreateBuilder(args);

// load custom assemblies for bootstrapping
var assemblies = AppDomain.CurrentDomain.GetAssemblies();
var additionalAssemblies = appBuilder.Configuration.GetSection("Bootstrap:Assemblies").Get<List<string>>() ?? [];
Console.WriteLine($"[INFO] Loading additional assemblies...");
foreach (var assemblyName in additionalAssemblies)
{
	Console.WriteLine($"[INFO] -- Loading assembly [{assemblyName}]...");
	try
	{
		assemblies = [.. assemblies, Assembly.Load(assemblyName)];
	}
	catch (Exception e) when (e is ArgumentException || e is FileNotFoundException || e is FileLoadException || e is BadImageFormatException)
	{
		Console.WriteLine($"[ERROR] -- Failed to load assembly [{assemblyName}]: {e.Message}");
	}
}

// Bootstrapping
var tasks = AppBootstrapper.Bootstrap(out var app, appBuilder, assemblies);
var logger = app.Services.GetService<ILogger<Program>>();
logger?.LogInformation("Waiting for {n} background bootstrapping task(s)...", tasks.Count);
await AsyncHelper.WaitForBackgroundTasksAsync(tasks, logger);
MyPo.Shared.Api.Globals.Ready = true;
logger?.LogInformation("Background bootstrapping completed.");

await app.RunAsync();
