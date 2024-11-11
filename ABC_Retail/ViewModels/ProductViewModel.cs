using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ABC_Retail.ViewModels
{
	/// <summary>
	/// Represents a product in the view as a view model of the product entity
	/// </summary>
	public class ProductViewModel
	{
		// Product attributes

		public int? ProductID { get; set; } // Unique identifier for the product

		public int? CustomerID { get; set; } // Unique identifier for the customer

		[Required(ErrorMessage = "A product name is required!")]
		[StringLength(100, ErrorMessage = "The product name cannot exceed 100 characters.")]
		public string? ProductName { get; set; } // Name of the product

		[Required(ErrorMessage = "Price is required")]
		[Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
		[RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Price must be within 2 decimal places")]
		public decimal ProductPrice { get; set; } // Price of the product

		[Required(ErrorMessage = "Description is required")]
		[StringLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
		public string? ProductDescription { get; set; } // Description of the product

		[Required(ErrorMessage = "Quantity is required")]
		[Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
		[RegularExpression(@"^\d+$", ErrorMessage = "Quantity must be a whole number")]
		public int ProductQuantity { get; set; } // Quantity of the product

		public bool ProductAvailability { get; set; } // Availability of the product

		// Product Image attributes
		public string? ProductImageName { get; set; } // Name of the product image file

		public string? ImageFileName { get; set; } // Name of the product image file
		public string? ImageUrl { get; set; } // URL of the product image file

		public IFormFile? File { get; set; } // Image file to be uploaded
	}
}