using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
	public class CustomerController : Controller
	{
		public IActionResult Index()
		{
			// Fetch customer list (simplified example)
			var customers = new List<CustomerViewModel>
			{
				new CustomerViewModel { Id = 1, Name = "John Doe", Email = "john@example.com" },
				new CustomerViewModel { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
			};
			return View(customers);
		}

		public IActionResult Manage(int id)
		{
			// Fetch customer details by ID (simplified example)
			var customer = new CustomerViewModel { Id = id, Name = "John Doe", Email = "john@example.com", Phone = "123-456-7890", Address = "123 Elm St" };
			return View(customer);
		}
	}
}