using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Workflows.Services;

namespace Workflows
{
    public class HttpFuncs
    {
        private readonly IDataContext _context;
        public HttpFuncs(IDataContext context)
        {
            _context = context;
        }
        [FunctionName("GetProspects")]
        public async Task<IActionResult> GetProspects(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting Prospects");
            return new OkObjectResult(_context.GetProspects());

        }
        [FunctionName("UpdateProspects")]
        public async Task<IActionResult> UpdateProspects(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var id = req.Query["id"];
            var status = req.Query["status"];
            log.LogInformation("Updating Prospect");
            await _context.UpdateProspectStatus(id, status);
            return new OkObjectResult("Updated");

        }
    }
}
