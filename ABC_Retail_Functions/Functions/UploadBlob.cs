//using Azure.Storage.Blobs;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using System.IO;
//using System.Threading.Tasks;
//using ABC_Retail.Services;
//using Microsoft.Azure.Functions.Worker.Http;
//using System.Net;

//namespace ABC_Retail_Functions.Functions
//{
//	public class UploadBlob
//	{
//		private readonly AzureBlobStorageService _blobService;
//		private readonly ILogger<UploadBlob> _logger;

//		public UploadBlob(AzureBlobStorageService blobService, ILogger<UploadBlob> logger)
//		{
//			_blobService = blobService;
//			_logger = logger;
//		}

//		[Function("UploadBlob")]
//		public async Task<HttpResponseData> Run(
//			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
//			FunctionContext executionContext)
//		{
//			var log = executionContext.GetLogger("UploadBlob");
//			log.LogInformation("Processing request to upload blob.");

//			// Your existing code here...

//			var response = req.CreateResponse(HttpStatusCode.OK);
//			await response.WriteStringAsync("Blob uploaded");
//			return response;
//		}
//	}
//}
