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
		private readonly ILogger<AzureQueueProcessingService> _logger;

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
		public AzureQueueProcessingService(Func<string, QueueClient> queueClientFactory, ProductTableService productTableService, ILogger<AzureQueueProcessingService> logger)
		{
			_queueClientFactory = queueClientFactory;
			_productTableService = productTableService;
			_logger = logger;
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
		/// Processes messages from the specified queue.
		/// </summary>
		/// <param name="queueClient">The client used to interact with the queue.</param>
		/// <param name="stoppingToken">A token that can be used to signal cancellation of the operation.</param>
		private async Task ProcessQueueMessagesAsync(QueueClient queueClient, CancellationToken stoppingToken)
		{
			// Receive up to 10 messages from the queue.
			var messages = await queueClient.ReceiveMessagesAsync(maxMessages: 10, visibilityTimeout: TimeSpan.FromMinutes(1), cancellationToken: stoppingToken);

			foreach (var message in messages.Value)
			{
				try
				{
					var messageText = message.MessageText;

					// Remove the outer quotes if they exist
					if (messageText.StartsWith("\"") && messageText.EndsWith("\""))
					{
						messageText = messageText.Substring(1, messageText.Length - 2);
					}

					// Unescape the JSON string
					string unescapedMessageText = System.Text.RegularExpressions.Regex.Unescape(messageText);

					// Determine the message type.
					var messageType = Message.GetMessageType(unescapedMessageText);

					switch (messageType)
					{
						case "OrderMessage":
							// Process order message.
							await ProcessOrderMessageAsync(unescapedMessageText);
							// Delete the message after processing
							await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
							break;
						case "InventoryUpdateMessage":
							// TODO: Maybe add something
							// Delete the message after processing
							await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
							break;
						default:
							_logger.LogWarning($"Unknown message type: {messageType}");
							break;
					}


				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error processing message");
				}
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Processes an order message, updates inventory, and logs the inventory update if successful.
		/// </summary>
		/// <param name="messageText">The JSON-encoded message text to be processed.</param>
		private async Task ProcessOrderMessageAsync(string messageText)
		{

			try
			{
				// De-serialize the message text into an OrderMessage object.
				var orderMessage = JsonSerializer.Deserialize<OrderMessage>(messageText);

				if (orderMessage == null)
				{
					_logger.LogWarning("Error deserializing order message");
					return;
				}

				// Process the order and update inventory.
				bool isOrderProcessed = await ProcessOrderAndUpdateInventoryAsync(orderMessage);

				if (!isOrderProcessed)
				{
					return;
				}

				// If the order was successfully processed, log the inventory update.
				await LogInventoryUpdateAsync(orderMessage);
				_logger.LogInformation($"Processed order message: {orderMessage.OrderId}");
			}
			catch (JsonException ex)
			{
				_logger.LogError(ex, "Error deserializing order message");
				throw;
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
			if (orderMessage.Products == null)
			{
				return false;
			}

			foreach (var product in orderMessage.Products)
			{
				if (string.IsNullOrEmpty(product.ProductId))
				{
					_logger.LogWarning("ProductId is null or empty in the order message");
					return false;
				}

				var dbProduct = await _productTableService.GetEntityAsync("Product", product.ProductId);
				if (dbProduct == null || dbProduct.Quantity < product.Quantity)
				{
					// Not enough stock or product not found, order cannot be processed.
					return false;
				}
			}

			foreach (var product in orderMessage.Products)
			{
				if (string.IsNullOrEmpty(product.ProductId))
				{
					_logger.LogWarning("ProductId is null or empty in the order message");
					return false;
				}

				var dbProduct = await _productTableService.GetEntityAsync("Product", product.ProductId ?? string.Empty);
				if (dbProduct == null || dbProduct.Quantity < product.Quantity)
				{
					// Not enough stock or product not found, order cannot be processed.
					return false;
				}

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
			var inventoryQueueClient = _queueClientFactory(_inventoryQueueName);

			if (orderMessage.Products == null)
			{
				return;
			}

			// Log inventory update for each product in the order.
			foreach (var product in orderMessage.Products)
			{
				var inventoryUpdateMessage = new InventoryUpdateMessage
				(
					product.ProductName,
					-product.Quantity, // Negative quantity to indicate reduction in stock.
					"Order processed"
				);

				// Send the inventory update message to the inventory queue.
				var messageText = JsonSerializer.Serialize(inventoryUpdateMessage);
				await inventoryQueueClient.SendMessageAsync(messageText);
			}
		}
	}
}