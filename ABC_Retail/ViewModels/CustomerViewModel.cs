using ABC_Retail.Models;
using System.ComponentModel.DataAnnotations;

namespace ABC_Retail.ViewModels
{
	/// <summary>
	/// Represents a customer in the view as a view model of the customer entity
	/// </summary>
	public class CustomerViewModel
	{
		public int CustomerId { get; set; } // Unique identifier for the customer

		[Required(ErrorMessage = "A customer name is required!")]
		[StringLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
		public string? Name { get; set; } // Name of the customer

		[Required(ErrorMessage = "A customer surname is required!")]
		[StringLength(50, ErrorMessage = "The surname cannot exceed 50 characters.")]
		public string? Surname { get; set; } // Surname of the customer

		[Required(ErrorMessage = "A phone number is required!")]
		[Phone(ErrorMessage = "Invalid phone number format.")]
		[StringLength(15, ErrorMessage = "The phone number cannot exceed 15 characters.")]
		public string? Phone { get; set; } // Phone number of the customer

		[Required(ErrorMessage = "An email address is required!")]
		[EmailAddress(ErrorMessage = "Invalid email address format.")]
		public string? Email { get; set; } // Email of the customer

		[Required(ErrorMessage = "A password is required!")]
		[StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 6 characters long.")]
		public string? Password { get; set; } // Password of the customer

		// Constructor to turn model into view model
		public CustomerViewModel(Customer customer)
		{
			CustomerId = customer.CustomerId;
			Name = customer.Name;
			Surname = customer.Surname;
			Phone = customer.Phone;
			Email = customer.Email;
			Password = customer.Password;
		}

		// Default constructor
		public CustomerViewModel()
		{
		}

	}
}