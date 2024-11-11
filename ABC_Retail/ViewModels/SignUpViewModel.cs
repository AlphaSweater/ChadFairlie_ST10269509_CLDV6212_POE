using System.ComponentModel.DataAnnotations;

namespace ABC_Retail.ViewModels
{
	public class SignUpViewModel
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Custom properties of the SignUpUserViewModel entity
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		[Required(ErrorMessage = "First name is required.")]
		[StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
		public string Name { get; set; }

		//--------------------------------------------------------------------------------------------------------------------------//
		[Required(ErrorMessage = "Last name is required.")]
		[StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
		public string Surname { get; set; }

		//--------------------------------------------------------------------------------------------------------------------------//
		[Required(ErrorMessage = "Phone number is required.")]
		[Phone(ErrorMessage = "Please enter a valid phone number.")]
		[StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
		public string Phone { get; set; }

		//--------------------------------------------------------------------------------------------------------------------------//
		[Required(ErrorMessage = "Email is required.")]
		[EmailAddress(ErrorMessage = "Please enter a valid email address.")]
		public string Email { get; set; }

		//--------------------------------------------------------------------------------------------------------------------------//
		[Required(ErrorMessage = "Password is required.")]
		[StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
		public string Password { get; set; }

		//--------------------------------------------------------------------------------------------------------------------------//
		[Required(ErrorMessage = "Confirmation of password is required.")]
		[Compare("Password", ErrorMessage = "Passwords do not match.")]
		public string ConfirmPassword { get; set; }
	}
}
