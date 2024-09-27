using ABC_Retail.Services;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services =>
	{
		services.AddApplicationInsightsTelemetryWorkerService();
		services.ConfigureFunctionsApplicationInsights();

		// Read the connection string from environment variables
		var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

		// Register services
		services.AddSingleton(new AzureFileStorageService(connectionString));
		services.AddSingleton(new AzureBlobStorageService(new BlobServiceClient(connectionString), null, null));
		services.AddSingleton(new ProductTableService(connectionString));
		services.AddSingleton(new CustomerTableService(connectionString));
		services.AddSingleton<Func<string, QueueClient>>(sp => queueName =>
		{
			var queueClient = new QueueClient(connectionString, queueName);
			queueClient.CreateIfNotExists(); // Ensure the queue exists
			return queueClient;
		});
		services.AddSingleton<AzureQueueService>();
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
