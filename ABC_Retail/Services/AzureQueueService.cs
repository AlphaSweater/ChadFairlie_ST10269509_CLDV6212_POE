using Azure.Storage.Queues;
using System.Text.Json;

namespace ABC_Retail.Services
{
	public class AzureQueueService
	{
		private readonly QueueClient _queueClient;

		// Modify the constructor to accept QueueClient
		public AzureQueueService(QueueClient queueClient)
		{
			_queueClient = queueClient;
			_queueClient.CreateIfNotExists(); // Ensure the queue exists
		}

		public async Task EnqueueMessageAsync(object message)
		{
			var jsonMessage = JsonSerializer.Serialize(message);
			await _queueClient.SendMessageAsync(jsonMessage);
		}
	}
}