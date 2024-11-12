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
	/// <summary>
	/// Azure Function to send messages to an Azure Queue.
	/// </summary>
	public class SendToQueueFunction
	{
		private readonly AzureQueueService _queueService;

		/// <summary>
		/// Constructor to initialize the queue service.
		/// </summary>
		/// <param name="queueService">Service for handling Azure Queue operations.</param>
		public SendToQueueFunction(AzureQueueService queueService)
		{
			_queueService = queueService;
		}

		/// <summary>
		/// HTTP trigger function to process the request and send a message to the specified queue.
		/// </summary>
		/// <param name="req">HTTP request containing the message and queue name.</param>
		/// <returns>ActionResult indicating the result of the operation.</returns>
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
				// Read and deserialize the request body
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

			// Validate the request data
			if (string.IsNullOrEmpty(requestData?.Message) || string.IsNullOrEmpty(requestData?.QueueName))
			{
				log.LogWarning("Invalid message or queue name received.");
				return new BadRequestObjectResult("Please provide a valid message and queue name.");
			}

			try
			{
				log.LogInformation("Trying to Add message to queue.");
				log.LogInformation($"Message: {requestData.Message}");
				log.LogInformation($"Queue Name: {requestData.QueueName}");
				// Enqueue the message to the specified queue
				await _queueService.EnqueueMessageAsync(requestData.QueueName, requestData.Message);

				log.LogInformation("Message added to queue successfully.");
				log.LogInformation($"Sending Base64-encoded message to queue: {requestData.Message}");
				return new OkObjectResult("Message added to queue (Base64-encoded)");
			}
			catch (Exception ex)
			{
				log.LogError($"Error adding message to queue. {ex.Message}");
				return new BadRequestObjectResult($"Error adding message to queue. Error: {StatusCodes.Status500InternalServerError}");
			}
		}

		/// <summary>
		/// Represents the data structure for the request body.
		/// </summary>
		private class RequestData
		{
			public string? Message { get; set; }
			// The message to be sent to the queue
			public string? QueueName { get; set; }
			// The name of the queue to which the message will be sent
		}
	}
}
