using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Queues;

namespace ABC_Retail_Functions.Functions
{
	public static class SendQueueMessage
	{
		[FunctionName("SendQueueMessage")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("Processing request to send message to queue.");

			// Read and deserialize the request body
			string requestBody;
			try
			{
				requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				log.LogInformation("Request body read successfully.");
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error reading request body.");
				return new BadRequestObjectResult("Error reading request body.");
			}

			RequestData requestData;
			try
			{
				requestData = JsonConvert.DeserializeObject<RequestData>(requestBody);
				log.LogInformation("Request body deserialized successfully.");
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error deserializing request body.");
				return new BadRequestObjectResult("Error deserializing request body.");
			}

			if (string.IsNullOrEmpty(requestData?.Message) || string.IsNullOrEmpty(requestData?.QueueName))
			{
				log.LogWarning("Invalid message or queue name received.");
				return new BadRequestObjectResult("Please provide a valid message and queue name.");
			}

			var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
			var queueServiceClient = new QueueServiceClient(connectionString);
			var queueClient = queueServiceClient.GetQueueClient(requestData.QueueName);

			try
			{
				await queueClient.CreateIfNotExistsAsync();
				await queueClient.SendMessageAsync(requestData.Message);
				log.LogInformation("Message added to queue successfully.");
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error adding message to queue.");
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}

			return new OkObjectResult("Message added to queue");
		}

		private class RequestData
		{
			public string? Message { get; set; }
			public string? QueueName { get; set; }
		}
	}
}
