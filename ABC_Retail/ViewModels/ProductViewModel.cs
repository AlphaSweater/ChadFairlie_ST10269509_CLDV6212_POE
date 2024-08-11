namespace ABC_Retail.ViewModels
{
	/// <summary>
	/// Represents a product in the view as a view model of the product entity
	/// </summary>
	public class ProductViewModel
	{
		public string? Id { get; set; } // Unique identifier for the product
		public string Name { get; set; } // Name of the product
		public double Price { get; set; } // Price of the product
		public string Description { get; set; } // Description of the product
		public int Quantity { get; set; } // Quantity of the product
	}
}