using ABC_Retail.Models;
using Azure.Storage.Queues;
using System.Text.Json;

namespace ABC_Retail.Services.BackgroundServices
{
	public class AzureQueueProcessingService : BackgroundService
	{
		private readonly Func<string, QueueClient> _queueClientFactory;
		private readonly AzureTableStorageService _tableStorageService;
		private readonly string _purchaseQueueName = "purchase-queue";
		private readonly string _inventoryQueueName = "inventory-queue";

		public AzureQueueProcessingService(Func<string, QueueClient> queueClientFactory, AzureTableStorageService tableStorageService)
		{
			_queueClientFactory = queueClientFactory;
			_tableStorageService = tableStorageService;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var purchaseQueueClient = _queueClientFactory(_purchaseQueueName);

			while (!stoppingToken.IsCancellationRequested)
			{
				await ProcessQueueMessagesAsync(purchaseQueueClient, stoppingToken);
				await Task.Delay(150, stoppingToken);
			}
		}

		private async Task ProcessQueueMessagesAsync(QueueClient queueClient, CancellationToken stoppingToken)
		{
			var message = await queueClient.ReceiveMessageAsync();
			if (message.Value != null)
			{
				var messageType = Message.GetMessageType(message.Value.MessageText);

				if (messageType == "OrderMessage")
				{
					await ProcessOrderMessageAsync(message.Value.MessageText);
				}

				await queueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt);
			}
		}

		private async Task ProcessOrderMessageAsync(string messageText)
		{
			var orderMessage = JsonSerializer.Deserialize<OrderMessage>(messageText);
			bool isOrderProcessed = await ProcessOrderAndUpdateInventoryAsync(orderMessage);

			if (isOrderProcessed)
			{
				await LogInventoryUpdateAsync(orderMessage);
			}
		}

		private async Task<bool> ProcessOrderAndUpdateInventoryAsync(OrderMessage orderMessage)
		{
			foreach (var product in orderMessage.Products)
			{
				var dbProduct = await _tableStorageService.GetProductAsync("Product", product.ProductId);
				if (dbProduct == null || dbProduct.Quantity < product.Quantity)
				{
					// Not enough stock or product not found
					return false;
				}
			}

			// Update inventory
			foreach (var product in orderMessage.Products)
			{
				var dbProduct = await _tableStorageService.GetProductAsync("Product", product.ProductId);
				dbProduct.Quantity -= product.Quantity;
				await _tableStorageService.UpdateProductAsync(dbProduct);
			}

			return true;
		}

		private async Task LogInventoryUpdateAsync(OrderMessage orderMessage)
		{
			foreach (var product in orderMessage.Products)
			{
				var inventoryUpdateMessage = new InventoryUpdateMessage
				{
					Name = product.ProductName,
					Quantity = -product.Quantity,
					Reason = "Order processed"
				};

				var inventoryQueueClient = _queueClientFactory(_inventoryQueueName);
				var messageText = JsonSerializer.Serialize(inventoryUpdateMessage);
				await inventoryQueueClient.SendMessageAsync(messageText);
			}
		}
	}
}