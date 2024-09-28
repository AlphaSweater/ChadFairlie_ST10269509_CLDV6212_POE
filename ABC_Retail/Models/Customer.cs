using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
	/// <summary>
	/// Represents a customer entity in the application
	/// </summary>
	public class Customer : ITableEntity
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Required properties for ITableEntity
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Custom properties of the Customer entity
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		public string Name { get; set; } // Name of the customer
		public string Surname { get; set; } // Surname of the customer
		public string Email { get; set; } // Email of the customer
		public string Phone { get; set; } // Phone number of the customer

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		public Customer()
		{
			PartitionKey = "Customer"; // Set the partition key to "Customer"
			RowKey = Guid.NewGuid().ToString(); // Set the row key to a new GUID
			Name = string.Empty;
			Surname = string.Empty;
			Email = string.Empty;
			Phone = string.Empty;
		}
	}
}