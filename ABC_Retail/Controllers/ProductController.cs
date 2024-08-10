using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
	public class ProductController : Controller
	{
		public IActionResult Index()
		{
			// Fetch product list (simplified example)
			var products = new List<ProductViewModel>
			{
				new ProductViewModel { Id = 1, Name = "Product A", Price = 9.99m, Description = "Description for Product A" },
				new ProductViewModel { Id = 2, Name = "Product B", Price = 19.99m, Description = "Description for Product B" }
			};
			return View(products);
		}

		public IActionResult Manage(int id)
		{
			// Fetch product details by ID (simplified example)
			var product = new ProductViewModel { Id = id, Name = "Product A", Price = 9.99m, Description = "Description for Product A" };
			return View(product);
		}
	}
}