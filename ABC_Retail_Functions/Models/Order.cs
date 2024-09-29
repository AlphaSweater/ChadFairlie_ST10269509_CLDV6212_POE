using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using static Azure.Core.HttpHeader;


namespace ABC_Retail_Functions.Models
{
	public class Order : ITableEntity
	{
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

		// Storing the RowKey of the related Customer entity
		public string CustomerId { get; set; }

		// Storing the list of Product RowKeys as a JSON string
		private string? ProductIdsJson { get; set; }

		// Not mapped to Table Storage, used for easier access in your code
		[IgnoreDataMember]
		public List<string> ProductIds
		{
			get => string.IsNullOrEmpty(ProductIdsJson)
				? new List<string>()
				: JsonSerializer.Deserialize<List<string>>(ProductIdsJson) ?? new List<string>();
			set => ProductIdsJson = JsonSerializer.Serialize(value);
		}

		// Other properties related to the order
		public DateTime OrderDate { get; set; }
		public decimal TotalAmount { get; set; }
		public string Status { get; set; }  // Pending, Shipped, Completed

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		public Order()
		{
			RowKey = Guid.NewGuid().ToString();  // Unique identifier for the order
			CustomerId = string.Empty;
			PartitionKey = CustomerId;  // Default partition, can be refined based on business logic
			OrderDate = DateTime.UtcNow;
			ProductIds = new List<string>();  // Initialize ProductIds list
			Status = "Pending";  // Default status
		}
	}
}