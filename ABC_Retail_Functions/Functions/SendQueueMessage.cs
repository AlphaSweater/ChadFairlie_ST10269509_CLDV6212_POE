using ABC_Retail.Services;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABC_Retail_Functions.Functions
{
	public class SendQueueMessage
	{
		private readonly AzureQueueService _queueService;

		public SendQueueMessage(AzureQueueService queueService)
		{
			_queueService = queueService;
		}

		[Function("ProcessQueueMessage")]
		public async Task<HttpResponseData> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
			FunctionContext executionContext)
		{
			var log = executionContext.GetLogger("ProcessQueueMessage");
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
				var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await errorResponse.WriteStringAsync("Error reading request body.");
				return errorResponse;
			}

			RequestData requestData;
			try
			{
				requestData = JsonSerializer.Deserialize<RequestData>(requestBody);
				log.LogInformation("Request body deserialized successfully.");
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error deserializing request body.");
				var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await errorResponse.WriteStringAsync("Error deserializing request body.");
				return errorResponse;
			}

			if (string.IsNullOrEmpty(requestData?.Message))
			{
				log.LogWarning("Invalid message received.");
				var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await badResponse.WriteStringAsync("Please provide a valid message.");
				return badResponse;
			}

			try
			{
				// Enqueue the message
				await _queueService.EnqueueMessageAsync(requestData.QueueName, requestData.Message);
				log.LogInformation("Message added to queue successfully.");
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error adding message to queue.");
				var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
				await errorResponse.WriteStringAsync("Error adding message to queue.");
				return errorResponse;
			}

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("Message added to queue");
			return response;
		}

		private class RequestData
		{
			public string Message { get; set; }
			public string QueueName { get; set; }
		}
	}
}