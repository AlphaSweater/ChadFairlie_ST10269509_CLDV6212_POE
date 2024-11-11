using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;
using System.Data.SqlClient;
using System.Data;
using ABC_Retail.ViewModels;

namespace ABC_Retail.Models
{
	/// <summary>
	/// Represents a customer entity in the application
	/// </summary>
	public class Customer
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Properties of the Customer entity
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		public int CustomerId { get; set; } // Unique identifier for the customer
		public string Name { get; set; } // Name of the customer
		public string Surname { get; set; } // Surname of the customer
		public string Phone { get; set; } // Phone number of the customer
		public string Email { get; set; } // Email of the customer
		public string Password { get; set; } // Password of the customer

		public readonly string _SQLConnectionString;

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructors
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Constructor for service use
		public Customer(string sqlConnectionString) 
		{
			_SQLConnectionString = sqlConnectionString;
		}

		// Constructor for LoginViewModel
		public Customer(LoginViewModel model) 
		{
			Email = model.Email;
			Password = model.Password;
		}

		// Constructor for SignUpViewModel
		public Customer(SignUpViewModel model)
		{
			Name = model.Name;
			Surname = model.Surname;
			Phone = model.Phone;
			Email = model.Email;
			Password = model.Password;
		}

		// Default constructor
		public Customer()
		{
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// SQL Methods
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Inserts a new customer into the database
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public async Task<int> InsertUserAsync(Customer m)
		{
			using (var con = new SqlConnection(_SQLConnectionString))
			{
				await con.OpenAsync();  // Open connection asynchronously
				using (var transaction = (SqlTransaction)await con.BeginTransactionAsync())  // Begin transaction asynchronously
				{
					try
					{
						// Hash the password
						var passwordHasher = new PasswordHasher<IdentityUser>();
						var passwordHash = passwordHasher.HashPassword(user: null, password: m.Password);

						string sql = "INSERT INTO tbl_customers (name, surname, phone, email, password_hash) OUTPUT INSERTED.customer_id VALUES (@CustomerName, @CustomerSurname, @CustomerPhone, @CustomerEmail, @PasswordHash)";
						using (SqlCommand cmd = new SqlCommand(sql, con, transaction))
						{
							cmd.Parameters.AddWithValue("@CustomerName", m.Name);
							cmd.Parameters.AddWithValue("@CustomerSurname", m.Surname);
							cmd.Parameters.AddWithValue("@CustomerPhone", m.Phone);
							cmd.Parameters.AddWithValue("@CustomerEmail", m.Email);
							cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

							var result = await cmd.ExecuteScalarAsync() ?? 0;  // Execute the query asynchronously
							await transaction.CommitAsync();  // Commit the transaction asynchronously
							return (int)result;
						}
					}
					catch (Exception)
					{
						await transaction.RollbackAsync();  // Rollback transaction asynchronously
						throw;
					}
					finally
					{
						if (con.State == ConnectionState.Open)
							await con.CloseAsync();  // Close connection asynchronously
					}
				}
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		public async Task UpdateUserAsync(Customer m)
		{
			using (var con = new SqlConnection(_SQLConnectionString))
			{
				await con.OpenAsync();  // Open connection asynchronously
				using (var transaction = (SqlTransaction)await con.BeginTransactionAsync())  // Begin transaction asynchronously
				{
					try
					{
						string sql = "UPDATE tbl_customers SET name = @CustomerName, surname = @CustomerSurname, phone = @CustomerPhone, email = @CustomerEmail WHERE customer_id = @CustomerId";
						using (SqlCommand cmd = new SqlCommand(sql, con, transaction))
						{
							cmd.Parameters.AddWithValue("@CustomerName", m.Name);
							cmd.Parameters.AddWithValue("@CustomerSurname", m.Surname);
							cmd.Parameters.AddWithValue("@CustomerPhone", m.Phone);
							cmd.Parameters.AddWithValue("@CustomerEmail", m.Email);
							cmd.Parameters.AddWithValue("@CustomerId", m.CustomerId);
							await cmd.ExecuteNonQueryAsync();  // Execute the query asynchronously
							await transaction.CommitAsync();  // Commit the transaction asynchronously
						}
					}
					catch (Exception)
					{
						await transaction.RollbackAsync();  // Rollback transaction asynchronously
						throw;
					}
					finally
					{
						if (con.State == ConnectionState.Open)
							await con.CloseAsync();  // Close connection asynchronously
					}
				}
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Validates a user's credentials
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public async Task<int> ValidateUserAsync(Customer m)
		{
			using (var con = new SqlConnection(_SQLConnectionString))
			{
				await con.OpenAsync();
				string sql = "SELECT customer_id, password_hash FROM tbl_customers WHERE email = @CustomerEmail";
				using (SqlCommand cmd = new SqlCommand(sql, con))
				{
					cmd.Parameters.AddWithValue("@CustomerEmail", m.Email);
					using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
					{
						if (await reader.ReadAsync())
						{
							var passwordHasher = new PasswordHasher<IdentityUser>();
							var hashedPassword = reader["password_hash"] as string;
							if (hashedPassword == null)
							{
								return 0;
							}
							var result = passwordHasher.VerifyHashedPassword(user: null, hashedPassword: hashedPassword, providedPassword: m.Password);
							if (result == PasswordVerificationResult.Success)
							{
								return reader["customer_id"] != DBNull.Value ? int.Parse(reader["customer_id"].ToString()) : 0;
							}
						}
						return 0;
					}
				}
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Fetches a user by their ID from the database
		/// </summary>
		/// <param name="customerId"></param>
		/// <returns></returns>
		public async Task<Customer> GetUserByIdAsync(int customerId)
		{
			using (var con = new SqlConnection(_SQLConnectionString))
			{
				await con.OpenAsync();  // Open connection asynchronously

				string sql = "SELECT customer_id, name, surname, phone, email FROM tbl_customers WHERE customer_id = @CustomerId";

				using (SqlCommand cmd = new SqlCommand(sql, con))
				{
					cmd.Parameters.AddWithValue("@CustomerId", customerId);

					using (var reader = await cmd.ExecuteReaderAsync())  // Execute the query asynchronously
					{
						if (await reader.ReadAsync())  // Check if a record was returned
						{
							return new Customer
							{
								CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
								Name = reader.GetString(reader.GetOrdinal("name")),
								Surname = reader.GetString(reader.GetOrdinal("surname")),
								Phone = reader.GetString(reader.GetOrdinal("phone")),
								Email = reader.GetString(reader.GetOrdinal("email"))
							};
						}
						return null;  // Return null if no user was found
					}
				}
			}
		}

	}
}