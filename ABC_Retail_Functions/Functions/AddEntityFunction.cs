using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ABC_Retail.Services;
using ABC_Retail.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ABC_Retail_Functions.Functions
{
	public class AddEntityFunction
	{
		private readonly ProductTableService _productTableService;
		private readonly CustomerTableService _customerTableService;

		public AddEntityFunction(ProductTableService productTableService, CustomerTableService customerTableService)
		{
			_productTableService = productTableService;
			_customerTableService = customerTableService;
		}

		[FunctionName("AddEntityFunction")]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("Processing request to add entity.");

			string requestBody;
			JObject requestData;
			try
			{
				using var reader = new StreamReader(req.Body);
				requestBody = await reader.ReadToEndAsync();
				requestData = JsonConvert.DeserializeObject<JObject>(requestBody);
			}
			catch
			{
				log.LogWarning("Error reading or deserializing request body.");
				return new BadRequestObjectResult("Error reading or deserializing request body.");
			}

			string entityType = requestData?["EntityType"]?.ToString();
			if (string.IsNullOrEmpty(entityType))
			{
				log.LogWarning("Entity type is missing or invalid.");
				return new BadRequestObjectResult("Please specify a valid entity type.");
			}

			log.LogInformation($"Entity identified as {entityType}.");

			switch (entityType.ToLower())
			{
				case "customer":
					return await ProcessCustomerAsync(requestBody, log);
				case "product":
					return await ProcessProductAsync(requestBody, log);
				default:
					log.LogWarning($"Unsupported entity type: {entityType}");
					return new BadRequestObjectResult("Unsupported entity type.");
			}
		}

		private async Task<IActionResult> ProcessCustomerAsync(string requestBody, ILogger log)
		{
			log.LogInformation("Deserializing to Customer entity.");
			Customer customer;
			try
			{
				customer = JsonSerializer.Deserialize<Customer>(requestBody);
				customer.EntityType = null;
			}
			catch
			{
				log.LogWarning("Invalid customer data.");
				return new BadRequestObjectResult("Invalid customer data.");
			}

			log.LogInformation("Adding Customer entity to table storage.");
			await _customerTableService.AddEntityAsync(customer);
			log.LogInformation("Customer entity added successfully.");
			return new OkObjectResult("Customer entity added successfully.");
		}

		private async Task<IActionResult> ProcessProductAsync(string requestBody, ILogger log)
		{
			log.LogInformation("Deserializing to Product entity.");
			Product product;
			try
			{
				product = JsonSerializer.Deserialize<Product>(requestBody);
				product.EntityType = null;
			}
			catch
			{
				log.LogWarning("Invalid product data.");
				return new BadRequestObjectResult("Invalid product data.");
			}

			log.LogInformation("Adding Product entity to table storage.");
			await _productTableService.AddEntityAsync(product);
			log.LogInformation("Product entity added successfully.");
			return new OkObjectResult("Product entity added successfully.");
		}
	}
}