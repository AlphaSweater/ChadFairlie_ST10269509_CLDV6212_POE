using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;

namespace ABC_Retail.Models
{
	/// <summary>
	/// Represents a product entity in the application
	/// </summary>
	public class Product : ITableEntity
	{
		// The type of the entity
		public string EntityType { get; set; }

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Required properties for ITableEntity
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Custom properties of the Product entity
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		public string Name { get; set; } // Name of the product
		public double Price { get; set; } // Price of the product
		public string Description { get; set; } // Description of the product
		public int Quantity { get; set; } // Quantity of the product
		public string FileID { get; set; } // ID of the product image file

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		public Product(string name, double price, string description, int quantity, string fileID)
		{
			EntityType = "Product";
			PartitionKey = "Product"; // Set the partition key to "Product"
			RowKey = Guid.NewGuid().ToString(); // Set the row key to a new GUID
			Name = name;
			Price = price;
			Description = description;
			Quantity = quantity;
			FileID = fileID;
		}
		public Product()
		{
			EntityType = "Product";
			PartitionKey = "Product"; // Set the partition key to "Product"
			RowKey = Guid.NewGuid().ToString(); // Set the row key to a new GUID
			Name = string.Empty;
			Price = 0;
			Description = string.Empty;
			Quantity = 0;
			FileID = "default-product-image.jpg";
		}
	}
}