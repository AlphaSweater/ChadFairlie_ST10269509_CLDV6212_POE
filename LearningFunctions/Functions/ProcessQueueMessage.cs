using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ST10114423.Functions
{
	public static class ProcessQueueMessage
	{
		[Function("ProcessQueueMessage")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
			ILogger log)
		{
			string queueName = req.Query["queueName"];
			string message = req.Query["message"];

			if (string.IsNullOrEmpty(queueName) || string.IsNullOrEmpty(message))
			{
				return new BadRequestObjectResult("Queue name and message must be provided.");
			}

			var connectionString = Environment.GetEnvironmentVariable("AzureStorage:ConnectionString");
			var queueServiceClient = new QueueServiceClient(connectionString);
			var queueClient = queueServiceClient.GetQueueClient(queueName);
			await queueClient.CreateIfNotExistsAsync();
			await queueClient.SendMessageAsync(message);

			return new OkObjectResult("Message added to queue");
		}
	}
}