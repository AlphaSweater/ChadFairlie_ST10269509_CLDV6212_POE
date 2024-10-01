using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ABC_Retail.Services;
using System.Linq;

namespace ABC_Retail_Functions.Functions
{
	public class UploadImageFunction
	{
		private readonly AzureBlobStorageService _blobStorageService;

		public UploadImageFunction(AzureBlobStorageService blobStorageService)
		{
			_blobStorageService = blobStorageService;
		}

		[FunctionName("UploadImageFunction")]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("File upload request received.");

			if (!req.Form.Files.Any())
			{
				log.LogWarning("No file found in the request.");
				return new BadRequestObjectResult("No file found in the request.");
			}

			IFormFile formFile = req.Form.Files[0];

			try
			{
				string blobName = await _blobStorageService.UploadFileAsync(formFile);
				log.LogInformation($"File uploaded to blob: {blobName}");
				return new OkObjectResult(blobName);
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error occurred while uploading the file.");
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}
		}
	}
}