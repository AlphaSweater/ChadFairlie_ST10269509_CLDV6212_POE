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

		public Order(string sqlConnectionString)
		{
			_SQLConnectionString = sqlConnectionString;
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
		public async Task<bool> RecordOrderAsync(int customerId, int productId, int quantity, decimal price)
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
							cmd.Parameters.AddWithValue("@CustomerId", customerId);
							cmd.Parameters.AddWithValue("@ProductId", productId);
							cmd.Parameters.AddWithValue("@Quantity", quantity);
							cmd.Parameters.AddWithValue("@TotalAmount", quantity * price);
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
		public async Task<List<Order>> GetOrdersByCustomerIdAsync(int customerId)
		{
			var orders = new List<Order>();

			using (SqlConnection con = new SqlConnection(_SQLConnectionString))
			{
				await con.OpenAsync();
				string sql = @"
				SELECT order_id, customer_id, product_id, total_quantity, total_amount, order_date
				FROM tbl_orders
				WHERE customer_id = @CustomerId";

				using (SqlCommand cmd = new SqlCommand(sql, con))
				{
					cmd.Parameters.AddWithValue("@CustomerId", customerId);

					using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							var order = new Order
							{
								OrderId = reader.GetInt32(reader.GetOrdinal("order_id")),
								CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
								ProductId = reader.GetInt32(reader.GetOrdinal("product_id")),
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