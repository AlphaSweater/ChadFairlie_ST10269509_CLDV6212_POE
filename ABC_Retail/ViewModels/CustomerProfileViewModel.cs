using ABC_Retail.Models;
using System.ComponentModel.DataAnnotations;

namespace ABC_Retail.ViewModels
{
	public class CustomerProfileViewModel
	{
		public int CustomerId { get; set; } // Unique identifier for the customer

		public string Name { get; set; } // Name of the customer

		public string Surname { get; set; } // Surname of the customer

		public string Phone { get; set; } // Phone number of the customer

		public string Email { get; set; } // Email of the customer

		public string Password { get; set; } // Password of the customer

		public bool IsAdmin { get; set; } = false; // Admin status of the customer

		public List<OrderViewModel> OrderHistory { get; set; } // Order history of the customer

		public List<OrderViewModel>? AllOrders { get; set; } // All orders of the system

		// Constructor to turn model into view model
		public CustomerProfileViewModel(Customer customer, List<OrderViewModel> orderHistory, List<OrderViewModel>? allOrders = null)
		{
			CustomerId = customer.CustomerId;
			Name = customer.Name;
			Surname = customer.Surname;
			Phone = customer.Phone;
			Email = customer.Email;
			Password = customer.Password;
			IsAdmin = customer.IsAdmin;
			OrderHistory = orderHistory;
			AllOrders = allOrders ?? new List<OrderViewModel>();
		}

		// Default constructor
		public CustomerProfileViewModel()
		{
		}
	}
}
