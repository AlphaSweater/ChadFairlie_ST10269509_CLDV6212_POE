using ABC_Retail.Models;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace ABC_Retail.Controllers
{
	public class HomeController : Controller
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Dependencies
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly Customer _customerTableService;
		private readonly ILogger<HomeController> _logger;

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Initializes a new instance of the <see cref="HomeController"/> class.
		/// </summary>
		/// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
		/// <param name="userTableService">Service for interacting with the user table.</param>
		/// <param name="configuration">Application configuration settings.</param>
		/// <param name="logger">Logger for logging information and errors.</param>

		public HomeController(IHttpContextAccessor httpContextAccessor, Customer customerService, ILogger<HomeController> logger)
		{
			_httpContextAccessor = httpContextAccessor;
			_customerTableService = customerService;
			_logger = logger;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Index Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		public IActionResult Index()
		{
			return View();
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Login Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays the login page.
		/// </summary>
		/// <returns>A view for the login page.</returns>
		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Handles the login process for a user.
		/// </summary>
		/// <param name="model">The login user view model containing email and password.</param>
		/// <returns>A redirect to the appropriate page based on user role, or the login view with errors.</returns>
		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			try
			{
				Customer userToLogin = new Customer(model);

				int customerId = await _customerTableService.ValidateUserAsync(userToLogin);

				if (customerId == 0)
				{
					ModelState.AddModelError(string.Empty, "Incorrect email or password.");
					return View(model);
				}

				_httpContextAccessor.HttpContext?.Session.SetInt32("CustomerId", customerId);

				string redirectUrl = Url.Action("Index", "Product");
				return Redirect(redirectUrl);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Failed to login user.");
				ModelState.AddModelError(string.Empty, "Failed to login user.");
				return View(model);
			}
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Sign-Up Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays the sign-up page.
		/// </summary>
		/// <returns>A view for the sign-up page.</returns>
		[HttpGet]
		public IActionResult SignUp()
		{
			return View();
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Handles the sign-up process for a new user.
		/// </summary>
		/// <param name="model">The sign-up user view model containing user details.</param>
		/// <returns>A JSON result indicating success or failure, or the sign-up view with errors.</returns>
		[HttpPost]
		public async Task<IActionResult> SignUp(SignUpViewModel model)
		{
			if (ModelState.IsValid)
			{
				Customer newCustomer = new Customer(model);

				int customerId = await _customerTableService.InsertUserAsync(newCustomer);

				if (customerId != 0)
				{
					// Set the user's ID in the session
					_httpContextAccessor.HttpContext?.Session.SetInt32("CustomerId", customerId);
					return Json(new { success = true });
				}
				else
				{
					return Json(new { success = false, message = "Failed to create user." });
				}
			}
			return Json(new { success = false, message = "Invalid data." });
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Sign-Out Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Handles the sign-out process for a user.
		/// </summary>
		/// <returns>A redirect to the login page.</returns>
		[HttpPost]
		public IActionResult SignOut()
		{
			HttpContext.Session.Clear();
			return RedirectToAction("Login");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
