using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace ABC_Retail.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class APIController : ControllerBase
	{
		private readonly HttpClient _httpClient;

		public APIController(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		[HttpPost("send-message")]
		public async Task<IActionResult> SendMessage([FromBody] RequestData requestData)
		{
			var jsonContent = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync("https://<your-function-app>.azurewebsites.net/api/SendQueueMessage", jsonContent);

			if (response.IsSuccessStatusCode)
			{
				return Ok("Message sent successfully!");
			}
			else
			{
				return StatusCode((int)response.StatusCode, "Failed to send message.");
			}
		}

		public class RequestData
		{
			public string? Message { get; set; }
			public string? QueueName { get; set; }
		}
	}
}
