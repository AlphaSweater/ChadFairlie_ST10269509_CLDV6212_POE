namespace ABC_Retail.ViewModels
{
	/// <summary>
	/// Represents a product in the view as a view model of the product entity
	/// </summary>
	public class ProductViewModel
	{
		// Product attributes

		public string? Id { get; set; } // Unique identifier for the product
		public string Name { get; set; } // Name of the product
		public double Price { get; set; } // Price of the product
		public string Description { get; set; } // Description of the product
		public int Quantity { get; set; } // Quantity of the product

		// Product Image attributes
		public string? FileName { get; set; } // Name of the product image file
		public string? FileUrl { get; set; } // URL of the product image file
		public IFormFile File { get; set; } // Image file to be uploaded

	}
}