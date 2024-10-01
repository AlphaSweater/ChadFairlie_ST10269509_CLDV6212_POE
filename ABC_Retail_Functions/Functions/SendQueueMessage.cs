using System;
using System.IO;
using System.Threading.Tasks;
using System.Text; // Add this for encoding
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Queues;
using ABC_Retail.Services;

namespace ABC_Retail_Functions.Functions
{
	public class SendToQueueFunction
	{
		private readonly AzureQueueService _queueService;

		public SendToQueueFunction(AzureQueueService queueService)
		{
			_queueService = queueService;
		}

		[FunctionName("SendQueueMessage")]
		public async Task<IActionResult> Run(
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
				// Ensure the queue exists
				await queueClient.CreateIfNotExistsAsync();

				// Encode the message in Base64
				string encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(requestData.Message));

				// Send the Base64-encoded message to the queue
				await queueClient.SendMessageAsync(encodedMessage);
				log.LogInformation("Message added to queue successfully.");
				log.LogInformation($"Sending Base64-encoded message to queue: {encodedMessage}");
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error adding message to queue.");
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}

			return new OkObjectResult("Message added to queue (Base64-encoded)");
		}

		private class RequestData
		{
			public string? Message { get; set; }
			public string? QueueName { get; set; }
		}
	}
}
