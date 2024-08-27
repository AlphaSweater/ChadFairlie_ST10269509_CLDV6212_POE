using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
	/// <summary>
	/// Controller responsible for managing product-related actions such as displaying, editing, and deleting products,
	/// as well as handling product purchases.
	/// </summary>
	public class ProductController : Controller
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Dependencies
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Service for interacting with product Azure Table Storage.
		private readonly ProductTableService _productTableService;

		// Service for interacting with Azure Blob Storage.
		private readonly AzureBlobStorageService _blobStorageService;

		// Service for interacting with Azure Queue Storage.
		private readonly AzureQueueService _queueService;

		// Name of the queue used for processing purchase orders.
		private readonly string _purchaseQueueName = "purchase-queue";

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		/// <summary>
		/// Initializes a new instance of the ProductController with the specified services.
		/// </summary>
		/// <param name="productTableService">Service for interacting with product Azure Table Storage.</param>
		/// <param name="queueService">Service for interacting with Azure Queue Storage.</param>
		/// <param name="blobStorageService"> Service for interacting with Azure Blob Storage.</param>
		public ProductController(ProductTableService productTableService, AzureBlobStorageService blobStorageService, AzureQueueService queueService)
		{
			_productTableService = productTableService;
			_blobStorageService = blobStorageService;
			_queueService = queueService;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Index Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays a list of all products on the index page.
		/// </summary>
		/// <returns>A view displaying a list of all products.</returns>
		public async Task<IActionResult> Index()
		{
			// Retrieve all products from Azure Table Storage.
			var products = await _productTableService.GetAllEntitiesAsync();

			// Map products to the view model.
			var productViewModels = new List<ProductViewModel>();

			foreach (var product in products)
			{
				string fileName = null;
				string fileUrl = null;

				if (!string.IsNullOrEmpty(product.FileID))
				{
					var blobClient = await _blobStorageService.GetFileAsync(product.FileID);
					fileName = blobClient.Name;
					fileUrl = blobClient.Uri.ToString();
				}

				productViewModels.Add(new ProductViewModel
				{
					Id = product.RowKey,
					Name = product.Name,
					Price = product.Price,
					Description = product.Description,
					Quantity = product.Quantity,
					FileName = fileName, // Set the file name from Blob Storage or null
					FileUrl = fileUrl // Set the file URL from Blob Storage or null
				});
			}

			// Return the view with the list of products.
			return View(productViewModels);
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Manage Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays the details of a specific product for management.
		/// </summary>
		/// <param name="id">The ID of the product to manage.</param>
		/// <returns>A view displaying the product details.</returns>
		public async Task<IActionResult> Manage(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				// Return a bad request response if the product ID is null or empty.
				return BadRequest("Product ID cannot be null or empty.");
			}

			// Retrieve the product from Azure Table Storage.
			var product = await _productTableService.GetEntityAsync("Product", id);
			if (product == null)
			{
				// Return a not found response if the product does not exist.
				return NotFound();
			}

			string fileName = null;
			string fileUrl = null;

			if (!string.IsNullOrEmpty(product.FileID))
			{
				var blobClient = await _blobStorageService.GetFileAsync(product.FileID);
				fileName = blobClient.Name;
				fileUrl = blobClient.Uri.ToString();
			}

			// Map the product to the view model.
			var productViewModel = new ProductViewModel
			{
				Id = product.RowKey,
				Name = product.Name,
				Price = product.Price,
				Description = product.Description,
				Quantity = product.Quantity,
				FileName = fileName, // Set the file name from Blob Storage or null
				FileUrl = fileUrl // Set the file URL from Blob Storage or null
			};

			// Return the view with the product details.
			return View(productViewModel);
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Edit Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Updates the details of a specific product.
		/// </summary>
		/// <param name="model">The view model containing the updated product details.</param>
		/// <returns>A redirect to the index action after updating the product.</returns>
		[HttpPost]
		public async Task<IActionResult> Edit(ProductViewModel model)
		{
			if (string.IsNullOrEmpty(model.Id))
			{
				// Return a bad request response if the product ID is null or empty.
				return BadRequest("Product ID cannot be null or empty.");
			}

			if (!ModelState.IsValid)
			{
				// If the model state is invalid, return the view with the current model to display validation errors.
				return View(model);
			}

			// Retrieve the product from Azure Table Storage.
			var product = await _productTableService.GetEntityAsync("Product", model.Id);
			if (product == null)
			{
				// Return a not found response if the product does not exist.
				return NotFound();
			}

			// Update the product details with the values from the view model.
			product.Name = model.Name;
			product.Price = model.Price;
			product.Description = model.Description;
			product.Quantity = model.Quantity;

			// Update the product image file if a new file is uploaded.
			if (model.File != null)
			{
				// Delete the existing image file from Azure Blob Storage.
				await _blobStorageService.DeleteFileAsync(product.FileID);
				// Upload the new image file and get the file ID.
				product.FileID = _blobStorageService.UploadFileAsync(model.File).Result;
			}

			// Save the updated product back to Azure Table Storage.
			await _productTableService.UpdateEntityAsync(product);

			// Redirect to the index action.
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Delete Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Deletes a specific product.
		/// </summary>
		/// <param name="id">The ID of the product to delete.</param>
		/// <returns>A redirect to the index action after deleting the product.</returns>
		[HttpPost]
		public async Task<IActionResult> Delete(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				// Return a bad request response if the product ID is null or empty.
				return BadRequest("Product ID cannot be null or empty.");
			}

			// Retrieve the product from Azure Table Storage.
			var product = await _productTableService.GetEntityAsync("Product", id);
			if (product == null)
			{
				// Return a not found response if the product does not exist.
				return NotFound();
			}

			// Delete the existing image file from Azure Blob Storage.
			await _blobStorageService.DeleteFileAsync(product.FileID);

			// Delete the product from Azure Table Storage.
			await _productTableService.DeleteEntityAsync(product.PartitionKey, product.RowKey);

			// Redirect to the index action.
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Create Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays the create product form.
		/// </summary>
		/// <returns>A view displaying the create product form.</returns>
		public IActionResult Create()
		{
			// Return the view for creating a new product.
			return View();
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Handles the creation of a new product.
		/// </summary>
		/// <param name="model">The view model containing the new product details.</param>
		/// <returns>A redirect to the index action after creating the product.</returns>
		[HttpPost]
		public async Task<IActionResult> Create(ProductViewModel model)
		{
			if (!ModelState.IsValid)
			{
				// If the model state is invalid, return the view with the current model to display validation errors.
				// TODO: Handle validation errors (e.g., return the view with errors highlighted).
				return View(model);
			}

			// Create a new product entity from the view model.
			var product = new Product
			{
				Name = model.Name,
				Price = model.Price,
				Description = model.Description,
				Quantity = model.Quantity,
				FileID = _blobStorageService.UploadFileAsync(model.File).Result // Upload the image file and get the file ID
			};

			// Save the new product to Azure Table Storage.
			await _productTableService.AddEntityAsync(product);

			// Redirect to the index action.
			return RedirectToAction("Index");
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Purchase Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Handles the purchase of a specific product.
		/// </summary>
		/// <param name="id">The ID of the product to purchase.</param>
		/// <returns>A redirect to the index action after purchasing the product.</returns>
		[HttpPost]
		public async Task<IActionResult> Purchase(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				// Return a bad request response if the product ID is null or empty.
				return BadRequest("Product ID cannot be null or empty.");
			}

			// Retrieve the product from Azure Table Storage.
			var product = await _productTableService.GetEntityAsync("Product", id);
			if (product == null)
			{
				// Return a not found response if the product does not exist.
				return NotFound();
			}

			if (product.Quantity <= 0)
			{
				// Return a bad request response if the product is out of stock.
				return BadRequest("The product is out of stock.");
			}

			// Create an OrderMessage to enqueue for processing.
			var orderMessage = new OrderMessage
			{
				OrderId = Guid.NewGuid().ToString(), // Generate a new OrderId
				CustomerId = "CustomerIdPlaceholder", // Replace with actual customer ID
				Products = new List<OrderMessage.ProductOrder>
				{
					new OrderMessage.ProductOrder
					{
						ProductId = product.RowKey,
						ProductName = product.Name,
						Quantity = 1 // Assume purchasing 1 unit for simplicity
					}
				},
				OrderDate = DateTime.UtcNow,
				TotalAmount = product.Price // Assuming the total amount is the price of one product
			};

			// Enqueue the order message for processing.
			await _queueService.EnqueueMessageAsync(_purchaseQueueName, orderMessage);

			// Fetch the updated product from Azure Table Storage.
			product = await _productTableService.GetEntityAsync("Product", id);

			// Return the updated quantity as JSON
			return Json(new { quantity = product.Quantity });
		}
	}
}