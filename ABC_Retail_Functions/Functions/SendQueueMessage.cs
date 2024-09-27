using ABC_Retail.Services;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
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
	public class SendQueueMessage
	{
		private readonly AzureQueueService _queueService;
		private readonly ILogger<SendQueueMessage> _logger;

		public SendQueueMessage(AzureQueueService queueService, ILogger<SendQueueMessage> logger)
		{
			_queueService = queueService;
			_logger = logger;
		}

		[Function("ProcessQueueMessage")]
		public async Task<HttpResponseData> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
			FunctionContext executionContext)
		{
			var log = executionContext.GetLogger("ProcessQueueMessage");
			log.LogInformation("Processing request to send message to queue.");

			// Read and deserialize the request body
			var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
			var requestData = JsonSerializer.Deserialize<RequestData>(requestBody);

			if (string.IsNullOrEmpty(requestData?.Message))
			{
				var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await badResponse.WriteStringAsync("Please provide a valid message.");
				return badResponse;
			}

			// Enqueue the message
			await _queueService.EnqueueMessageAsync("your-queue-name", requestData.Message);

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("Message added to queue");
			return response;
		}

		private class RequestData
		{
			public string Message { get; set; }
		}
	}
}