namespace ABC_Retail.ViewModels
{
	public class OrderViewModel
	{
		// Unique identifier for the order
		public int OrderId { get; set; }

		// The ID of the related Customer
		public int CustomerId { get; set; }

		public string CustomerFullName { get; set; }

		// The ID of the related Product
		public int ProductId { get; set; }

		public string ProductName { get; set; }

		public string ProductImageName { get; set; }

		// Total quantity of the product in the order
		public int TotalQuantity { get; set; }

		// Total amount for the order
		public decimal TotalAmount { get; set; }

		// Date the order was placed
		public DateTime OrderDate { get; set; }
	}
}
