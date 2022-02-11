using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace RicherCode.Voyager3nPlus1.FunctionApp
{
    public static class Voyager_3nPlus1
    {
        [FunctionName("Orchestrator_Voyager_3nPlus1")]
        public static async Task<double> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            double seedValue = 1;
            var currentTasks = new List<Task>(); // ValueTasks??

            // Read seed value from storage

            int batchSize = 1000000;
            int groupCount = 10;

            while (seedValue < 1000000000) // Change this to check for some break condition - or maybe not bother
            {
                for (int i = 0; i < groupCount; i++)
                {
                    currentTasks.Add(context.CallActivityAsync("Activity_3nPlus1_CalculateBatch", new CaclulateParameters(seedValue, batchSize)));
                    seedValue += batchSize;
                }

                await Task.WhenAll(currentTasks);

                currentTasks.Clear();

                //log.LogInformation($"Current seed value is {seedValue}.");

                // Store seedValue
            }

            return seedValue;
        }

        public record CaclulateParameters (double StartValue, double Count);
        public record CalculateOutputs (double NumberOfIterations, double MaxValue);


        [FunctionName("Activity_3nPlus1_CalculateBatch")]
        public static void CalculateBatch([ActivityTrigger] CaclulateParameters param, ILogger log)
        {
            double seedValue = param.StartValue;
            for (double i=0; i < param.Count; i++, seedValue++)
            {
                var results = CalculateSequence(seedValue);
                //log.LogInformation($"Completed seed value {seedValue}. Loop ended in {results.NumberOfIterations} iterations.  Max value was {results.MaxValue}");
            }

            log.LogInformation($"Completed seed {seedValue}.");
        }

        private static CalculateOutputs CalculateSequence(double seedValue)
        {
            var currentValue = seedValue;
            double numberOfIterations = 0;
            double maxValue = seedValue;
            while (currentValue != 1)
            {
                if (currentValue % 2 == 0)
                {
                    // Even
                    currentValue /= 2;
                }
                else
                {
                    // Odd
                    currentValue = (3 * currentValue) + 1;
                }

                if (currentValue > maxValue)
                    maxValue = currentValue;

                numberOfIterations++;
            }

            return new CalculateOutputs(numberOfIterations, maxValue);
        }

        [FunctionName("Voyager_3nPlus1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Orchestrator_Voyager_3nPlus1", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}