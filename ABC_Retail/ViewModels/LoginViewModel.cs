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
		[EmailAddress(ErrorMessage = "Invalid email address format.")]
		public string? Email { get; set; } // Email of the customer

		//--------------------------------------------------------------------------------------------------------------------------//
		public string? Password { get; set; } // Password of the customer
	}
}
