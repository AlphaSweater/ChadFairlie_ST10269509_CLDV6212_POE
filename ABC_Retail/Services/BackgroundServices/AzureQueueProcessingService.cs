using ABC_Retail.Models;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Text;
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

		// Service for interacting with Azure Queue Storage.
		private readonly AzureQueueService _queueService;

		// Service for interacting with product SQL Table Storage.
		private readonly Product _productTableService;

		// Service for interacting with order SQL Table Storage.
		private readonly Order _orderTableService;

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
		public AzureQueueProcessingService(Func<string, QueueClient> queueClientFactory, AzureQueueService queueService, Product productTableService, Order orderTableService)
		{
			_queueClientFactory = queueClientFactory;
			_queueService = queueService;
			_productTableService = productTableService;
			_orderTableService = orderTableService;
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
			// Receive a message from the queue.
			var message = await queueClient.ReceiveMessageAsync();
			if (message.Value == null)
			{
				return;
			}

			try
			{
				// Decode the Base64-encoded message
				string base64Message = message.Value.MessageText;
				string jsonMessage = Encoding.UTF8.GetString(Convert.FromBase64String(base64Message));

				// Deserialize the order message
				var orderMessage = JsonSerializer.Deserialize<OrderMessage>(jsonMessage);
				if (orderMessage == null)
				{
					return;
				}

				// Process the order
				bool success = await ProcessOrderAndUpdateInventoryAsync(orderMessage);
				if (success)
				{
					// Delete the processed message from the queue.
					await queueClient.DeleteMessageAsync(message.Value.MessageId, message.Value.PopReceipt);

					await LogInventoryUpdateAsync(orderMessage);
				}
			}
			catch (Exception ex)
			{
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
			// Check if product in the order is available in the required quantity.
			var dbProduct = await _productTableService.GetProductByIdAsync(orderMessage.ProductId);
			if (dbProduct == null || dbProduct.Quantity < orderMessage.Quantity)
			{
				// Not enough stock or product not found, order cannot be processed.
				return false;
			}
		
			dbProduct.Quantity -= orderMessage.Quantity;

			try
			{
				await _productTableService.UpdateProductAsync(dbProduct);
				await _orderTableService.RecordOrderAsync(orderMessage);
			}
			catch (RequestFailedException ex) when (ex.Status == 412)
			{
				// Concurrency conflict, retry the operation
				return await ProcessOrderAndUpdateInventoryAsync(orderMessage);
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
			var inventoryUpdateMessage = new InventoryUpdateMessage
			(
				orderMessage.ProductId,
				-orderMessage.Quantity, // Negative quantity for stock deduction
				"Order processed"
			);

			var messageText = JsonSerializer.Serialize(inventoryUpdateMessage);
			await _queueService.EnqueueMessageAsync(_inventoryQueueName, messageText);
		}
	}
}