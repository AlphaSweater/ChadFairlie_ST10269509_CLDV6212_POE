using Microsoft.AspNetCore.Http;
using System;

namespace ABC_Retail.ViewModels
{
	public class ContractFileViewModel
	{
		public string FileName { get; set; } // Name of the file
		public string Url { get; set; } // URL to download the file
		public string InlineUrl { get; set; } // For inline view
		public long FileSize { get; set; } // Size of the file in bytes
		public DateTimeOffset LastModified { get; set; } // Last modified date of the file
	}

	public class UploadContractViewModel
	{
		public IFormFile File { get; set; }
	}
}