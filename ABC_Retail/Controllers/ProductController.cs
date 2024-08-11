using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
	public class ProductController : Controller
	{
		private readonly AzureTableStorageService _tableStorageService;
		private readonly AzureQueueService _queueService;

		public ProductController(AzureTableStorageService tableStorageService, AzureQueueService queueService)
		{
			_tableStorageService = tableStorageService;
			_queueService = queueService;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Index Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		// Displays a list of all products
		public async Task<IActionResult> Index()
		{
			var products = await _tableStorageService.GetAllProductsAsync();
			var productViewModels = products.Select(p => new ProductViewModel
			{
				Id = p.RowKey,
				Name = p.Name,
				Price = p.Price,
				Description = p.Description,
				Quantity = p.Quantity
			}).ToList();

			return View(productViewModels);
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Manage Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		// Displays the details of a specific product for management
		public async Task<IActionResult> Manage(string id)
		{
			var product = await _tableStorageService.GetProductAsync("Product", id);
			if (product == null)
			{
				return NotFound();
			}

			var productViewModel = new ProductViewModel
			{
				Id = product.RowKey,
				Name = product.Name,
				Price = product.Price,
				Description = product.Description,
				Quantity = product.Quantity
			};

			return View(productViewModel);
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Edit Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		// Updates the details of a specific product
		[HttpPost]
		public async Task<IActionResult> Edit(ProductViewModel model)
		{
			var product = await _tableStorageService.GetProductAsync("Product", model.Id);
			if (product == null)
			{
				return NotFound();
			}

			product.Name = model.Name;
			product.Price = model.Price;
			product.Description = model.Description;
			product.Quantity = model.Quantity;

			await _tableStorageService.UpdateProductAsync(product);
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Delete Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		// Deletes a specific product
		[HttpPost]
		public async Task<IActionResult> Delete(string id)
		{
			var product = await _tableStorageService.GetProductAsync("Product", id);
			if (product == null)
			{
				return NotFound();
			}

			await _tableStorageService.DeleteProductAsync(product.PartitionKey, product.RowKey);
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Create Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		// Displays the create product form
		public IActionResult Create()
		{
			return View();
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Handles the creation of a new product
		[HttpPost]
		public async Task<IActionResult> Create(ProductViewModel model)
		{
			if (!ModelState.IsValid)
			{
				// TODO: Handle validation errors
				return View(model);
			}

			var product = new Product
			{
				Name = model.Name,
				Price = model.Price,
				Description = model.Description,
				Quantity = model.Quantity
			};

			await _tableStorageService.AddProductAsync(product);
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Create Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		// Handles product purchase and queues the operation
		public async Task<IActionResult> Purchase(string id)
		{
			var product = await _tableStorageService.GetProductAsync("Product", id);
			if (product == null || product.Quantity <= 0)
			{
				return NotFound("Product not available or out of stock.");
			}

			// Enqueue a purchase message
			var purchaseMessage = new
			{
				ProductId = product.RowKey,
				ProductName = product.Name,
				Quantity = 1 // Assume purchasing 1 unit for simplicity
			};

			await _queueService.EnqueueMessageAsync(purchaseMessage);

			// Redirect to the HomeController Index action
			return RedirectToAction("Index", "Home");
		}
	}
}