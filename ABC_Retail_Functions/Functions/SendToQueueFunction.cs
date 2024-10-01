using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Queues;

namespace ABC_Retail_Functions.Functions
{
	public class SendToQueueFunction
	{
		[FunctionName("SendToQueueFunction")]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("Processing request to send message to queue.");

			string requestBody;
			RequestData requestData;
			try
			{
				using var reader = new StreamReader(req.Body);
				requestBody = await reader.ReadToEndAsync();
				log.LogInformation("Request body read successfully.");
				requestData = JsonConvert.DeserializeObject<RequestData>(requestBody);
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error reading or deserializing request body.");
				return new BadRequestObjectResult("Error reading or deserializing request body.");
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

				var encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(requestData.Message));
				await queueClient.SendMessageAsync(encodedMessage);

				log.LogInformation("Message added to queue successfully.");
				log.LogInformation($"Sending Base64-encoded message to queue: {encodedMessage}");
				return new OkObjectResult("Message added to queue (Base64-encoded)");
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error adding message to queue.");
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}
		}

		private class RequestData
		{
			public string? Message { get; set; }
			public string? QueueName { get; set; }
		}
	}
}
