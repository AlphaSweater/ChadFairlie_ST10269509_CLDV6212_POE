using ABC_Retail.Services;
using Azure.Storage.Blobs;

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
			string storageConnectionString = configuration.GetConnectionString("AzureStorage");

			// Add Azure Table Storage service
			services.AddSingleton(new AzureTableStorageService(storageConnectionString));

			// Add BlobServiceClient
			services.AddSingleton(new BlobServiceClient(storageConnectionString));

			// Add SasTokenGenerator and AzureBlobStorageService
			services.AddSingleton<SasTokenGenerator>();
			services.AddSingleton<AzureBlobStorageService>(sp =>
			{
				var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
				var blobServiceClient = sp.GetRequiredService<BlobServiceClient>();
				var sasTokenGenerator = sp.GetRequiredService<SasTokenGenerator>();
				return new AzureBlobStorageService(blobServiceClient, sasTokenGenerator, logger);
			});
		}
	}
}