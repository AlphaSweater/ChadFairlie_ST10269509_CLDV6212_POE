using ABC_Retail.Models;
using Azure.Storage.Queues;
using System.Text.Json;

namespace ABC_Retail.Services.BackgroundServices
{
	/// <summary>
	/// Background service responsible for processing messages from Azure Queue Storage.
	/// Handles orders and updates inventory accordingly.
	/// </summary>
	public class AzureQueueProcessingService : BackgroundService
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Fields and Dependencies
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Factory function to create QueueClient instances for different queues.
		private readonly Func<string, QueueClient> _queueClientFactory;

		// Service for interacting with product Azure Table Storage.
		private readonly ProductTableService _productTableService;

		// Name of the queue used for processing purchase orders.
		private readonly string _purchaseQueueName = "purchase-queue";

		// Name of the queue used for processing inventory updates.
		private readonly string _inventoryQueueName = "inventory-queue";

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		/// <summary>
		/// Initializes a new instance of the <see cref="AzureQueueProcessingService"/> class.
		/// </summary>
		public AzureQueueProcessingService(Func<string, QueueClient> queueClientFactory, ProductTableService productTableService)
		{
			_queueClientFactory = queueClientFactory;
			_productTableService = productTableService;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Main Execution Loop
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Executes the background service, processing messages from the purchase queue.
		/// </summary>
		/// <param name="stoppingToken">A token that can be used to signal cancellation of the operation.</param>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// Create a QueueClient for the purchase queue.
			var purchaseQueueClient = _queueClientFactory(_purchaseQueueName);

			// Continuously process messages from the queue until cancellation is requested.
			while (!stoppingToken.IsCancellationRequested)
			{
				await ProcessQueueMessagesAsync(purchaseQueueClient, stoppingToken);
				await Task.Delay(150, stoppingToken); // Delay to prevent rapid polling of the queue.
			}
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Message Processing Logic
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Processes messages from the specified queue, determining the message type and handling it accordingly.
		/// </summary>
		/// <param name="queueClient">The client used to interact with the queue.</param>
		/// <param name="stoppingToken">A token that can be used to signal cancellation of the operation.</param>
		private async Task ProcessQueueMessagesAsync(QueueClient queueClient, CancellationToken stoppingToken)
		{
			// Receive a message from the queue.
			var message = await queueClient.ReceiveMessageAsync();
			if (message.Value != null)
			{
				// Determine the message type.
				var messageType = Message.GetMessageType(message.Value.MessageText);

				// Process order messages.
				if (messageType == "OrderMessage")
				{
					await ProcessOrderMessageAsync(message.Value.MessageText);
				}

				// Delete the processed message from the queue.
				await queueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt);
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Processes an order message, updates inventory, and logs the inventory update if successful.
		/// </summary>
		/// <param name="messageText">The JSON-encoded message text to be processed.</param>
		private async Task ProcessOrderMessageAsync(string messageText)
		{
			// De-serialize the message text into an OrderMessage object.
			var orderMessage = JsonSerializer.Deserialize<OrderMessage>(messageText);

			// Process the order and update inventory.
			bool isOrderProcessed = await ProcessOrderAndUpdateInventoryAsync(orderMessage);

			// If the order was successfully processed, log the inventory update.
			if (isOrderProcessed)
			{
				await LogInventoryUpdateAsync(orderMessage);
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Processes the order by checking product availability and updating inventory accordingly.
		/// </summary>
		/// <param name="orderMessage">The order message containing product information.</param>
		/// <returns>A boolean indicating whether the order was successfully processed and inventory updated.</returns>
		private async Task<bool> ProcessOrderAndUpdateInventoryAsync(OrderMessage orderMessage)
		{
			// Check if each product in the order is available in the required quantity.
			foreach (var product in orderMessage.Products)
			{
				var dbProduct = await _productTableService.GetEntityAsync("Product", product.ProductId);
				if (dbProduct == null || dbProduct.Quantity < product.Quantity)
				{
					// Not enough stock or product not found, order cannot be processed.
					return false;
				}
			}

			// Update inventory for each product in the order.
			foreach (var product in orderMessage.Products)
			{
				var dbProduct = await _productTableService.GetEntityAsync("Product", product.ProductId);
				dbProduct.Quantity -= product.Quantity;
				await _productTableService.UpdateEntityAsync(dbProduct);
			}

			return true; // Order processed successfully.
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Logs the inventory update by sending a message to the inventory queue.
		/// </summary>
		/// <param name="orderMessage">The order message containing the products that were processed.</param>
		private async Task LogInventoryUpdateAsync(OrderMessage orderMessage)
		{
			// Log inventory update for each product in the order.
			foreach (var product in orderMessage.Products)
			{
				var inventoryUpdateMessage = new InventoryUpdateMessage
				{
					Name = product.ProductName,
					Quantity = -product.Quantity, // Negative quantity to indicate reduction in stock.
					Reason = "Order processed"
				};

				// Send the inventory update message to the inventory queue.
				var inventoryQueueClient = _queueClientFactory(_inventoryQueueName);
				var messageText = JsonSerializer.Serialize(inventoryUpdateMessage);
				await inventoryQueueClient.SendMessageAsync(messageText);
			}
		}
	}
}