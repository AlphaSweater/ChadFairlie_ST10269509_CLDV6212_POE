using ABC_Retail.Services;
using ABC_Retail.Services.BackgroundServices;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

namespace ABC_Retail
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			ConfigureServices(builder.Services, builder.Configuration);

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}");

			app.Run();
		}

		// Configure services
		public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
		{
			services.AddControllersWithViews();

			// Add logging services
			services.AddLogging(config =>
			{
				config.AddConsole();
				config.AddDebug();
			});

			// Get the connection string for Azure Storage
			string storageConnectionString = configuration.GetConnectionString("StorageConnectionString")
								 ?? throw new InvalidOperationException("AzureStorage connection string is not configured.");

			// Add product Azure Table Storage service
			services.AddSingleton(new ProductTableService(storageConnectionString));

			// Add customer Azure Table Storage service
			services.AddSingleton(new CustomerTableService(storageConnectionString));

			// Add Azure File Storage service
			services.AddSingleton(new AzureFileStorageService(storageConnectionString));

			// Add BlobServiceClient
			services.AddSingleton(new BlobServiceClient(storageConnectionString));

			// Register a factory method for creating QueueClient instances
			services.AddSingleton<Func<string, QueueClient>>(sp => queueName =>
			{
				var queueClient = new QueueClient(storageConnectionString, queueName);
				queueClient.CreateIfNotExists(); // Ensure the queue exists
				return queueClient;
			});

			// Register AzureQueueService and AzureQueueProcessingService
			services.AddSingleton<AzureQueueService>();
			services.AddSingleton<IHostedService, AzureQueueProcessingService>();

			// Add SasTokenGenerator and AzureBlobStorageService
			services.AddSingleton<SasTokenGenerator>();
			services.AddSingleton<AzureBlobStorageService>(sp =>
			{
				var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
				var blobServiceClient = sp.GetRequiredService<BlobServiceClient>();
				var sasTokenGenerator = sp.GetRequiredService<SasTokenGenerator>();
				return new AzureBlobStorageService(blobServiceClient, sasTokenGenerator, logger);
			});

			// Add HttpClient
			services.AddHttpClient();
		}
	}
}