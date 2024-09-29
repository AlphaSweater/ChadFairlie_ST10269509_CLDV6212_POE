using System;
using Azure.Storage.Queues;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ABC_Retail_Shared.Services;
using ABC_Retail_Shared.Models;

namespace ABC_Retail_Functions.Functions
{
	public class ProcessOrderFunction
	{
		private readonly ProductTableService _productTableService;
		private readonly Func<string, QueueClient> _queueClientFactory;
		private readonly string _inventoryQueueName = "inventory-queue";

		public ProcessOrderFunction(ProductTableService productTableService, Func<string, QueueClient> queueClientFactory)
		{
			_productTableService = productTableService;
			_queueClientFactory = queueClientFactory;
		}

		[FunctionName("ProcessOrderFunction")]
		public async Task Run(
	[QueueTrigger("purchase-queue", Connection = "AzureWebJobsStorage")] string myQueueItem,
	ILogger log)
		{
			log.LogInformation($"Processing order message: {myQueueItem}");

			try
			{
				// Deserialize the order message
				var orderMessage = JsonSerializer.Deserialize<OrderMessage>(myQueueItem);
				if (orderMessage == null)
				{
					log.LogWarning("Failed to deserialize order message");
					return;
				}

				// Process the order
				bool success = await ProcessOrderAndUpdateInventoryAsync(orderMessage, log);
				if (success)
				{
					log.LogInformation($"Order {orderMessage.OrderId} processed successfully");
					await LogInventoryUpdateAsync(orderMessage, log);
				}
				else
				{
					log.LogWarning($"Failed to process order {orderMessage.OrderId}");
				}
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error processing order message");
			}
		}

		private async Task<bool> ProcessOrderAndUpdateInventoryAsync(OrderMessage orderMessage, ILogger log)
		{
			foreach (var product in orderMessage.Products)
			{
				log.LogInformation($"Retrieving product {product.ProductId} from table storage");
				var dbProduct = await _productTableService.GetEntityAsync("Product", product.ProductId);
				if (dbProduct == null)
				{
					log.LogWarning($"Product {product.ProductId} not found in table storage");
					return false;
				}

				if (dbProduct.Quantity < product.Quantity)
				{
					log.LogWarning($"Insufficient stock for product {product.ProductId}. Available: {dbProduct.Quantity}, Required: {product.Quantity}");
					return false;
				}

				// Deduct quantity and update the table
				dbProduct.Quantity -= product.Quantity;
				log.LogInformation($"Updating product {product.ProductId} quantity to {dbProduct.Quantity}");
				await _productTableService.UpdateEntityAsync(dbProduct);
			}

			return true; // Order processed successfully
		}

		private async Task LogInventoryUpdateAsync(OrderMessage orderMessage, ILogger log)
		{
			var inventoryQueueClient = _queueClientFactory(_inventoryQueueName);

			foreach (var product in orderMessage.Products)
			{
				var inventoryUpdateMessage = new InventoryUpdateMessage
				(
					product.ProductName,
					-product.Quantity, // Negative quantity for stock deduction
					"Order processed"
				);

				var messageText = JsonSerializer.Serialize(inventoryUpdateMessage);
				await inventoryQueueClient.SendMessageAsync(messageText);
			}

			log.LogInformation($"Inventory update logged for order {orderMessage.OrderId}");
		}

	}
}
