using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
	public class CustomerController : Controller
	{
		private readonly AzureTableStorageService _tableStorageService;

		public CustomerController(AzureTableStorageService tableStorageService)
		{
			_tableStorageService = tableStorageService;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Index Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Displays a list of all customers
		//--------------------------------------------------------------------------------------------------------------------------//
		public async Task<IActionResult> Index()
		{
			var customers = await _tableStorageService.GetAllCustomersAsync();
			var customerViewModels = customers.Select(c => new CustomerViewModel
			{
				Id = c.RowKey,
				Name = c.Name,
				Surname = c.Surname,
				Email = c.Email,
				Phone = c.Phone
			}).ToList();

			return View(customerViewModels);
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Manage Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Displays the details of a specific customer for management
		//--------------------------------------------------------------------------------------------------------------------------//
		public async Task<IActionResult> Manage(string id)
		{
			var customer = await _tableStorageService.GetCustomerAsync("Customer", id);
			if (customer == null)
			{
				return NotFound();
			}

			var customerViewModel = new CustomerViewModel
			{
				Id = customer.RowKey,
				Name = customer.Name,
				Surname = customer.Surname,
				Email = customer.Email,
				Phone = customer.Phone
			};

			return View(customerViewModel);
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Edit Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Updates the details of a specific customer
		//--------------------------------------------------------------------------------------------------------------------------//
		[HttpPost]
		public async Task<IActionResult> Edit(CustomerViewModel model)
		{
			var customer = await _tableStorageService.GetCustomerAsync("Customer", model.Id);
			if (customer == null)
			{
				return NotFound();
			}

			customer.Name = model.Name;
			customer.Surname = model.Surname;
			customer.Email = model.Email;
			customer.Phone = model.Phone;

			await _tableStorageService.UpdateCustomerAsync(customer);
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Delete Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Deletes a specific customer
		//--------------------------------------------------------------------------------------------------------------------------//
		[HttpPost]
		public async Task<IActionResult> Delete(string id)
		{
			var customer = await _tableStorageService.GetCustomerAsync("Customer", id);
			if (customer == null)
			{
				return NotFound();
			}

			await _tableStorageService.DeleteCustomerAsync(customer.PartitionKey, customer.RowKey);
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Create Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Displays a form to create a new customer
		//--------------------------------------------------------------------------------------------------------------------------//
		public IActionResult Create()
		{
			return View();
		}

		// Handles the creation of a new customer
		//--------------------------------------------------------------------------------------------------------------------------//
		[HttpPost]
		public async Task<IActionResult> Create(CustomerViewModel model)
		{
			if (!ModelState.IsValid)
			{
				// TODO: Handle validation errors
				return View(model);
			}

			var customer = new Customer
			{
				Name = model.Name,
				Surname = model.Surname,
				Email = model.Email,
				Phone = model.Phone
			};

			await _tableStorageService.AddCustomerAsync(customer);
			return RedirectToAction("Index");
		}
	}
}