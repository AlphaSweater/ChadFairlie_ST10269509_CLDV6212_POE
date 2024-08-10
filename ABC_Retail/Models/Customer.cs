using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
	public class Customer : ITableEntity
	{
		public string PartitionKey { get; set; }
		public string RowKey { get; set; }
		public DateTimeOffset? Timestamp { get; set; }
		public ETag ETag { get; set; }

		// Custom properties

		public string Name { get; set; }
		public string Surname { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }

		public Customer()
		{
			PartitionKey = "Customer";
			RowKey = Guid.NewGuid().ToString();
		}
	}
}