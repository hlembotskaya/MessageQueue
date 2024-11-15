using Azure.Messaging.ServiceBus;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace QueryReceiver
{
    class Program
    {
        public static void Main(string[] args)
        {
            string path = @"D:\\PDF";
            string connectionString = "";
            string queueName = "queue1";

            PdfFolderWatcher watcher = new PdfFolderWatcher(path, connectionString, queueName);
            watcher.StartWatching();

            Console.WriteLine("Press Enter...");
            Console.ReadLine();
        }
       
    }

}

