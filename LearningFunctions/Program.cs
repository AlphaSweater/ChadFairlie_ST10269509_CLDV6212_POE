using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services =>
	{
		services.AddApplicationInsightsTelemetryWorkerService();
		services.ConfigureFunctionsApplicationInsights();
	})
	.ConfigureWebJobs(b =>
	{
		// Register specific storage bindings
		b.AddHttp();
		b.AddAzureStorageBlobs(); // For Blob Storage functions
		b.AddAzureStorageQueues(); // For Queue Storage functions
								   // b.AddAzureStorageQueuesScaleForTrigger(); // Add this if scaling is needed for Queue Triggers
	})
	.Build();

host.Run();
