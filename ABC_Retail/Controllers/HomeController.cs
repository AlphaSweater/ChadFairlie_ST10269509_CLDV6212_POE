using ABC_Retail.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using static System.Net.WebRequestMethods;

namespace ABC_Retail.Controllers
{
	public class HomeController : Controller
	{
		private static readonly HttpClient client = new HttpClient();

		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		public async Task<IActionResult> SendMessage()
		{
			var functionUrl = "https://cldv-functions.azurewebsites.net/api/ProcessQueueMessage?code=xS4TM0xuwIhn7tYg8PIRmL_asDoietCxkzCPwH-7xkhfAzFufO9JCg%3D%3D";

			var response = await client.PostAsync(functionUrl, null); // No body sent in this case
			var responseString = await response.Content.ReadAsStringAsync();

			ViewBag.Response = responseString;
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}