using System;
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
    public class MonitoringSamples
    {
        private readonly IDataContext _context;
        public MonitoringSamples(IDataContext context)
        {
            _context = context;
        }
        [FunctionName("StartMonitor")]
        public async Task StartMonitor(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var prospect = context.GetInput<MonitorProspectEvent>();
            while (context.CurrentUtcDateTime < prospect.ExpireTime)
            {
                var status = await context.CallActivityAsync<bool>("ArbitraryTask1", prospect.id);
                if (status)
                {
                    // Perform an action when a condition is met.
                    await context.CallActivityAsync("SendAlert", "Complete");
                    return;
                }
                // Orchestration sleeps until this time.
                var nextCheck = context.CurrentUtcDateTime.AddSeconds(5);
                await context.CreateTimer(nextCheck, CancellationToken.None);
            }
            await context.CallActivityAsync("SendAlert", "Failed");
            return;
        }
        [FunctionName(nameof(ArbitraryTask1))]
        public async Task<bool> ArbitraryTask1([ActivityTrigger] string id, ILogger log)
        {
            log.LogInformation($"Checking Status");
            var prospect = _context.GetProspect(id);
            return prospect.CurrentStatus == "complete";
        }
        [FunctionName(nameof(SendAlert))]
        public async Task<bool> SendAlert([ActivityTrigger] string message, ILogger log)
        {
            log.LogInformation($"Alert Sent: {message}");
            return true;
        }
        [FunctionName("StartMonitor_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            var inputs = req.RequestUri.ParseQueryString();
            var monitorEvent = new MonitorProspectEvent();
            monitorEvent.id = inputs["id"];
            monitorEvent.ExpireTime = DateTime.UtcNow.AddMinutes(1);
            string instanceId = await starter.StartNewAsync("StartMonitor", null, monitorEvent);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}