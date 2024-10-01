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
	/// Azure Function to handle file uploads to Azure Blob Storage.
	/// </summary>
	public class UploadFileFunction
	{
		private readonly AzureFileStorageService _fileStorageService;

		/// <summary>
		/// Constructor to initialize the file storage service.
		/// </summary>
		/// <param name="fileStorageService">Service for handling Azure File Storage operations.</param>
		public UploadFileFunction(AzureFileStorageService fileStorageService)
		{
			_fileStorageService = fileStorageService;
		}

		/// <summary>
		/// HTTP trigger function to process the file upload request.
		/// </summary>
		/// <param name="req">HTTP request containing the file to be uploaded.</param>
		/// <returns>ActionResult indicating the result of the operation.</returns>
		[FunctionName("UploadFileFunction")]
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
			string fileName = formFile.FileName;

			log.LogInformation($"File to upload: {fileName}");

			try
			{
				// Upload the file to Azure Blob Storage
				using var stream = formFile.OpenReadStream();
				log.LogInformation("Uploading file to Azure Blob Storage...");
				await _fileStorageService.UploadFileAsync(stream, fileName, "contracts");

				log.LogInformation($"File uploaded successfully: {fileName}");
				return new OkObjectResult(new { fileName });
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Error occurred while uploading the file.");
				return new StatusCodeResult(StatusCodes.Status500InternalServerError);
			}
		}
	}
}