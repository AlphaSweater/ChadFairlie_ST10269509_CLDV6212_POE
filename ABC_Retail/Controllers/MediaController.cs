using Azure.Storage.Blobs;
using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
	/// <summary>
	/// Controller responsible for handling media-related actions such as listing, uploading, and deleting media files.
	/// </summary>
	public class MediaController : Controller
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Dependencies
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// Service for interacting with Azure Blob Storage.
		private readonly AzureBlobStorageService _blobStorageService;

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		/// <summary>
		/// Initializes a new instance of the <see cref="MediaController"/> class.
		/// </summary>
		/// <param name="blobStorageService">The service for Azure Blob Storage operations.</param>
		public MediaController(AzureBlobStorageService blobStorageService)
		{
			_blobStorageService = blobStorageService;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Index Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Displays a list of all media files stored in Azure Blob Storage on the index page.
		/// </summary>
		/// <returns>A view displaying a list of media file view models.</returns>
		public async Task<IActionResult> Index()
		{
			// Retrieve the list of files from Azure Blob Storage.
			var files = await _blobStorageService.ListFilesAsync();

			// Map the blob files to media file view models.
			var mediaFiles = files.Select(f => new MediaFileViewModel
			{
				FileName = f.Name,
				Url = f.Uri.ToString()
			}).ToList();

			// Return the view with the list of media file view models.
			return View(mediaFiles);
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Upload Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Handles the upload of a new media file to Azure Blob Storage.
		/// </summary>
		/// <param name="file">The media file to be uploaded.</param>
		/// <returns>
		/// A redirect to the index action after successful upload, or an error view if the file is invalid.
		/// </returns>
		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile file)
		{
			if (file != null && file.Length > 0)
			{
				// Upload the file to Azure Blob Storage and get the file URL.
				var url = await _blobStorageService.UploadFileAsync(file);

				// Redirect to the index action after successful upload.
				return RedirectToAction("Index");
			}

			// Return an error view if no file is selected or the file is empty.
			return View("Error", new ErrorViewModel { RequestId = "No file selected or file is empty." });
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Delete Actions
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Deletes a specific media file from Azure Blob Storage.
		/// </summary>
		/// <param name="fileName">The name of the file to be deleted.</param>
		/// <returns>A redirect to the index action after successful deletion.</returns>
		[HttpPost]
		public async Task<IActionResult> Delete(string fileName)
		{
			// Delete the specified file from Azure Blob Storage.
			await _blobStorageService.DeleteFileAsync(fileName);

			// Redirect to the index action after successful deletion.
			return RedirectToAction("Index");
		}
	}
}