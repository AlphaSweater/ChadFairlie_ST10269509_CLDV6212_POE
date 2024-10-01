using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ABC_Retail.Services;
using System.Linq;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;

namespace ABC_Retail_Functions.Functions
{
    public class UploadImageFunction
    {
		// Service for interacting with Azure Blob Storage.
		private readonly AzureBlobStorageService _blobStorageService;

		public UploadImageFunction(AzureBlobStorageService blobStorageService)
		{
			_blobStorageService = blobStorageService;
		}


		[FunctionName("UploadImageFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
			log.LogInformation("File upload request received.");

			// Check if there are files in the request
			if (!req.Form.Files.Any())
			{
				return new BadRequestObjectResult("No file found in the request.");
			}

			IFormFile formFile = req.Form.Files[0];
			string blobName = null;

			try
			{
				blobName = await _blobStorageService.UploadFileAsync(formFile);

				log.LogInformation($"File uploaded to blob: {blobName}");

				// Return the blob name or URL as a result
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