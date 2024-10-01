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
	public class UploadFileFunction
	{
		private readonly AzureFileStorageService _fileStorageService;

		public UploadFileFunction(AzureFileStorageService fileStorageService)
		{
			_fileStorageService = fileStorageService;
		}

		[FunctionName("UploadFileFunction")]
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
			string fileName = formFile.FileName;

			log.LogInformation($"File to upload: {fileName}");

			try
			{
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