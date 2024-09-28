using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;
using System.Text.Json;


namespace ABC_Retail.Models
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
		private string ProductIdsJson { get; set; }

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

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		public Order()
		{
			PartitionKey = "Order";
			RowKey = Guid.NewGuid().ToString();
			OrderDate = DateTime.UtcNow;
			ProductIds = new List<string>();
			CustomerId = string.Empty; // Initialize CustomerId
			ProductIdsJson = string.Empty; // Initialize ProductIdsJson
		}
	}
}