using ABC_Retail.Models;
using ABC_Retail.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

[assembly: FunctionsStartup(typeof(ABC_Retail_Functions.Startup))]

namespace ABC_Retail_Functions
{
	public class Startup : FunctionsStartup
	{
		public override void Configure(IFunctionsHostBuilder builder)
		{
			// Add your service registrations here just like in the ASP.NET Core app

			// Get the configuration from the environment (optional)
			var configuration = builder.GetContext().Configuration;
			string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
				?? throw new InvalidOperationException("AzureStorage connection string is not configured.");
			string sqlConnectionString = Environment.GetEnvironmentVariable("AzureSQLStorage")
				?? throw new InvalidOperationException("Azure SQL connection string is not configured.");

			// Register common services
			builder.Services.AddSingleton(new Product(sqlConnectionString));
			builder.Services.AddSingleton(new Customer(sqlConnectionString));
			builder.Services.AddSingleton(new AzureFileStorageService(storageConnectionString));
			builder.Services.AddSingleton(new BlobServiceClient(storageConnectionString));

			// Register a factory method for creating QueueClient instances
			builder.Services.AddSingleton<Func<string, QueueClient>>(sp => queueName =>
			{
				var queueClient = new QueueClient(storageConnectionString, queueName);
				queueClient.CreateIfNotExists(); // Ensure the queue exists
				return queueClient;
			});

			// Register your custom AzureQueueService
			builder.Services.AddSingleton<AzureQueueService>();

			// Register BlobStorage-related services
			builder.Services.AddSingleton<SasTokenGenerator>();
			builder.Services.AddSingleton<AzureBlobStorageService>(sp =>
			{
				var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
				var blobServiceClient = sp.GetRequiredService<BlobServiceClient>();
				var sasTokenGenerator = sp.GetRequiredService<SasTokenGenerator>();
				return new AzureBlobStorageService(blobServiceClient, sasTokenGenerator);
			});
		}
	}
}