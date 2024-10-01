using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ABC_Retail.Services;
using ABC_Retail.Models;

namespace ABC_Retail_Functions.Functions
{
	public class ProcessOrderFunction
	{
		private readonly ProductTableService _productTableService;
		private readonly AzureQueueService _queueService;
		private readonly string _inventoryQueueName = "inventory-queue";

		public ProcessOrderFunction(ProductTableService productTableService, AzureQueueService queueService)
		{
			_productTableService = productTableService;
			_queueService = queueService;
		}

		[FunctionName("ProcessOrderFunction")]
		public async Task Run(
			[QueueTrigger("purchase-queue", Connection = "AzureWebJobsStorage")] string myQueueItem,
			ILogger log)
		{
			log.LogInformation($"Processing order message: {myQueueItem}");

			if (!TryDeserializeOrderMessage(myQueueItem, out var orderMessage, log))
			{
				log.LogWarning("Failed to deserialize order message");
				return;
			}

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

				dbProduct.Quantity -= product.Quantity;
				log.LogInformation($"Updating product {product.ProductId} quantity to {dbProduct.Quantity}");
				await _productTableService.UpdateEntityAsync(dbProduct);
			}

			return true;
		}

		private async Task LogInventoryUpdateAsync(OrderMessage orderMessage, ILogger log)
		{
			foreach (var product in orderMessage.Products)
			{
				var inventoryUpdateMessage = new InventoryUpdateMessage
				(
					product.ProductName,
					-product.Quantity,
					"Order processed"
				);

				var messageText = JsonSerializer.Serialize(inventoryUpdateMessage);
				await _queueService.EnqueueMessageAsync(_inventoryQueueName, messageText);
			}

			log.LogInformation($"Inventory update logged for order {orderMessage.OrderId}");
		}
	}
}
