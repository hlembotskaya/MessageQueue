using System;

namespace ServiceBusSender
{
    class Program 
    {
        private const string path = @"D:\PDF_Output\";
        private const string connectionString = "";
        private const string queueName = "queue1";

        static async Task Main(string[] args)
        {
            MessageReceiver receiver = new MessageReceiver(connectionString, queueName);
            await receiver.ReceiveAndAssembleFileAsync(path);
        }
    }
}