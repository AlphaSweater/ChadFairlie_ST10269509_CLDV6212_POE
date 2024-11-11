using System.ComponentModel.DataAnnotations;

namespace ABC_Retail.ViewModels
{
	//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
	/// <summary>
	/// Represents a user trying to log in as a view model.
	/// </summary>
	public class LoginViewModel
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Custom properties of the LoginUserViewModel entity
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		[Required(ErrorMessage = "An email address is required!")]
		[EmailAddress(ErrorMessage = "Invalid email address format.")]
		public string? Email { get; set; } // Email of the customer

		//--------------------------------------------------------------------------------------------------------------------------//
		[Required(ErrorMessage = "A password is required!")]
		[StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 6 characters long.")]
		public string? Password { get; set; } // Password of the customer
	}
}
