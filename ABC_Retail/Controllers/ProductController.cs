using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;

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

		// The HTTP client for making HTTP requests.
		private readonly HttpClient _httpClient;

		// The function URL for sending a queue message.
		private readonly string _sendToQueueFunctionUrl;

		// The function URL for adding an entity.
		private readonly string _addEntityFunctionUrl;

		// The function URL for uploading an image.
		private readonly string _uploadImageFunctionUrl;

		// Name of the queue used for processing purchase orders.
		private readonly string _purchaseQueueName = "purchase-queue";

		private readonly string _defaultProductImage = "default-product-image.jpg";

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		/// <summary>
		/// Initializes a new instance of the ProductController with the specified services.
		/// </summary>
		/// <param name="productTableService">Service for interacting with product Azure Table Storage.</param>
		/// <param name="queueService">Service for interacting with Azure Queue Storage.</param>
		/// <param name="blobStorageService"> Service for interacting with Azure Blob Storage.</param>
		/// <param name="httpClient">The HTTP client for making HTTP requests.</param>"
		public ProductController(ProductTableService productTableService, AzureBlobStorageService blobStorageService, HttpClient httpClient, IConfiguration configuration)
		{
			_productTableService = productTableService;
			_blobStorageService = blobStorageService;
			_httpClient = httpClient;
			_sendToQueueFunctionUrl = configuration["AzureFunctions:SendToQueueFunctionUrl"] ?? throw new ArgumentNullException(nameof(configuration), "SendQueueMessageUrl configuration is missing.");
			_addEntityFunctionUrl = configuration["AzureFunctions:AddEntityFunctionUrl"] ?? throw new ArgumentNullException(nameof(configuration), "SendQueueMessageUrl configuration is missing.");
			_uploadImageFunctionUrl = configuration["AzureFunctions:UploadImageFunctionUrl"] ?? throw new ArgumentNullException(nameof(configuration), "UploadImageFunctionUrl configuration is missing.");
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
				string? fileName = null;
				string? fileUrl = null;

				if (!string.IsNullOrEmpty(product.FileID))
				{
					var blobClient = _blobStorageService.GetFile(product.FileID);
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

			string? fileName = null;
			string? fileUrl = null;

			if (!string.IsNullOrEmpty(product.FileID))
			{
				var blobClient = _blobStorageService.GetFile(product.FileID);
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
			product.Name = model.Name ?? string.Empty;
			product.Price = model.Price;
			product.Description = model.Description ?? string.Empty;
			product.Quantity = model.Quantity;

			// Update the product image file if a new file is uploaded.
			if (model.File != null)
			{
				if (product.FileID != null && model.FileName != _defaultProductImage)
				{
					// Delete the existing image file from Azure Blob Storage.
					await _blobStorageService.DeleteFileAsync(product.FileID);
				}

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

			// Delete the product image file from Azure Blob Storage if it is not the default image.
			if (!string.IsNullOrEmpty(product.FileID) && product.FileID != _defaultProductImage)
			{
				// Delete the existing image file from Azure Blob Storage.
				await _blobStorageService.DeleteFileAsync(product.FileID);
			}

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
			return View(new ProductViewModel());
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

			string fileID = string.Empty;

			if (model.File == null)
			{
				fileID = _defaultProductImage; // Set a default image file ID
			}
			else
			{
				var imageFunctionUrl = _uploadImageFunctionUrl;

				// Create a multipart form content to send the file
				using var imageContent = new MultipartFormDataContent();
				using var fileStream = model.File.OpenReadStream();
				var fileContent = new StreamContent(fileStream);
				imageContent.Add(fileContent, "file", model.File.FileName);

				// Send the file to the Azure Function
				var blobResponse = await _httpClient.PostAsync(imageFunctionUrl, imageContent);

				if (blobResponse.IsSuccessStatusCode)
				{
					fileID = await blobResponse.Content.ReadAsStringAsync(); // Get the blob name from the function response
				}
				else
				{
					fileID = _defaultProductImage; // Set a default image file ID
				}
			}

			// Create a new product entity from the view model.
			var product = new Product(
				model.Name ?? string.Empty,
				model.Price,
				model.Description ?? string.Empty,
				model.Quantity,
				fileID
			);

			var productFunctionUrl = _addEntityFunctionUrl;

			var jsonContent = JsonSerializer.Serialize(product);
			var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync(productFunctionUrl, content);
			if (!response.IsSuccessStatusCode)
			{
				return Json(new { success = false, message = "Failed to trigger the add entity function." });
			}

			return Json(new { success = true, message = "Product added successfully." });
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
				return BadRequest("Product ID cannot be null or empty.");
			}

			var product = await _productTableService.GetEntityAsync("Product", id);
			if (product == null)
			{
				return NotFound();
			}

			if (product.Quantity <= 0)
			{
				return Json(new { success = false, message = "The product is out of stock." });
			}

			var orderMessage = new OrderMessage(
				"CustomerIdPlaceholder",
				new List<OrderMessage.ProductOrder>
				{
			new OrderMessage.ProductOrder
			(
				product.RowKey,
				product.Name,
				1
			)
				},
				product.Price
			);

			var functionUrl = _sendToQueueFunctionUrl;
			var requestData = new
			{
				Message = JsonSerializer.Serialize(orderMessage),
				QueueName = _purchaseQueueName
			};
			var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync(functionUrl, content);
			if (!response.IsSuccessStatusCode)
			{
				return Json(new { success = false, message = "Failed to trigger the order function." });
			}

			return Json(new { success = true, message = "Order successfully placed." });
		}
	}
}