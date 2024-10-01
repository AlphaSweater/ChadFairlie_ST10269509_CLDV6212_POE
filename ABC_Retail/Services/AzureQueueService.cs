using Azure.Storage.Queues;
using System.Text;
using System.Text.Json;


namespace ABC_Retail.Services
{
	/// <summary>
	/// Provides a service for interacting with Azure Queue Storage, including
	/// enqueuing messages into a specified queue.
	/// </summary>
	public class AzureQueueService
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Dependencies
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Factory function to create QueueClient instances for different queues.
		private readonly Func<string, QueueClient> _queueClientFactory;

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		/// <summary>
		/// Initializes a new instance of the <see cref="AzureQueueService"/> class.
		/// </summary>
		public AzureQueueService(Func<string, QueueClient> queueClientFactory)
		{
			_queueClientFactory = queueClientFactory;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Enqueue Message Methods
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Enqueues a message to the specified Azure Queue.
		/// </summary>
		/// <param name="queueName">The name of the queue to enqueue the message into.</param>
		/// <param name="message">The message to be enqueued.</param>
		public async Task EnqueueMessageAsync(string queueName, string message)
		{
			try
			{
				// Create a QueueClient for the specified queue.
				var queueClient = _queueClientFactory(queueName);

				var encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));

				// Send the serialized message to the queue.
				await queueClient.SendMessageAsync(encodedMessage);
			}
			catch (Exception ex)
			{
				// Log the exception (assuming a logger is available)
				Console.WriteLine($"Error enqueuing message: {ex.Message}");
				throw;
			}
		}
	}
}