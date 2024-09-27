using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using ABC_Retail.Services;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace ABC_Retail_Functions.Functions
{
	public class UploadFile
	{
		private readonly AzureFileStorageService _fileService;
		private readonly ILogger<UploadFile> _logger;

		public UploadFile(AzureFileStorageService fileService, ILogger<UploadFile> logger)
		{
			_fileService = fileService;
			_logger = logger;
		}

		[Function("UploadFile")]
		public async Task<HttpResponseData> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
			FunctionContext executionContext)
		{
			var log = executionContext.GetLogger("UploadFile");
			log.LogInformation("Processing request to upload file.");

			// Your existing code here...


			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("File uploaded to Azure Files");
			return response;
		}
	}
}
