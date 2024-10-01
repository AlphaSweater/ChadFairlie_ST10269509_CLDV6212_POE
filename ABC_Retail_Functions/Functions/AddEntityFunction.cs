using System;
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
		// Service for interacting with product Azure Table Storage.
		private readonly ProductTableService _productTableService;

		// Service for interacting with customer Azure Table Storage.
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

			// Read the request body
			string requestBody;
			try
			{
				requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				log.LogInformation("Request body read successfully.");
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error reading request body.");
				return new BadRequestObjectResult("Error reading request body.");
			}

			log.LogInformation("Beginning to deserialize message");

			// Deserialize the request body into a JObject to check the type of entity
			JObject requestData;
			try
			{
				requestData = JsonConvert.DeserializeObject<JObject>(requestBody);
				log.LogInformation("Request body deserialized successfully.");
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error deserializing request body.");
				return new BadRequestObjectResult("Error deserializing request body.");
			}

			// Ensure the entity type is specified and valid
			string entityType = requestData?["EntityType"]?.ToString();
			if (string.IsNullOrEmpty(entityType))
			{
				log.LogWarning("Entity type is missing or invalid.");
				return new BadRequestObjectResult("Please specify a valid entity type.");
			}

			log.LogInformation($"Entity identified as {entityType}.");

			// Depending on the entity type, process the entity
			try
			{
				switch (entityType.ToLower())
				{
					case "customer":
						log.LogInformation("Deserializing to Customer entity.");
						Customer customer;
						try
						{
							customer = JsonSerializer.Deserialize<Customer>(requestBody);
							customer.EntityType = null;
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error deserializing to Customer entity.");
							return new BadRequestObjectResult("Invalid customer data.");
						}

						if (customer == null)
						{
							log.LogWarning("Invalid customer data.");
							return new BadRequestObjectResult("Invalid customer data.");
						}

						log.LogInformation("Adding Customer entity to table storage.");
						await _customerTableService.AddEntityAsync(customer);
						log.LogInformation("Customer entity added successfully.");
						break;

					case "product":
						log.LogInformation("Deserializing to Product entity.");
						Product product;
						try
						{
							product = JsonSerializer.Deserialize<Product>(requestBody);
							product.EntityType = null;
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error deserializing to Product entity.");
							return new BadRequestObjectResult("Invalid product data.");
						}

						if (product == null)
						{
							log.LogWarning("Invalid product data.");
							return new BadRequestObjectResult("Invalid product data.");
						}

						log.LogInformation("Adding Product entity to table storage.");
						await _productTableService.AddEntityAsync(product);
						log.LogInformation("Product entity added successfully.");
						break;

					default:
						log.LogWarning($"Unsupported entity type: {entityType}");
						return new BadRequestObjectResult("Unsupported entity type.");
				}
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error adding entity.");
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}

			return new OkObjectResult($"Entity of type {entityType} added successfully.");
		}
	}
}
