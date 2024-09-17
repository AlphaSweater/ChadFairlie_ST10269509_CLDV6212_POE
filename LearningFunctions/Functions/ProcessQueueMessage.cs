using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABC_Retail_Functions.Functions
{
	public static class ProcessQueueMessage
	{
		[Function("ProcessQueueMessage")]
		public static async Task<HttpResponseData> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
			FunctionContext executionContext)
		{
			var log = executionContext.GetLogger("ProcessQueueMessage");
			log.LogInformation("Processing request to send message to queue.");

			string queueName = "inventory-queue";

			try
			{
				// Read and parse request body (expecting JSON with a "message" property)
				var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				var requestData = JsonSerializer.Deserialize<RequestData>(requestBody);

				if (string.IsNullOrEmpty(requestData?.Message))
				{
					var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
					await badResponse.WriteStringAsync("Please provide a valid message.");
					return badResponse;
				}

				// Azure Storage connection string from environment variable
				var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
				// Create QueueClient and send message
				var queueServiceClient = new QueueServiceClient(connectionString);
				var queueClient = queueServiceClient.GetQueueClient(queueName);
				await queueClient.CreateIfNotExistsAsync();
				await queueClient.SendMessageAsync(requestData.Message);

				// Return success response
				var okResponse = req.CreateResponse(HttpStatusCode.OK);
				await okResponse.WriteStringAsync($"Message '{requestData.Message}' added to queue '{queueName}'");
				return okResponse;
			}
			catch (Exception ex)
			{
				log.LogError($"Error processing queue message: {ex.Message}");

				// Return error response
				var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
				await errorResponse.WriteStringAsync("Error processing request: " + ex.Message);
				return errorResponse;
			}
		}

		// Helper class to model the request data (JSON)
		private class RequestData
		{
			public string Message { get; set; }
		}
	}
}