using ABC_Retail.ViewModels;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Hosting;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;

namespace ABC_Retail.Models
{
	/// <summary>
	/// Represents a product entity in the application
	/// </summary>
	public class Product
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Properties of the Product entity
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		public int ProductID { get; set; } // Unique identifier for the product
		public int CustomerID { get; set; } // Unique identifier for the owner of the product
		public string Name { get; set; } // Name of the product
		public decimal Price { get; set; } // Price of the product
		public string Description { get; set; } // Description of the product
		public int Quantity { get; set; } // Quantity of the product
		public bool Availability { get; set; } // Availability of the product
		public bool IsArchived { get; set; } // Archive status of the product

		public readonly string _SQLConnectionString;

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructors
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Constructor for service use
		public Product(string sqlConnectionString)
		{
			_SQLConnectionString = sqlConnectionString;
		}

		// Constructor for ProductViewModel
		public Product(ProductViewModel model)
		{
			Name = model.ProductName;
			Description = model.ProductDescription;
			Price = model.ProductPrice;
			Quantity = model.ProductQuantity;
			Availability = model.ProductAvailability;
		}

		// Default constructor
		public Product() 
		{
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// SQL Methods
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Inserts a new product into the database
		/// </summary>
		/// <param name="m"></param> represents the model of the product
		/// <param name="userID"></param> represents the ID of the user who is inserting the product
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public int InsertProduct(Product m, int customerID, string connectionString)
		{
			using (var con = new SqlConnection(connectionString))
			{
				con.Open();
				using (var transaction = con.BeginTransaction())
				{
					try
					{
						string sql = "INSERT INTO tbl_products (customer_id, name, description, price, quantity, availability) OUTPUT INSERTED.product_id VALUES (@customerID, @ProductName, @ProductDescription, @ProductPrice, @ProductQuantity, @ProductAvailability)";
						SqlCommand cmd = new SqlCommand(sql, con, transaction); // Associate the command with the transaction
						cmd.Parameters.AddWithValue("@customerID", customerID);
						cmd.Parameters.AddWithValue("@ProductName", m.Name);
						cmd.Parameters.AddWithValue("@ProductDescription", m.Description);
						cmd.Parameters.AddWithValue("@ProductPrice", m.Price);
						cmd.Parameters.AddWithValue("@ProductQuantity", m.Quantity);
						m.Availability = m.Quantity > 0;
						cmd.Parameters.AddWithValue("@ProductAvailability", m.Availability);

						// Execute the command and retrieve the new ProductID
						m.ProductID = (int)cmd.ExecuteScalar();

						transaction.Commit(); // Commit the transaction if all commands were successful
						return m.ProductID;
					}
					catch (Exception)
					{
						transaction.Rollback();
						throw;
					}
					finally
					{
						if (con.State == ConnectionState.Open)
							con.Close();
					}
				}
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Fetches all available products from the database
		/// </summary>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public List<ProductViewModel> ListAvailableProducts(string connectionString)
		{
			List<ProductViewModel> productsList = new List<ProductViewModel>();

			using (var con = new SqlConnection(connectionString))
			{
				con.Open();
				string productSql =@"  
				SELECT
					tp.product_id,
					tp.name,
					tp.description,
					tp.price,
					tp.availability,
					tpi.image_name
				FROM
					tbl_products tp
				INNER JOIN
					tbl_product_images tpi ON tp.product_id = tpi.product_id
				WHERE tp.is_archived = 0";

				using (var productCmd = new SqlCommand(productSql, con))
				{
					using (var reader = productCmd.ExecuteReader())
					{
						while (reader.Read())
						{
							productsList.Add(new ProductViewModel
							{
								ProductID = (int)reader["product_id"],
								ProductName = reader["name"].ToString(),
								ProductDescription = reader["description"].ToString(),
								ProductPrice = (decimal)reader["price"],
								ProductAvailability = (bool)reader["availability"],
								ProductImageName = reader["image_name"].ToString()
							});
						}
					}
				}
			}
			return productsList;
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Updates a product in the database
		/// </summary>
		/// <param name="product"></param>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public async Task UpdateProductAsync(Product product, string connectionString)
		{
			using (var con = new SqlConnection(connectionString))
			{
				await con.OpenAsync();

				string sql = @"
				UPDATE tbl_products
				SET
					name = @Name,
					description = @Description,
					price = @Price,
					quantity = @Quantity,
					availability = @Availability
				WHERE product_id = @ProductID";

				using (var cmd = new SqlCommand(sql, con))
				{
					// Add parameters with values from the Product object
					cmd.Parameters.AddWithValue("@ProductId", product.ProductID);
					cmd.Parameters.AddWithValue("@Name", product.Name);
					cmd.Parameters.AddWithValue("@Description", product.Description);
					cmd.Parameters.AddWithValue("@Price", product.Price);
					cmd.Parameters.AddWithValue("@Quantity", product.Quantity);
					cmd.Parameters.AddWithValue("@Availability", product.Availability);

					// Execute the update operation
					await cmd.ExecuteNonQueryAsync();
				}
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Fetches a product by its ID from the database
		/// </summary>
		/// <param name="productId"></param>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public async Task<Product> GetProductByIdAsync(int productId, string connectionString)
		{
			using (var con = new SqlConnection(connectionString))
			{
				await con.OpenAsync();

				string sql = @"
				SELECT product_id, customer_id, name, description, price, quantity, availability
				FROM tbl_products
				WHERE product_id = @ProductId";

				using (var cmd = new SqlCommand(sql, con))
				{
					// Add parameter for product ID
					cmd.Parameters.AddWithValue("@ProductId", productId);

					// Execute the query and read the result
					using (var reader = await cmd.ExecuteReaderAsync())
					{
						if (await reader.ReadAsync())
						{
							// Map the data from the reader to a Product object
							return new Product
							{
								ProductID = reader.GetInt32(reader.GetOrdinal("product_id")),
								CustomerID = reader.GetInt32(reader.GetOrdinal("customer_id")),
								Name = reader.GetString(reader.GetOrdinal("name")),
								Description = reader.GetString(reader.GetOrdinal("description")),
								Price = reader.GetDecimal(reader.GetOrdinal("price")),
								Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
								Availability = reader.GetBoolean(reader.GetOrdinal("availability"))
							};
						}
						else
						{
							// Return null if no product was found with the given ID
							return null;
						}
					}
				}
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Archives a product by setting its is_archived column to 1
		/// </summary>
		/// <param name="productId"></param>
		/// <param name="connectionString"></param>
		/// <returns></returns>
		public async Task ArchiveProductAsync(int productId, string connectionString)
		{
			using (var con = new SqlConnection(connectionString))
			{
				await con.OpenAsync();

				string sql = "UPDATE tbl_products SET is_archived = 1 WHERE product_id = @ProductId";

				using (var cmd = new SqlCommand(sql, con))
				{
					cmd.Parameters.AddWithValue("@ProductId", productId);

					await cmd.ExecuteNonQueryAsync();
				}
			}
		}


	}
}