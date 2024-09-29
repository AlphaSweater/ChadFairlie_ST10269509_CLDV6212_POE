using ABC_Retail_Shared.Models;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retail_Shared.Services
{
	/// <summary>
	/// Provides generic services for interacting with Azure Table Storage, including operations
	/// for managing entities stored in their respective tables.
	/// </summary>
	public class AzureTableStorageService<T> where T : class, ITableEntity, new()
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Fields and Dependencies
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// The TableServiceClient instance for interacting with Azure Table Storage.
		private readonly TableServiceClient _tableServiceClient;

		// The name of the Azure Table used.
		private readonly string _tableName;

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		/// <summary>
		/// Initializes a new instance of the <see cref="AzureTableStorageService{T}"/> class.
		/// </summary>
		public AzureTableStorageService(string storageConnectionString, string tableName)
		{
			_tableServiceClient = new TableServiceClient(storageConnectionString);
			_tableName = tableName;
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Ensures that a table exists in Azure Table Storage. If the table does not exist, it is created.
		/// </summary>
		/// <param name="tableName">The name of the table to check or create.</param>
		/// <returns>A <see cref="TableClient"/> instance for the specified table.</returns>
		private async Task<TableClient> GetOrCreateTableClientAsync()
		{
			var tableClient = _tableServiceClient.GetTableClient(_tableName);
			await tableClient.CreateIfNotExistsAsync();
			return tableClient;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Methods to interact with a Table
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Asynchronously adds a new entity to a table.
		/// </summary>
		/// <param name="customer">The <see cref="Customer"/> entity to add.</param>
		public async Task AddEntityAsync(T entity)
		{
			var tableClient = await GetOrCreateTableClientAsync();
			try
			{
				await tableClient.AddEntityAsync(entity);
			}
			catch (RequestFailedException ex)
			{
				Console.WriteLine($"Error adding entity: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Asynchronously retrieves an entity from a table by partition key and row key.
		/// </summary>
		/// <param name="partitionKey">The partition key of the entity.</param>
		/// <param name="rowKey">The row key of the entity.</param>
		/// <returns>The retrieved <see cref="Customer"/> entity, or null if not found.</returns>
		public async Task<T> GetEntityAsync(string partitionKey, string rowKey)
		{
			var tableClient = await GetOrCreateTableClientAsync();
			try
			{
				return await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
			}
			catch (RequestFailedException ex) when (ex.Status == 404)
			{
				Console.WriteLine($"Entity not found: {ex.Message}");
				return null;
			}
			catch (RequestFailedException ex)
			{
				Console.WriteLine($"Error retrieving entity: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Asynchronously updates an existing entity in a table.
		/// </summary>
		/// <param name="entity">The entity to update.</param>
		public async Task UpdateEntityAsync(T entity)
		{
			var tableClient = await GetOrCreateTableClientAsync();
			try
			{
				await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
			}
			catch (RequestFailedException ex)
			{
				Console.WriteLine($"Error updating entity: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Asynchronously deletes an existing entity from a Table
		/// </summary>
		/// <param name="partitionKey">The partition key of the entity.</param>
		/// <param name="rowKey">The row key of the entity.</param>
		public async Task DeleteEntityAsync(string partitionKey, string rowKey)
		{
			var tableClient = await GetOrCreateTableClientAsync();
			try
			{
				await tableClient.DeleteEntityAsync(partitionKey, rowKey);
			}
			catch (RequestFailedException ex)
			{
				Console.WriteLine($"Error deleting entity: {ex.Message}");
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Asynchronously retrieves all entities from a table.
		/// </summary>
		/// <returns>A list of all entities in a table.</returns>
		public async Task<List<T>> GetAllEntitiesAsync()
		{
			var tableClient = await GetOrCreateTableClientAsync();
			var entities = new List<T>();

			await foreach (var entity in tableClient.QueryAsync<T>())
			{
				entities.Add(entity);
			}

			return entities;
		}
	}

	//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
	// Non-generic Table Services
	//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

	//--------------------------------------------------------------------------------------------------------------------------//
	/// <summary>
	/// Custom Table Service for a Customer entity.
	/// </summary>
	public class CustomerTableService : AzureTableStorageService<Customer>
	{
		public CustomerTableService(string storageConnectionString)
			: base(storageConnectionString, "Customers")
		{
		}
	}

	//--------------------------------------------------------------------------------------------------------------------------//
	/// <summary>
	/// Product Table Service for a Product entity.
	/// </summary>
	public class ProductTableService : AzureTableStorageService<Product>
	{
		public ProductTableService(string storageConnectionString)
			: base(storageConnectionString, "Products")
		{
		}
	}
}