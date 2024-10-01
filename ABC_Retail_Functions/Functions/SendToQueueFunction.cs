using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

			try
			{
				await _queueService.EnqueueMessageAsync(requestData.QueueName, requestData.Message);

				log.LogInformation("Message added to queue successfully.");
				log.LogInformation($"Sending Base64-encoded message to queue: {requestData.Message}");
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
