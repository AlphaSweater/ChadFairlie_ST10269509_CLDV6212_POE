using ABC_Retail.ViewModels;
using Azure;
using Azure.Data.Tables;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Text.Json;
using static Azure.Core.HttpHeader;


namespace ABC_Retail.Models
{
	public class Order
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Properties of the Order entity
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Unique identifier for the order
		public int OrderId { get; set; } 

		// The ID of the related Customer
		public int CustomerId { get; set; }

		// The ID of the related Product
		public int ProductId { get; set; }

		// Total quantity of the product in the order
		public int TotalQuantity { get; set; }

		// Total amount for the order
		public decimal TotalAmount { get; set; }

		// Date the order was placed
		public DateTime OrderDate { get; set; }


		private readonly string _SQLConnectionString;

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructors
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Constructor for service use
		public Order(string sqlConnectionString)
		{
			_SQLConnectionString = sqlConnectionString;
		}

		// Constructor for OrderViewModel
		public Order(OrderViewModel model)
		{
			OrderId = model.OrderId;
			CustomerId = model.CustomerId;
			ProductId = model.ProductId;
			TotalQuantity = model.TotalQuantity;
			TotalAmount = model.TotalAmount;
			OrderDate = model.OrderDate;
		}

		// Default constructor
		public Order()
		{
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// SQL Methods
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Records an order in the database.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="productId"></param>
		/// <param name="quantity"></param>
		/// <returns></returns>
		public async Task<bool> RecordOrderAsync(OrderMessage order)
		{
			using (SqlConnection con = new SqlConnection(_SQLConnectionString))
			{
				await con.OpenAsync();
				using (SqlTransaction transaction = con.BeginTransaction())
				{
					try
					{
						string sql = @"
						INSERT INTO tbl_orders (customer_id, product_id, total_quantity, total_amount, order_date)
						VALUES (@CustomerId, @ProductId, @Quantity, @TotalAmount, @OrderDate)";

						using (SqlCommand cmd = new SqlCommand(sql, con, transaction))
						{
							cmd.Parameters.AddWithValue("@CustomerId", order.CustomerId);
							cmd.Parameters.AddWithValue("@ProductId", order.ProductId);
							cmd.Parameters.AddWithValue("@Quantity", order.Quantity);
							cmd.Parameters.AddWithValue("@TotalAmount", order.Quantity * order.Price);
							cmd.Parameters.AddWithValue("@OrderDate", DateTime.Now);

							int rowsAffected = await cmd.ExecuteNonQueryAsync();
							if (rowsAffected > 0)
							{
								transaction.Commit();
								return true;
							}
							else
							{
								transaction.Rollback();
								return false;
							}
						}
					}
					catch (Exception)
					{
						transaction.Rollback();
						throw;
					}
				}
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Retrieves all of a customer's orders from the database by their ID.
		/// </summary>
		/// <param name="customerId"></param>
		/// <returns></returns>
		public async Task<List<OrderViewModel>> GetOrdersByCustomerIdAsync(int customerId)
		{
			var orders = new List<OrderViewModel>();

			using (SqlConnection con = new SqlConnection(_SQLConnectionString))
			{
				await con.OpenAsync();
				string sql = @"
				SELECT o.order_id, o.customer_id, c.name + ' ' + c.surname AS customer_full_name, 
				       o.product_id, p.name AS product_name, pi.image_name AS product_image_name, 
				       o.total_quantity, o.total_amount, o.order_date
				FROM tbl_orders o
				JOIN tbl_customers c ON o.customer_id = c.customer_id
				JOIN tbl_products p ON o.product_id = p.product_id
				LEFT JOIN tbl_product_images pi ON p.product_id = pi.product_id
				WHERE o.customer_id = @CustomerId";

				using (SqlCommand cmd = new SqlCommand(sql, con))
				{
					cmd.Parameters.AddWithValue("@CustomerId", customerId);

					using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							var order = new OrderViewModel
							{
								OrderId = reader.GetInt32(reader.GetOrdinal("order_id")),
								CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
								CustomerFullName = reader.GetString(reader.GetOrdinal("customer_full_name")),
								ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
								ProductName = reader.GetString(reader.GetOrdinal("product_name")),
								ProductImageName = reader.IsDBNull(reader.GetOrdinal("product_image_name")) ? null : reader.GetString(reader.GetOrdinal("product_image_name")),
								TotalQuantity = reader.GetInt32(reader.GetOrdinal("total_quantity")),
								TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
								OrderDate = reader.GetDateTime(reader.GetOrdinal("order_date"))
							};
							orders.Add(order);
						}
					}
				}
			}

			return orders;
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Retrieves all orders from the database.
		/// </summary>
		/// <returns></returns>
		public async Task<List<OrderViewModel>> GetAllOrdersAsync()
		{
			var orders = new List<OrderViewModel>();
			using (SqlConnection con = new SqlConnection(_SQLConnectionString))
			{
				await con.OpenAsync();
				string sql = @"
				SELECT o.order_id, o.customer_id, c.name + ' ' + c.surname AS customer_full_name, 
				       o.product_id, p.name AS product_name, pi.image_name AS product_image_name, 
				       o.total_quantity, o.total_amount, o.order_date
				FROM tbl_orders o
				JOIN tbl_customers c ON o.customer_id = c.customer_id
				JOIN tbl_products p ON o.product_id = p.product_id
				LEFT JOIN tbl_product_images pi ON p.product_id = pi.product_id";

				using (SqlCommand cmd = new SqlCommand(sql, con))
				{
					using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							var order = new OrderViewModel
							{
								OrderId = reader.GetInt32(reader.GetOrdinal("order_id")),
								CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
								CustomerFullName = reader.GetString(reader.GetOrdinal("customer_full_name")),
								ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
								ProductName = reader.GetString(reader.GetOrdinal("product_name")),
								ProductImageName = reader.IsDBNull(reader.GetOrdinal("product_image_name")) ? null : reader.GetString(reader.GetOrdinal("product_image_name")),
								TotalQuantity = reader.GetInt32(reader.GetOrdinal("total_quantity")),
								TotalAmount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
								OrderDate = reader.GetDateTime(reader.GetOrdinal("order_date"))
							};
							orders.Add(order);
						}
					}
				}
			}
			return orders;
		}
	}
}
