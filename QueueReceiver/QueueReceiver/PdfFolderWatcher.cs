using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

class PdfFolderWatcher
{
    private readonly string folderPath;
    private readonly string connectionString;
    private readonly string queueName;
    private const int ChunkSize = 262000;

    public PdfFolderWatcher(string path, string serviceBusConnectionString, string serviceBusQueueName)
    {
        folderPath = path;
        connectionString = serviceBusConnectionString;
        queueName = serviceBusQueueName;

    }

    public void StartWatching()
    {
        FileSystemWatcher watcher = new FileSystemWatcher
        {
            Path = folderPath,
            Filter = "*.pdf", 
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        watcher.Created += OnNewPdfCreated;
        watcher.EnableRaisingEvents = true;

        Console.WriteLine("Watching enabled: " + folderPath);
    }

    private async void OnNewPdfCreated(object sender, FileSystemEventArgs e)
    {
        //await Task.Delay(500); //check big data

        Console.WriteLine($"New pdf file detected: {e.FullPath}");

        await SendPdfToServiceBus(e.FullPath);
    }

    private async Task SendPdfToServiceBus(string filePath)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        int numberOfChunks = (int)Math.Ceiling((double)fileBytes.Length / ChunkSize);

        await using var client = new ServiceBusClient(connectionString);
        ServiceBusSender sender = client.CreateSender(queueName);

        try
        {
           
            for (int i = 0; i < numberOfChunks; i++)
            {
                int chunkSize = Math.Min(ChunkSize, fileBytes.Length - i * ChunkSize);
                byte[] chunkData = new byte[chunkSize];
                Array.Copy(fileBytes, i * ChunkSize, chunkData, 0, chunkSize);

                var message = new ServiceBusMessage(chunkData)
                {
                    ContentType = "application/pdf",
                    Subject = Path.GetFileName(filePath),
                    ApplicationProperties = { ["ChunkIndex"] = i, ["TotalChunks"] = numberOfChunks }
                };

                await sender.SendMessageAsync(message);
                Console.WriteLine($"Sent chunk #{i + 1}/{numberOfChunks}");
            }
            
            
            Console.WriteLine($"file {Path.GetFileName(filePath)} has been sent to the queue.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }


}