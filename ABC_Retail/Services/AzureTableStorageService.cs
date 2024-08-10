using ABC_Retail.Models;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Services
{
	public class AzureTableStorageService
	{
		private readonly TableServiceClient _tableServiceClient;
		private readonly string _customersTableName = "Customers";
		private readonly string _productsTableName = "Products";

		public AzureTableStorageService(string storageConnectionString)
		{
			_tableServiceClient = new TableServiceClient(storageConnectionString);
		}

		// Method to ensure the table exists
		private async Task<TableClient> GetOrCreateTableClientAsync(string tableName)
		{
			var tableClient = _tableServiceClient.GetTableClient(tableName);
			await tableClient.CreateIfNotExistsAsync();
			return tableClient;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Methods to interact with Customer Table
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to add a customer profile to the CustomerProfiles table.
		public async Task AddCustomerProfileAsync(Customer customer)
		{
			var tableClient = await GetOrCreateTableClientAsync(_customersTableName);
			try
			{
				await tableClient.AddEntityAsync(customer);
			}
			catch (RequestFailedException ex)
			{
				// Handle exceptions like entity already exists or other table errors
				Console.WriteLine($"Error adding customer profile: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to get a customer profile from the CustomerProfiles table.
		public async Task<Customer> GetCustomerProfileAsync(string partitionKey, string rowKey)
		{
			var tableClient = await GetOrCreateTableClientAsync(_customersTableName);
			try
			{
				return await tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
			}
			catch (RequestFailedException ex) when (ex.Status == 404)
			{
				// Entity not found
				Console.WriteLine($"Customer profile not found: {ex.Message}");
				return null;
			}
			catch (RequestFailedException ex)
			{
				// Handle other potential errors
				Console.WriteLine($"Error retrieving customer profile: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to update a customer profile in the CustomerProfiles table.
		public async Task UpdateCustomerProfileAsync(Customer customer)
		{
			var tableClient = await GetOrCreateTableClientAsync(_customersTableName);
			try
			{
				await tableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
			}
			catch (RequestFailedException ex)
			{
				// Handle exceptions like entity not existing
				Console.WriteLine($"Error updating customer profile: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to delete a customer profile from the CustomerProfiles table.
		public async Task DeleteCustomerProfileAsync(string partitionKey, string rowKey)
		{
			var tableClient = await GetOrCreateTableClientAsync(_customersTableName);
			try
			{
				await tableClient.DeleteEntityAsync(partitionKey, rowKey);
			}
			catch (RequestFailedException ex)
			{
				// Handle exceptions like entity not existing
				Console.WriteLine($"Error deleting customer profile: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to get all customer profiles from the CustomerProfiles table.
		public async Task<List<Customer>> GetAllCustomerProfilesAsync()
		{
			var tableClient = await GetOrCreateTableClientAsync(_customersTableName);
			var customers = new List<Customer>();

			await foreach (var customer in tableClient.QueryAsync<Customer>())
			{
				customers.Add(customer);
			}

			return customers;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Methods to interact with Product Table
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to add a product to the Products table.
		public async Task AddProductAsync(Product product)
		{
			var tableClient = await GetOrCreateTableClientAsync(_productsTableName);
			try
			{
				await tableClient.AddEntityAsync(product);
			}
			catch (RequestFailedException ex)
			{
				// Handle exceptions like entity already exists or other table errors
				Console.WriteLine($"Error adding product: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to get a product from the Products table.
		public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
		{
			var tableClient = await GetOrCreateTableClientAsync(_productsTableName);
			try
			{
				return await tableClient.GetEntityAsync<Product>(partitionKey, rowKey);
			}
			catch (RequestFailedException ex) when (ex.Status == 404)
			{
				// Entity not found
				Console.WriteLine($"Product not found: {ex.Message}");
				return null;
			}
			catch (RequestFailedException ex)
			{
				// Handle other potential errors
				Console.WriteLine($"Error retrieving product: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to update a product in the Products table.
		public async Task UpdateProductAsync(Product product)
		{
			var tableClient = await GetOrCreateTableClientAsync(_productsTableName);
			try
			{
				await tableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
			}
			catch (RequestFailedException ex)
			{
				// Handle exceptions like entity not existing
				Console.WriteLine($"Error updating product: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to delete a product from the Products table.
		public async Task DeleteProductAsync(string partitionKey, string rowKey)
		{
			var tableClient = await GetOrCreateTableClientAsync(_productsTableName);
			try
			{
				await tableClient.DeleteEntityAsync(partitionKey, rowKey);
			}
			catch (RequestFailedException ex)
			{
				// Handle exceptions like entity not existing
				Console.WriteLine($"Error deleting product: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Async method to get all products from the Products table.
		public async Task<List<Product>> GetAllProductsAsync()
		{
			var tableClient = await GetOrCreateTableClientAsync(_productsTableName);
			var products = new List<Product>();

			await foreach (var product in tableClient.QueryAsync<Product>())
			{
				products.Add(product);
			}

			return products;
		}
	}
}