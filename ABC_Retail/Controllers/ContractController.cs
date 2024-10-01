using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ABC_Retail.Controllers
{
	public class ContractController : Controller
	{
		private readonly AzureFileStorageService _fileStorageService;
		private readonly string _contractShareName = "contracts";

		public ContractController(AzureFileStorageService fileStorageService, IConfiguration configuration)
		{
			_fileStorageService = fileStorageService;
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// List all contracts
		public async Task<IActionResult> Index()
		{
			var contractFiles = await _fileStorageService.ListFilesAsync("contracts");
			return View(contractFiles);
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Upload a new contract file
		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile file)
		{
			if (file != null && file.Length > 0)
			{
				var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
				var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

				if (!allowedExtensions.Contains(extension))
				{
					ModelState.AddModelError("File", "Invalid file type. Only PDF and Word documents are allowed.");
					var existingContractFiles = await _fileStorageService.ListFilesAsync(_contractShareName);
					return View("Index", existingContractFiles);
				}

				using (var stream = file.OpenReadStream())
				{
					await _fileStorageService.UploadFileAsync(stream, file.FileName, _contractShareName);
				}
				return RedirectToAction("Index");
			}

			ModelState.AddModelError("File", "No file selected or file is empty.");
			var contractFiles = await _fileStorageService.ListFilesAsync(_contractShareName);
			return View("Index", contractFiles);
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Display a Contract in detail
		public async Task<IActionResult> Details(string fileName)
		{
			var fileDetails = await _fileStorageService.GetFileDetailsAsync(fileName, _contractShareName);

			return View(new ContractFileViewModel
			{
				FileName = fileName,
				Url = Url.Action("Download", new { fileName }), // For download
				InlineUrl = Url.Action("InlineView", new { fileName }), // For inline view
				FileSize = fileDetails.ContentLength,
				LastModified = fileDetails.LastModified
			});
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Inline view of a contract file
		public async Task<IActionResult> InlineView(string fileName)
		{
			var stream = await _fileStorageService.DownloadFileAsync(fileName, _contractShareName);
			return File(stream, "application/pdf");
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Download a contract file
		public async Task<IActionResult> Download(string fileName)
		{
			var stream = await _fileStorageService.DownloadFileAsync(fileName, _contractShareName);

			if (stream == null)
			{
				return NotFound();
			}

			// Set the correct content type and force download
			return File(stream, "application/octet-stream", fileName);
		}


		//--------------------------------------------------------------------------------------------------------------------------//
		// Delete a contract file
		[HttpPost]
		public async Task<IActionResult> Delete(string fileName)
		{
			await _fileStorageService.DeleteFileAsync(fileName, _contractShareName);
			return RedirectToAction("Index");
		}
	}
}