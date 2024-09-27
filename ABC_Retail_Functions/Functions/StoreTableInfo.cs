using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ABC_Retail.Services;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;


namespace ABC_Retail_Functions.Functions
{
	public class StoreTableInfo
	{
		private readonly AzureTableStorageService<TableEntity> _tableService;
		private readonly ILogger<StoreTableInfo> _logger;

		public StoreTableInfo(AzureTableStorageService<TableEntity> tableService, ILogger<StoreTableInfo> logger)
		{
			_tableService = tableService;
			_logger = logger;
		}

		[Function("StoreTableInfo")]
		public async Task<HttpResponseData> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
			FunctionContext executionContext)
		{
			var log = executionContext.GetLogger("StoreTableInfo");
			log.LogInformation("Processing request to store table info.");

			// Your existing code here...

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("Data added to table");
			return response;
		}
	}
}