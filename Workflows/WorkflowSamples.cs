using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Workflows.Models;
using Workflows.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Workflows
{
    public class WorkflowSamples
    {
        private readonly IDataContext _context;
        public WorkflowSamples(IDataContext context)
        {
            _context = context;
        }
        [FunctionName("StartOffer")]
        public async Task<List<string>> StartOffer(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var prospectId = context.GetInput<string>();
            var offerPath = await context.CallActivityAsync<OfferType>(nameof(DetermineOfferType), prospectId);
            log.LogInformation($"Prospect {prospectId} will continue down {offerPath.ToString()} path");
            var outputs = new List<string>();
            switch (offerPath)
            {
                case OfferType.Standard:
                    outputs.Add(await context.CallActivityAsync<string>(nameof(SendContractStandard), prospectId));
                    break;
                case OfferType.NonStandard:
                    outputs.Add(await context.CallActivityAsync<string>(nameof(SendContractNonStandard), prospectId));
                    outputs.Add(await context.CallActivityAsync<string>(nameof(NotifyDOATeam), prospectId));
                    break;
                default:
                    break;
            }

            return outputs;
        }
        [FunctionName(nameof(DetermineOfferType))]
        public async Task<OfferType> DetermineOfferType([ActivityTrigger] string prospectId, ILogger log)
        {
            log.LogInformation($"Determining Offer Type for {prospectId}.");
            var result = _context.GetProspect(prospectId);
            log.LogInformation($"Prospect {prospectId} experienced = {result.IsExperienced}.");
            return result.IsExperienced? OfferType.NonStandard :OfferType.Standard;
        }
        [FunctionName(nameof(SendContractStandard))]
        public async Task<string> SendContractStandard([ActivityTrigger] string prospectId, ILogger log)
        {
            log.LogInformation($"Sending Standard Contract for {prospectId}.");
            Thread.Sleep(5000);
            return "Sent Standard";
        }
        [FunctionName(nameof(SendContractNonStandard))]
        public async Task<string> SendContractNonStandard([ActivityTrigger] string prospectId, ILogger log)
        {
            log.LogInformation($"Sending Non-Standard Contract for {prospectId}.");
            Thread.Sleep(10000);
            return "Sent Non Standard";
        }
        [FunctionName(nameof(NotifyDOATeam))]
        public async Task<string> NotifyDOATeam([ActivityTrigger] string prospectId, ILogger log)
        {
            log.LogInformation($"Notifying DOA Team on NonStandard Contract for {prospectId}.");
            Thread.Sleep(5000);
            return "Notified DOA Team";
        }
        [FunctionName("StartOffer_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            var inputs = req.RequestUri.ParseQueryString();
            string instanceId = await starter.StartNewAsync("StartOffer", null, inputs["id"]);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}