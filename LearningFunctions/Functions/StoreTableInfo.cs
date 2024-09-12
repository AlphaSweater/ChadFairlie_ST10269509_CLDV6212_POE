using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ST10114423.Functions
{
	public static class StoreTableInfo
	{
		[Function("StoreTableInfo")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
			ILogger log)
		{
			string tableName = req.Query["tableName"];
			string partitionKey = req.Query["partitionKey"];
			string rowKey = req.Query["rowKey"];
			string data = req.Query["data"];

			if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey) || string.IsNullOrEmpty(data))
			{
				return new BadRequestObjectResult("Table name, partition key, row key, and data must be provided.");
			}

			var connectionString = Environment.GetEnvironmentVariable("AzureStorage:ConnectionString");
			var serviceClient = new TableServiceClient(connectionString);
			var tableClient = serviceClient.GetTableClient(tableName);
			await tableClient.CreateIfNotExistsAsync();

			var entity = new TableEntity(partitionKey, rowKey) { ["Data"] = data };
			await tableClient.AddEntityAsync(entity);

			return new OkObjectResult("Data added to table");
		}
	}
}