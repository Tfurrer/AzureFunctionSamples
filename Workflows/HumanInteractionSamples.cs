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
    public class HumanInteractionSamples
    {
        private readonly IDataContext _context;
        public HumanInteractionSamples(IDataContext context)
        {
            _context = context;
        }
        [FunctionName("StartHuman_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("ApprovalWorkflow", null);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
        [FunctionName("ApprovalWorkflow")]
        public static async Task ApprovalWorkflow([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            //await context.CallActivityAsync("RequestApproval", null);
            using (var timeoutCts = new CancellationTokenSource())
            {
                DateTime dueTime = context.CurrentUtcDateTime.AddMinutes(1);
                Task durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);

                Task<bool> approvalEvent = context.WaitForExternalEvent<bool>("ApprovalEvent");
                if (approvalEvent == await Task.WhenAny(approvalEvent, durableTimeout))
                {
                    timeoutCts.Cancel();
                    log.LogInformation($"Process Approval '{approvalEvent.Result}'.");
                    //await context.CallActivityAsync("ProcessApproval", approvalEvent.Result);
                }
                else
                {
                    log.LogInformation($"Process Escalation.");
                    //await context.CallActivityAsync("Escalate", null);
                }
            }
        }
        [FunctionName("RaiseEventToOrchestration")]
        public static async Task RaiseEventToOrchestration([HttpTrigger] string instanceId,[DurableClient] IDurableOrchestrationClient client)
        {
            bool isApproved = true;
            await client.RaiseEventAsync(instanceId, "ApprovalEvent", isApproved);
        }

    }
}