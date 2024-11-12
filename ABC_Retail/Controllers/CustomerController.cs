using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

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

		// Service for interacting with customer SQL Table Storage.
		private readonly Customer _customerTableService;

		private readonly Order _orderTableService;

		// HTTP client for making requests to Azure Functions.
		private readonly HttpClient _httpClient;

		private readonly IHttpContextAccessor _httpContextAccessor;

		// The function URL for adding an entity.
		private readonly string _addEntityFunctionUrl;


		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomerController"/> class.
		/// </summary>
		/// <param name="customerTableService">The service for customer Azure Table Storage operations.</param>
		public CustomerController(Customer customerTableService, Order orderTableService, HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
		{
			_customerTableService = customerTableService;
			_orderTableService = orderTableService;
			_httpClient = httpClient;
			_httpContextAccessor = httpContextAccessor;
			_addEntityFunctionUrl = configuration["AzureFunctions:AddEntityFunctionUrl"] ?? throw new ArgumentNullException(nameof(configuration), "SendQueueMessageUrl configuration is missing.");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Index Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			int customerId = _httpContextAccessor?.HttpContext?.Session.GetInt32("CustomerId") ?? 0;
			bool isAdmin = _httpContextAccessor?.HttpContext?.Session.GetString("IsAdmin") == "true";

			CustomerProfileViewModel customerProfile;

			// Retrieve logged in customer from SQL Table Storage.
			var customer = await _customerTableService.GetUserByIdAsync(customerId);
			if (customer == null)
			{
				// Return a 404 Not Found response if the customer does not exist.
				return NotFound();
			}

			// Retrieve order history for the customer from SQL Table Storage.
			var orderHistory = await _orderTableService.GetOrdersByCustomerIdAsync(customerId);

			if (isAdmin)
			{
				// If the logged in customer is an admin, retrieve all customer orders from SQL Table Storage.
				var allOrders = await _orderTableService.GetAllOrdersAsync();
				customerProfile = new CustomerProfileViewModel(customer, orderHistory, allOrders);
			}
			else
			{
				// If the logged in customer is not an admin, only show their own order history.
				customerProfile = new CustomerProfileViewModel(customer, orderHistory);
			}

			// Return the view with the customer profile view model.
			return View(customerProfile);
		}



		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Manage Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays the details of a specific customer for management.
		/// </summary>
		/// <param name="customerId">The unique identifier (RowKey) of the customer.</param>
		/// <returns>A view displaying the customer's details for management.</returns>
		public async Task<IActionResult> Manage(int customerId)
		{
			// Retrieve the customer from Azure Table Storage by PartitionKey and RowKey.
			var dbCustomer = await _customerTableService.GetUserByIdAsync(customerId);
			if (dbCustomer == null)
			{
				// Return a 404 Not Found response if the customer does not exist.
				return NotFound();
			}

			// Map the customer entity to a customer view model.
			var customerViewModel = new CustomerViewModel(dbCustomer);

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
			var dbCustomer = await _customerTableService.GetUserByIdAsync(model.CustomerId);
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
			await _customerTableService.UpdateUserAsync(dbCustomer);

			// Redirect to the index action after successful update.
			return RedirectToAction("Index");
		}
	}
}