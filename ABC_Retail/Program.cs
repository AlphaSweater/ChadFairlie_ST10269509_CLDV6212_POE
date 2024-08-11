using ABC_Retail.Services;

namespace ABC_Retail
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddControllersWithViews();

			// Add logging services
			builder.Services.AddLogging(config =>
			{
				config.AddConsole();
				config.AddDebug();
			});

			// Add Azure Table Storage service
			string storageConnectionString = builder.Configuration.GetConnectionString("AzureStorage");
			builder.Services.AddSingleton(new AzureTableStorageService(storageConnectionString));
			builder.Services.AddSingleton<AzureBlobStorageService>(sp =>
			{
				var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
				return new AzureBlobStorageService(storageConnectionString, logger);
			});

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
	}
}