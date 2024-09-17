using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABC_Retail_Functions.Functions
{
    public class AzureFunctionRoot
    {
        private readonly ILogger<AzureFunctionRoot> _logger;

        public AzureFunctionRoot(ILogger<AzureFunctionRoot> logger)
        {
            _logger = logger;
        }

        [Function("HTTPTest")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }

        
    }
}
