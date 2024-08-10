namespace ABC_Retail.ViewModels
{
	/// <summary>
	/// Represents a customer in the view as a view model of the customer entity
	/// </summary>
	public class CustomerViewModel
	{
		public string? Id { get; set; } // Unique identifier for the customer
		public string Name { get; set; } // Name of the customer
		public string Surname { get; set; } // Surname of the customer
		public string Email { get; set; } // Email of the customer
		public string Phone { get; set; } // Phone number of the customer
	}
}