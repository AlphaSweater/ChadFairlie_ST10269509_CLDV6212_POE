//using ABC_Retail.Services;
//using Microsoft.AspNetCore.Mvc;

//namespace ABC_Retail.Controllers
//{
//	public class OrderController : Controller
//	{
//		private readonly AzureQueueService _queueService;

//		public OrderController(AzureQueueService queueService)
//		{
//			_queueService = queueService;
//		}

//		[HttpPost]
//		public async Task<IActionResult> PlaceOrder(Order order)
//		{
//			// Basic validation (you could expand this)
//			if (order == null || !ModelState.IsValid)
//			{
//				return BadRequest("Invalid order data.");
//			}

//			// Enqueue the order for processing
//			await _queueService.EnqueueMessageAsync(order);

//			return RedirectToAction("OrderConfirmation");
//		}
//	}
//}