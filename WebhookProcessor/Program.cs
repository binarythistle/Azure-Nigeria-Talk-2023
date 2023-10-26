using Microsoft.Extensions.Configuration;
using WebhookProcessor.Utilities;
using Azure.Messaging.ServiceBus;
using Hangfire;
using Hangfire.MemoryStorage;
using WebhookProcessor.BackgroundJobs;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .Build();

GlobalConfiguration.Configuration.UseMemoryStorage();
JobStorage.Current = new MemoryStorage();



ServiceBusClient client;
ServiceBusProcessor processor;

string connectionString = configuration["ServiceBusConnectionString"]!;
string topicName = configuration["ServiceBusTopicName"]!;
string subscriptionName = configuration["ServiceBusSubscriptionName"]!;
int count = 0;



ConsoleWriter.PrintColorMessage("\n-------------------------------------", ConsoleColor.Green);
ConsoleWriter.PrintColorMessage("---------- WEBHOOK PROCESSOR --------", ConsoleColor.Green);
ConsoleWriter.PrintColorMessage("-------------------------------------", ConsoleColor.Green);

ConsoleWriter.PrintColorMessage("--[Nigeria Azure Community Edition]--", ConsoleColor.Green);

ConsoleWriter.PrintColorMessage($"\n-> Service Bus Topic: {topicName}", ConsoleColor.Yellow);
ConsoleWriter.PrintColorMessage($"-> Service Bus Subscription: {subscriptionName}", ConsoleColor.Yellow);



// handle received messages
async Task MessageHandler(ProcessMessageEventArgs args)
{
    count++;
    //string body = args.Message.Body.ToString();

    ConsoleWriter.PrintColorMessage($"Received {count}", ConsoleColor.Cyan);

    BackgroundJob.Enqueue(() => new ProcessWebhook().Process(count));

    // complete the message. messages is deleted from the subscription. 
    await args.CompleteMessageAsync(args.Message);
}

// handle any errors when receiving messages
Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}

client = new ServiceBusClient(connectionString);

processor = client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions());


ConsoleWriter.PrintColorMessage("--> Connecting to Azure Service Bus...", ConsoleColor.Blue);

try
{
    //Start hangfire server
    //using (var server = new BackgroundJobServer())
    //{

    //    ConsoleWriter.PrintColorMessage("--> Hangfire Server started.", ConsoleColor.Red);

    //}
    // add handler to process messages
    processor.ProcessMessageAsync += MessageHandler;

    // add handler to process any errors
    processor.ProcessErrorAsync += ErrorHandler;

    // start processing 
    await processor.StartProcessingAsync();

    ConsoleWriter.PrintColorMessage("---> Connected", ConsoleColor.Green);
    ConsoleWriter.PrintColorMessage("---> Waiting to process webhooks...", ConsoleColor.Green);

    //Console.WriteLine("\nPress any key to end the processing");
    using (var server = new BackgroundJobServer())
    {
        ConsoleWriter.PrintColorMessage("--> Hangfire Server started.", ConsoleColor.Red);
        Console.ReadKey();
    }

    // stop processing 
    Console.WriteLine("\nStopping the receiver...");
    await processor.StopProcessingAsync();
    Console.WriteLine("Stopped receiving messages");
}
finally
{
    await processor.DisposeAsync();
    await client.DisposeAsync();
}