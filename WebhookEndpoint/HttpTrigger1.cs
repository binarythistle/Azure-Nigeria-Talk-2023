using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;

namespace Company.Function
{
    public static class HttpTrigger1
    {
        static string connectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
        static string topicName = Environment.GetEnvironmentVariable("ServiceBusTopicName");
        static string authHeaderKey = Environment.GetEnvironmentVariable("AuthHeaderKey");
        static string authHeaderValue = Environment.GetEnvironmentVariable("AuthHeaderValue");
        static ServiceBusClient client;
        static ServiceBusSender sender;

        [FunctionName("HttpTrigger1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("Webhook Event Received");

            // Validate Auth Header
            if(!(req.Headers.ContainsKey(authHeaderKey) && req.Headers[authHeaderKey] == authHeaderValue))
            {
                log.LogError("Auth Header Validation Failed");
                return new OkResult();
            }


            // Create Payload Object
            WebhookCreateDto webhookEvent = new ()
            {
                Headers = JsonConvert.SerializeObject(req.Headers),
                Body = await new StreamReader(req.Body).ReadToEndAsync()
            };

            // Create Service Bus Client
            client = new ServiceBusClient(connectionString);
            sender = client.CreateSender(topicName);

            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

            if (!messageBatch.TryAddMessage(new ServiceBusMessage(JsonConvert.SerializeObject(webhookEvent))))
            {
                throw new Exception($"The message is too large to fit in the batch.");
            }

            try
            {
                await sender.SendMessagesAsync(messageBatch);
                Console.WriteLine("A Batch has been placed onto the message bus topic");
                
            }
            finally
            {
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }

            return new OkObjectResult("Webhook Received OK");

        }
    }
}
