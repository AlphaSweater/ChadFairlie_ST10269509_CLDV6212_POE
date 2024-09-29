using System.ComponentModel.DataAnnotations;

namespace ABC_Retail_Shared.ViewModels
{
	/// <summary>
	/// Represents a customer in the view as a view model of the customer entity
	/// </summary>
	public class CustomerViewModel
	{
		public string? Id { get; set; } // Unique identifier for the customer

		[Required(ErrorMessage = "A customer name is required!")]
		[StringLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
		public string? Name { get; set; } // Name of the customer

		[Required(ErrorMessage = "A customer surname is required!")]
		[StringLength(50, ErrorMessage = "The surname cannot exceed 50 characters.")]
		public string? Surname { get; set; } // Surname of the customer

		[Required(ErrorMessage = "An email address is required!")]
		[EmailAddress(ErrorMessage = "Invalid email address format.")]
		public string? Email { get; set; } // Email of the customer

		[Required(ErrorMessage = "A phone number is required!")]
		[Phone(ErrorMessage = "Invalid phone number format.")]
		[StringLength(15, ErrorMessage = "The phone number cannot exceed 15 characters.")]
		public string? Phone { get; set; } // Phone number of the customer
	}
}