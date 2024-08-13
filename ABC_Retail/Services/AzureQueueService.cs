using Azure.Storage.Queues;
using System.Text.Json;

namespace ABC_Retail.Services
{
	public class AzureQueueService
	{
		private readonly Func<string, QueueClient> _queueClientFactory;

		public AzureQueueService(Func<string, QueueClient> queueClientFactory)
		{
			_queueClientFactory = queueClientFactory;
		}

		public async Task EnqueueMessageAsync<T>(string queueName, T message)
		{
			var queueClient = _queueClientFactory(queueName);
			var jsonMessage = JsonSerializer.Serialize(message);
			await queueClient.SendMessageAsync(jsonMessage);
		}
	}
}