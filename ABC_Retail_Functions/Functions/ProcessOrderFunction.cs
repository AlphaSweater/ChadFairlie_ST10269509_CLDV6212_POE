using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ABC_Retail.Services;
using ABC_Retail.Models;
using Azure;

namespace ABC_Retail_Functions.Functions
{
	/// <summary>
	/// Azure Function to process orders from a queue and update inventory in Azure Table Storage.
	/// </summary>
	public class ProcessOrderFunction
	{
		private readonly Product _productTableService;
		private readonly Order _orderTableService;
		private readonly AzureQueueService _queueService;
		private readonly string _inventoryQueueName = "inventory-queue";

		/// <summary>
		/// Constructor to initialize the table and queue services.
		/// </summary>
		/// <param name="productTableService">Service for handling Product entities.</param>
		/// <param name="queueService">Service for handling Azure Queue operations.</param>
		public ProcessOrderFunction(Product productTableService, Order orderTableService, AzureQueueService queueService)
		{
			_productTableService = productTableService;
			_orderTableService = orderTableService;
			_queueService = queueService;
		}

		/// <summary>
		/// Queue trigger function to process order messages and update inventory.
		/// </summary>
		/// <param name="myQueueItem">Queue message containing the order data.</param>
		[FunctionName("ProcessOrderFunction")]
		public async Task Run(
			[QueueTrigger("purchase-queue", Connection = "AzureWebJobsStorage")] string myQueueItem,
			ILogger log)
		{
			log.LogInformation($"Processing order message: {myQueueItem}");

			// Deserialize the order message
			if (!TryDeserializeOrderMessage(myQueueItem, out var orderMessage, log))
			{
				log.LogWarning("Failed to deserialize order message");
				return;
			}

			// Process the order and update inventory
			if (await ProcessOrderAndUpdateInventoryAsync(orderMessage, log))
			{
				log.LogInformation($"Order {orderMessage.OrderId} processed successfully");
				await LogInventoryUpdateAsync(orderMessage, log);
			}
			else
			{
				log.LogWarning($"Failed to process order {orderMessage.OrderId}");
			}
		}

		/// <summary>
		/// Tries to deserialize the order message from the queue.
		/// </summary>
		/// <param name="message">Queue message as a string.</param>
		/// <param name="orderMessage">Deserialized order message object.</param>
		/// <returns>True if deserialization is successful, otherwise false.</returns>
		private bool TryDeserializeOrderMessage(string message, out OrderMessage orderMessage, ILogger log)
		{
			try
			{
				orderMessage = JsonSerializer.Deserialize<OrderMessage>(message);
				return orderMessage != null;
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error deserializing order message");
				orderMessage = null;
				return false;
			}
		}

		/// <summary>
		/// Processes the order and updates the inventory in table storage.
		/// </summary>
		/// <param name="orderMessage">Order message containing the order details.</param>
		/// <returns>True if the order is processed and inventory is updated successfully, otherwise false.</returns>
		private async Task<bool> ProcessOrderAndUpdateInventoryAsync(OrderMessage orderMessage, ILogger log)
		{
				log.LogInformation($"Retrieving product {orderMessage.ProductId} from table storage");
				var dbProduct = await _productTableService.GetProductByIdAsync(orderMessage.ProductId);
				if (dbProduct == null)
				{
					log.LogWarning($"Product {orderMessage.ProductId} not found in table storage");
					return false;
				}

				if (dbProduct.Quantity < orderMessage.Quantity)
				{
					log.LogWarning($"Insufficient stock for product {orderMessage.ProductId}. Available: {dbProduct.Quantity}, Required: {orderMessage.Quantity}");
					return false;
				}

				// Update the product quantity
				dbProduct.Quantity -= orderMessage.Quantity;
				log.LogInformation($"Updating product {orderMessage.ProductId} quantity to {dbProduct.Quantity}");

				try
				{
					await _productTableService.UpdateProductAsync(dbProduct);
					await _orderTableService.RecordOrderAsync(orderMessage);
				}
				catch (RequestFailedException ex) when (ex.Status == 412)
				{
					log.LogWarning($"Concurrency conflict when updating product {orderMessage.ProductId}. Retrying...");
					return await ProcessOrderAndUpdateInventoryAsync(orderMessage, log);
				}

			return true;
		}

		/// <summary>
		/// Logs the inventory update by enqueuing a message to the inventory queue.
		/// </summary>
		/// <param name="orderMessage">Order message containing the order details.</param>
		private async Task LogInventoryUpdateAsync(OrderMessage orderMessage, ILogger log)
		{
			var inventoryUpdateMessage = new InventoryUpdateMessage
			(
				orderMessage.ProductId,
				-orderMessage.Quantity,
				"Order processed"
			);

			var messageText = JsonSerializer.Serialize(inventoryUpdateMessage);
			await _queueService.EnqueueMessageAsync(_inventoryQueueName, messageText);

			log.LogInformation($"Inventory update logged for order {orderMessage.OrderId}");
		}
	}
}
