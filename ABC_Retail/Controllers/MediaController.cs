using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
	public class MediaController : Controller
	{
		private readonly AzureBlobStorageService _blobStorageService;

		public MediaController(AzureBlobStorageService blobStorageService)
		{
			_blobStorageService = blobStorageService;
		}

		// List all media files
		public async Task<IActionResult> Index()
		{
			var files = await _blobStorageService.ListFilesAsync();
			var mediaFiles = files.Select(f => new MediaFileViewModel
			{
				FileName = Path.GetFileName(f),
				Url = f
			}).ToList();

			return View(mediaFiles);
		}

		// Upload a new file
		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile file)
		{
			if (file != null && file.Length > 0)
			{
				var url = await _blobStorageService.UploadFileAsync(file);
				return RedirectToAction("Index");
			}

			return View("Error", new ErrorViewModel { RequestId = "No file selected or file is empty." });
		}
	}
}