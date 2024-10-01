using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace ABC_Retail.Controllers
{
	/// <summary>
	/// Controller responsible for handling customer-related actions such as listing, managing,
	/// editing, deleting, and creating customers.
	/// </summary>
	public class CustomerController : Controller
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Dependencies
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Service for interacting with customer Azure Table Storage.
		private readonly CustomerTableService _customerTableService;

		// HTTP client for making requests to Azure Functions.
		private readonly HttpClient _httpClient;

		// The function URL for adding an entity.
		private readonly string _addEntityFunctionUrl;


		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomerController"/> class.
		/// </summary>
		/// <param name="customerTableService">The service for customer Azure Table Storage operations.</param>
		public CustomerController(CustomerTableService customerTableService, HttpClient httpClient, IConfiguration configuration)
		{
			_customerTableService = customerTableService;
			_httpClient = httpClient;
			_addEntityFunctionUrl = configuration["AzureFunctions:AddEntityFunctionUrl"] ?? throw new ArgumentNullException(nameof(configuration), "SendQueueMessageUrl configuration is missing.");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Index Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays a list of all customers on the index page.
		/// </summary>
		/// <returns>A view displaying a list of customer view models.</returns>
		public async Task<IActionResult> Index()
		{
			// Retrieve all customers from Azure Table Storage.
			var customers = await _customerTableService.GetAllEntitiesAsync();

			// Map the customer entities to customer view models.
			var customerViewModels = customers.Select(c => new CustomerViewModel
			{
				Id = c.RowKey,
				Name = c.Name,
				Surname = c.Surname,
				Email = c.Email,
				Phone = c.Phone
			}).ToList();

			// Return the view with the list of customer view models.
			return View(customerViewModels);
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Manage Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays the details of a specific customer for management.
		/// </summary>
		/// <param name="id">The unique identifier (RowKey) of the customer.</param>
		/// <returns>A view displaying the customer's details for management.</returns>
		public async Task<IActionResult> Manage(string id)
		{
			// Retrieve the customer from Azure Table Storage by PartitionKey and RowKey.
			var customer = await _customerTableService.GetEntityAsync("Customer", id);
			if (customer == null)
			{
				// Return a 404 Not Found response if the customer does not exist.
				return NotFound();
			}

			// Map the customer entity to a customer view model.
			var customerViewModel = new CustomerViewModel
			{
				Id = customer.RowKey,
				Name = customer.Name,
				Surname = customer.Surname,
				Email = customer.Email,
				Phone = customer.Phone
			};

			// Return the view with the customer view model.
			return View(customerViewModel);
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Edit Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Updates the details of a specific customer.
		/// </summary>
		/// <param name="model">The customer view model containing updated details.</param>
		/// <returns>A redirect to the index action if successful, or a view displaying the validation errors.</returns>
		[HttpPost]
		public async Task<IActionResult> Edit(CustomerViewModel model)
		{
			// Retrieve the customer entity from Azure Table Storage.
			var dbCustomer = await _customerTableService.GetEntityAsync("Customer", model.Id);
			if (dbCustomer == null)
			{
				// Return a 404 Not Found response if the customer does not exist.
				return NotFound();
			}

			// Update the customer entity with the details from the view model.
			dbCustomer.Name = model.Name;
			dbCustomer.Surname = model.Surname;
			dbCustomer.Email = model.Email;
			dbCustomer.Phone = model.Phone;

			// Save the updated customer entity back to Azure Table Storage.
			await _customerTableService.UpdateEntityAsync(dbCustomer, dbCustomer.ETag);

			// Redirect to the index action after successful update.
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Delete Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Deletes a specific customer.
		/// </summary>
		/// <param name="id">The unique identifier (RowKey) of the customer to be deleted.</param>
		/// <returns>A redirect to the index action after successful deletion.</returns>
		[HttpPost]
		public async Task<IActionResult> Delete(string id)
		{
			// Retrieve the customer entity from Azure Table Storage.
			var customer = await _customerTableService.GetEntityAsync("Customer", id);
			if (customer == null)
			{
				// Return a 404 Not Found response if the customer does not exist.
				return NotFound();
			}

			// Delete the customer entity from Azure Table Storage.
			await _customerTableService.DeleteEntityAsync(customer.PartitionKey, customer.RowKey);

			// Redirect to the index action after successful deletion.
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Create Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays a form to create a new customer.
		/// </summary>
		/// <returns>A view displaying the customer creation form.</returns>
		public IActionResult Create()
		{
			// Return the view for creating a new customer.
			return View(new CustomerViewModel());
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Handles the creation of a new customer.
		/// </summary>
		/// <param name="model">The customer view model containing the new customer details.</param>
		/// <returns>A redirect to the index action if successful, or a view displaying the validation errors.</returns>
		[HttpPost]
		public async Task<IActionResult> Create(CustomerViewModel model)
		{
			if (!ModelState.IsValid)
			{
				// TODO: Handle validation errors (e.g., return the view with errors highlighted).
				return View(model);
			}

			// Map the customer view model to a customer entity.
			var customer = new Customer(model.Name, model.Surname, model.Email, model.Phone);

			var functionUrl = _addEntityFunctionUrl;

			var jsonContent = JsonSerializer.Serialize(customer);
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync(functionUrl, content);
			if (!response.IsSuccessStatusCode)
			{
				return Json(new { success = false, message = "Failed to trigger the add entity function." });
			}

			return Json(new { success = true, message = "Customer added successfully." });
		}
	}
}