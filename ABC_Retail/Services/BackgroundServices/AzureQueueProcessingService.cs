using Azure.Storage.Queues;
using System.Text.Json;

namespace ABC_Retail.Services.BackgroundServices
{
	public class AzureQueueProcessingService : BackgroundService
	{
		private readonly QueueClient _queueClient;
		private readonly AzureTableStorageService _tableStorageService;

		// Modify the constructor to accept QueueClient
		public AzureQueueProcessingService(QueueClient queueClient, AzureTableStorageService tableStorageService)
		{
			_queueClient = queueClient;
			_tableStorageService = tableStorageService;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				var message = await _queueClient.ReceiveMessageAsync();
				if (message.Value != null)
				{
					var purchaseMessage = JsonSerializer.Deserialize<PurchaseMessage>(message.Value.MessageText);

					// Process the purchase
					var product = await _tableStorageService.GetProductAsync("Product", purchaseMessage.ProductId);
					if (product != null && product.Quantity > 0)
					{
						product.Quantity -= purchaseMessage.Quantity;
						await _tableStorageService.UpdateProductAsync(product);
					}

					// Delete the message after processing
					await _queueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt);
				}

				await Task.Delay(150, stoppingToken); // Add delay to prevent tight looping
			}
		}

		private class PurchaseMessage
		{
			public string ProductId { get; set; }
			public string ProductName { get; set; }
			public int Quantity { get; set; }
		}
	}
}