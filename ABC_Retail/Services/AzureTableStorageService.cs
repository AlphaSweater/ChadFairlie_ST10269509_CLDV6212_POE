using ABC_Retail.Models;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Services
{
	/// <summary>
	/// Provides services for interacting with Azure Table Storage, including operations
	/// for managing customer profiles and products stored in their respective tables.
	/// </summary>
	public class AzureTableStorageService
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Fields and Dependencies
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// The TableServiceClient instance for interacting with Azure Table Storage.
		private readonly TableServiceClient _tableServiceClient;

		// The name of the Azure Table used to store customer profiles.
		private readonly string _customersTableName = "Customers";

		// The name of the Azure Table used to store products.
		private readonly string _productsTableName = "Products";

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		/// <summary>
		/// Initializes a new instance of the <see cref="AzureTableStorageService"/> class.
		/// </summary>
		public AzureTableStorageService(string storageConnectionString)
		{
			_tableServiceClient = new TableServiceClient(storageConnectionString);
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Ensures that a table exists in Azure Table Storage. If the table does not exist, it is created.
		/// </summary>
		/// <param name="tableName">The name of the table to check or create.</param>
		/// <returns>A <see cref="TableClient"/> instance for the specified table.</returns>
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
		/// <summary>
		/// Asynchronously adds a new customer profile to the Customers table in Azure Table Storage.
		/// </summary>
		/// <param name="customer">The <see cref="Customer"/> entity to add.</param>
		public async Task AddCustomerAsync(Customer customer)
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
		/// <summary>
		/// Asynchronously retrieves a customer profile from the Customers table by partition key and row key.
		/// </summary>
		/// <param name="partitionKey">The partition key of the customer entity.</param>
		/// <param name="rowKey">The row key of the customer entity.</param>
		/// <returns>The retrieved <see cref="Customer"/> entity, or null if not found.</returns>
		public async Task<Customer> GetCustomerAsync(string partitionKey, string rowKey)
		{
			var tableClient = await GetOrCreateTableClientAsync(_customersTableName);
			try
			{
				return await tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
			}
			catch (RequestFailedException ex) when (ex.Status == 404)
			{
				// Handle case where the entity was not found in the table.
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
		/// <summary>
		/// Asynchronously updates an existing customer profile in the Customers table.
		/// </summary>
		/// <param name="customer">The <see cref="Customer"/> entity to update.</param>
		public async Task UpdateCustomerAsync(Customer customer)
		{
			var tableClient = await GetOrCreateTableClientAsync(_customersTableName);
			try
			{
				await tableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
			}
			catch (RequestFailedException ex)
			{
				// Handle potential errors such as entity not existing.
				Console.WriteLine($"Error updating customer profile: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Asynchronously deletes a customer profile from the Customers table.
		/// </summary>
		/// <param name="partitionKey">The partition key of the customer entity.</param>
		/// <param name="rowKey">The row key of the customer entity.</param>
		public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
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
		/// <summary>
		/// Asynchronously retrieves all customer profiles from the Customers table.
		/// </summary>
		/// <returns>A list of all <see cref="Customer"/> entities in the Customers table.</returns>
		public async Task<List<Customer>> GetAllCustomersAsync()
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
		/// <summary>
		/// Asynchronously adds a new product to the Products table in Azure Table Storage.
		/// </summary>
		/// <param name="product">The <see cref="Product"/> entity to add.</param>
		public async Task AddProductAsync(Product product)
		{
			var tableClient = await GetOrCreateTableClientAsync(_productsTableName);
			try
			{
				await tableClient.AddEntityAsync(product);
			}
			catch (RequestFailedException ex)
			{
				// Handle potential errors such as entity already existing or other table-related issues.
				Console.WriteLine($"Error adding product: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Asynchronously retrieves a product from the Products table by partition key and row key.
		/// </summary>
		/// <param name="partitionKey">The partition key of the product entity.</param>
		/// <param name="rowKey">The row key of the product entity.</param>
		/// <returns>The retrieved <see cref="Product"/> entity, or null if not found.</returns>
		public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
		{
			var tableClient = await GetOrCreateTableClientAsync(_productsTableName);
			try
			{
				return await tableClient.GetEntityAsync<Product>(partitionKey, rowKey);
			}
			catch (RequestFailedException ex) when (ex.Status == 404)
			{
				// Handle case where the entity was not found in the table.
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
		/// <summary>
		/// Asynchronously updates an existing product in the Products table.
		/// </summary>
		/// <param name="product">The <see cref="Product"/> entity to update.</param>
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
		/// <summary>
		/// Asynchronously deletes a product from the Products table.
		/// </summary>
		/// <param name="partitionKey">The partition key of the product entity.</param>
		/// <param name="rowKey">The row key of the product entity.</param>
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
		/// <summary>
		/// Asynchronously retrieves all products from the Products table.
		/// </summary>
		/// <returns>A list of all <see cref="Product"/> entities in the Products table.</returns>
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