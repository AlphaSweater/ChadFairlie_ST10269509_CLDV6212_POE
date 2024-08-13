using System.Text.Json;

namespace ABC_Retail.Models
{
	public class Message
	{
		// Common properties for all messages can be added here
		public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Example common property

		public static string GetMessageType(string messageText)
		{
			var jsonDoc = JsonDocument.Parse(messageText);
			if (jsonDoc.RootElement.TryGetProperty("OrderId", out _))
			{
				return "OrderMessage";
			}
			if (jsonDoc.RootElement.TryGetProperty("ProductId", out _) && jsonDoc.RootElement.TryGetProperty("QuantityChange", out _))
			{
				return "InventoryUpdateMessage";
			}
			return "Unknown";
		}
	}

	public class OrderMessage : Message
	{
		public string OrderId { get; set; } // Unique identifier for the order
		public string CustomerId { get; set; } // Identifier for the customer placing the order
		public List<ProductOrder> Products { get; set; } // List of products in the order

		public DateTime OrderDate { get; set; } // Date the order was placed
		public double TotalAmount { get; set; } // Total amount for the order

		public class ProductOrder
		{
			public string ProductId { get; set; } // Product identifier
			public string ProductName { get; set; } // Product name
			public int Quantity { get; set; } // Quantity of the product ordered
		}
	}

	public class InventoryUpdateMessage : Message
	{
		public string Name { get; set; } // Product identifier
		public int Quantity { get; set; } // Quantity change (positive for addition, negative for removal)
		public string Reason { get; set; } // Reason for the inventory update (optional)
	}
}