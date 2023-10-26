using WebhookProcessor.Utilities;

namespace WebhookProcessor.BackgroundJobs;

public class ProcessWebhook
{
    public void Process(int id)
    {
        Thread.Sleep(3000);
        ConsoleWriter.PrintColorMessage($"----> Webhook {id} Processed", ConsoleColor.Yellow);
    }
    
}