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
	/// <summary>
	/// Azure Function to handle image uploads to Azure Blob Storage.
	/// </summary>
	public class UploadImageFunction
	{
		private readonly AzureBlobStorageService _blobStorageService;

		/// <summary>
		/// Constructor to initialize the blob storage service.
		/// </summary>
		/// <param name="blobStorageService">Service for handling Azure Blob Storage operations.</param>
		public UploadImageFunction(AzureBlobStorageService blobStorageService)
		{
			_blobStorageService = blobStorageService;
		}

		/// <summary>
		/// HTTP trigger function to process the image upload request.
		/// </summary>
		/// <param name="req">HTTP request containing the image file to be uploaded.</param>
		/// <returns>ActionResult indicating the result of the operation.</returns>
		[FunctionName("UploadImageFunction")]
		public async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("File upload request received.");

			// Check if the request contains any files
			if (!req.Form.Files.Any())
			{
				log.LogWarning("No file found in the request.");
				return new BadRequestObjectResult("No file found in the request.");
			}

			IFormFile formFile = req.Form.Files[0];

			try
			{
				// Upload the file to Azure Blob Storage
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