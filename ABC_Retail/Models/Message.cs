using System.Text.Json;

namespace ABC_Retail.Models
{
	/// <summary>
	/// Base class for all message types, containing common properties and methods.
	/// </summary>
	public abstract class Message
	{
		// Common properties for all messages can be added here
		public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Example common property

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Determines the type of message based on the JSON structure of the provided message text.
		/// </summary>
		/// <param name="messageText">The JSON string representing the message.</param>
		/// <returns>A string representing the type of message: "OrderMessage", "InventoryUpdateMessage", 
		/// or "Unknown" if the message type cannot be determined.</returns>
		public static string GetMessageType(string messageText)
		{
			try
			{
				// Parse the JSON string
				using (JsonDocument jsonDoc = JsonDocument.Parse(messageText))
				{
					JsonElement root = jsonDoc.RootElement;

					// Check for OrderMessage by looking for an "OrderId" property in the JSON.
					if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("OrderId", out _))
					{
						return "OrderMessage";
					}

					// Check for InventoryUpdateMessage by looking for both "ProductId" and "QuantityChange" properties.
					if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("ProductId", out _) && root.TryGetProperty("QuantityChange", out _))
					{
						return "InventoryUpdateMessage";
					}

					// If neither type matches, return "Unknown".
					return "Unknown";
				}
			}
			catch (JsonException ex)
			{
				// Log the exception (assuming a logger is available)
				Console.WriteLine($"JSON parsing error: {ex.Message}");
				return "Unknown";
			}
			catch (Exception ex)
			{
				// Log the exception (assuming a logger is available)
				Console.WriteLine($"Unexpected error: {ex.Message}");
				return "Unknown";
			}
		}
	}

	//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
	/// <summary>
	/// Represents a message containing order information, inheriting from the base Message class.
	/// </summary>
	public class OrderMessage : Message
	{
		public string OrderId { get; set; } // Unique identifier for the order
		public string CustomerId { get; set; } // Identifier for the customer placing the order
		public List<ProductOrder>? Products { get; set; } // List of products in the order
		public DateTime OrderDate { get; set; } // Date the order was placed
		public double TotalAmount { get; set; } // Total amount for the order

		public OrderMessage(string customerId, List<ProductOrder> products, double totalAmount)
		{
			OrderId = Guid.NewGuid().ToString();
			CustomerId = customerId;
			Products = products;
			OrderDate = DateTime.UtcNow;
			TotalAmount = totalAmount;
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Represents a product within an order, including details such as product ID, name, and quantity.
		/// </summary>
		public class ProductOrder
		{
			public string ProductId { get; set; } // Unique identifier of the product ordered
			public string ProductName { get; set; } // Name of the product ordered
			public int Quantity { get; set; } // Quantity of the product ordered

			public ProductOrder(string productId, string productName, int quantity)
			{
				ProductId = productId;
				ProductName = productName;
				Quantity = quantity;
			}
		}
	}

	//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
	/// <summary>
	/// Represents a message indicating an inventory update, inheriting from the base Message class.
	/// </summary>
	public class InventoryUpdateMessage : Message
	{
		public string Name { get; set; } // // Name of the product being updated.
		public int Quantity { get; set; } // Quantity change (positive for addition, negative for removal)
		public string Reason { get; set; } // Reason for the inventory update

		public InventoryUpdateMessage(string name, int quantity, string reason)
		{
			Name = name;
			Quantity = quantity;
			Reason = reason;
		}
	}
}