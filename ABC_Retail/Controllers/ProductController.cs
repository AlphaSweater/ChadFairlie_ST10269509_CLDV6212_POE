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

		// Service for interacting with product SQL Table Storage.
		private readonly Product _productTableService;

		// Service for interacting with Azure Blob Storage.
		private readonly AzureBlobStorageService _blobStorageService;

		// The HTTP client for making HTTP requests.
		private readonly HttpClient _httpClient;

		private readonly IHttpContextAccessor _httpContextAccessor;

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
		public ProductController(Product productTableService, AzureBlobStorageService blobStorageService, HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
		{
			_productTableService = productTableService;
			_blobStorageService = blobStorageService;
			_httpClient = httpClient;
			_httpContextAccessor = httpContextAccessor;
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
			// Retrieve all products from SQL Table Storage.
			var productViewModels = await _productTableService.ListAvailableProductsAsync();

			foreach (var product in productViewModels)
			{
				if (!string.IsNullOrEmpty(product.ProductImageName))
				{
					var blobClient = _blobStorageService.GetFile(product.ProductImageName);
					product.ImageFileName = blobClient.Name;
					product.ImageUrl = blobClient.Uri.ToString();
				}
				else
				{
					var blobClient = _blobStorageService.GetFile(_defaultProductImage);
					product.ImageFileName = blobClient.Name;
					product.ImageUrl = blobClient.Uri.ToString();
				}
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
		/// <param name="productId">The ID of the product to manage.</param>
		/// <returns>A view displaying the product details.</returns>
		public async Task<IActionResult> Manage(int productId)
		{
			if (productId != null)
			{
				// Return a bad request response if the product ID is null or empty.
				return BadRequest("Product ID cannot be null or empty.");
			}

			// Retrieve the product from Azure Table Storage.
			var product = await _productTableService.GetProductByIdAsync(productId);
			if (product == null)
			{
				// Return a not found response if the product does not exist.
				return NotFound();
			}

			string? fileName = null;
			string? fileUrl = null;

			string? imageName = await product.GetProductImageNameAsync();

			if (!string.IsNullOrEmpty(imageName))
			{
				var blobClient = _blobStorageService.GetFile(imageName);
				fileName = blobClient.Name;
				fileUrl = blobClient.Uri.ToString();
			}

			// Map the product to the view model.
			var productViewModel = new ProductViewModel
			{
				ProductID = product.ProductID,
				ProductName = product.Name,
				ProductPrice = product.Price,
				ProductDescription = product.Description,
				ProductQuantity = product.Quantity,
				ProductImageName = fileName, // Set the file name from Blob Storage or null
				ImageUrl = fileUrl // Set the file URL from Blob Storage or null
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
			if (model.ProductID != null)
			{
				// Return a bad request response if the product ID is null or empty.
				return BadRequest("Product ID cannot be null or empty.");
			}

			if (!ModelState.IsValid)
			{
				// If the model state is invalid, return the view with the current model to display validation errors.
				return View(model);
			}

			if (model.ProductID == null)
			{
				// Return a bad request response if the product ID is null or empty.
				return BadRequest("Product ID cannot be null or empty.");
			}

			var dbProduct = await _productTableService.GetProductByIdAsync(model.ProductID.Value);
			if (dbProduct == null)
			{
				// Return a not found response if the product does not exist.
				return NotFound();
			}

			// Update the product details with the values from the view model.
			dbProduct.Name = model.ProductName ?? string.Empty;
			dbProduct.Price = model.ProductPrice;
			dbProduct.Description = model.ProductDescription ?? string.Empty;
			dbProduct.Quantity = model.ProductQuantity;

			// Update the product image file if a new file is uploaded.
			if (model.File != null)
			{
				var imageName = await dbProduct.GetProductImageNameAsync();

				if (imageName != null && model.ProductImageName != _defaultProductImage)
				{
					// Delete the existing image file from Azure Blob Storage.
					await _blobStorageService.DeleteFileAsync(imageName);
				}
				
				// Upload the new image file and get the file ID.
				imageName = _blobStorageService.UploadFileAsync(model.File).Result;

				await dbProduct.UpdateProductImageAsync(imageName);
			}

			// Save the updated product back to SQL Table Storage.
			await _productTableService.UpdateProductAsync(dbProduct);

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
		/// <param name="productId">The ID of the product to delete.</param>
		/// <returns>A redirect to the index action after deleting the product.</returns>
		[HttpPost]
		public async Task<IActionResult> Delete(int productId)
		{
			if (productId != 0)
			{
				// Return a bad request response if the product ID is null or empty.
				return BadRequest("Product ID cannot be null or empty.");
			}

			// Retrieve the product from Azure Table Storage.
			var product = await _productTableService.GetProductByIdAsync(productId);
			if (product == null)
			{
				// Return a not found response if the product does not exist.
				return NotFound();
			}

			// Delete the product from Azure Table Storage.
			await _productTableService.ArchiveProductAsync(product.ProductID);
			await product.DeleteProductImageAsync();

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
			int customerId = _httpContextAccessor?.HttpContext?.Session.GetInt32("CustomerId") ?? 0;

			if (customerId == 0)
			{
				// Return a bad request response if the customer ID is null or empty.
				return BadRequest("Customer ID cannot be null or empty.");
			}

			// Check if the model state is valid
			if (!ModelState.IsValid)
			{
				// If the model state is invalid, return the partial view with the current model to display validation errors.
				return PartialView("_CreateProductForm", model);
			}

			string imageName = string.Empty;

			// Check if a file is provided
			if (model.File == null)
			{
				imageName = _defaultProductImage; // Set a default image file ID
			}
			else
			{
				// Get the URL of the Azure Function to upload the image
				var imageFunctionUrl = _uploadImageFunctionUrl;

				// Create a multipart form content to send the file
				using var imageContent = new MultipartFormDataContent();
				using var fileStream = model.File.OpenReadStream();
				var fileContent = new StreamContent(fileStream);
				imageContent.Add(fileContent, "file", model.File.FileName);

				// Send the file to the Azure Function
				var blobResponse = await _httpClient.PostAsync(imageFunctionUrl, imageContent);

				// Check if the file upload was successful
				if (blobResponse.IsSuccessStatusCode)
				{
					imageName = await blobResponse.Content.ReadAsStringAsync(); // Get the blob name from the function response
				}
				else
				{
					imageName = _defaultProductImage; // Set a default image file ID
				}
			}

			// Create a new product entity from the view model
			var product = new Product(model);
			product.CustomerID = customerId;

			product.ProductID = await _productTableService.InsertProductAsync(product, customerId);

			await _productTableService.InsertProductImageAsync(product.ProductID, imageName);

			// Check if the function invocation was successful
			if (product.ProductID == 0)
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
		/// <param name="productId">The ID of the product to purchase.</param>
		/// <returns>A JSON result indicating the success or failure of the purchase operation.</returns>
		[HttpPost]
		public async Task<IActionResult> Purchase(int productId)
		{
			var customerId = _httpContextAccessor?.HttpContext?.Session.GetInt32("CustomerId") ?? 0;

			// Check if the customer ID is provided
			if (customerId == 0)
			{
				return BadRequest("Customer ID cannot be null or empty.");
			}

			// Check if the product ID is provided
			if (productId == 0)
			{
				return BadRequest("Product ID cannot be null or empty.");
			}

			// Retrieve the product from Azure Table Storage
			var product = await _productTableService.GetProductByIdAsync(productId);
			if (product == null)
			{
				return NotFound();
			}

			// Check if the product is in stock
			if (product.Quantity <= 0)
			{
				return Json(new { success = false, message = "The product is out of stock." });
			}

			// Create an order message
			var orderMessage = new OrderMessage(customerId, productId, 1, product.Price);

			// Get the URL of the Azure Function to send the order message
			var functionUrl = _sendToQueueFunctionUrl;

			// Create the request data for the Azure Function
			var requestData = new
			{
				Message = JsonSerializer.Serialize(orderMessage),
				QueueName = _purchaseQueueName
			};
			var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

			// Send the order message to the Azure Function
			var response = await _httpClient.PostAsync(functionUrl, content);

			// Check if the function invocation was successful
			if (!response.IsSuccessStatusCode)
			{
				return Json(new { success = false, message = "Failed to trigger the order function." });
			}

			return Json(new { success = true, message = "Order successfully placed." });
		}
	}
}