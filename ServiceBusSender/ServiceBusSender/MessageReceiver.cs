using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

public class MessageReceiver
{
    private readonly string connectionString;
    private readonly string queueName;


    public class File
    {
        public string fileName;
        public SortedDictionary<int, byte[]> chunks = new SortedDictionary<int, byte[]>();
        public int totalChunk;

    }

    public MessageReceiver(string serviceConnectionString, string serviceBusQueueName)
    {
        queueName = serviceBusQueueName;
        connectionString = serviceConnectionString;
    }

    public async Task WriteFile(File file, string path )
    {
        try
        {
            using (var fileStream = new FileStream(path + file.fileName, FileMode.Create))
            {
                foreach (var chunkData in file.chunks.Values)
                {
                    await fileStream.WriteAsync(chunkData, 0, chunkData.Length);
                }
                Console.WriteLine($"File compiled successfully in {path + file.fileName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async Task ReceiveAndAssembleFileAsync(string outputFilePath)
    {
        await using var client = new ServiceBusClient(connectionString);
        ServiceBusReceiver receiver = client.CreateReceiver(queueName);

        var files = new Dictionary<string, File>();
     

        while (true)
        { 
            var message = await receiver.ReceiveMessageAsync();

            if (message == null) break;

            var fileName = message.Subject;

            if (!files.ContainsKey(fileName)) {
                files[fileName] = new File() { fileName = fileName, totalChunk = (int)message.ApplicationProperties["TotalChunks"] };
                Console.WriteLine("Start receiving" + fileName);
            }

            int chunkIndex = (int)message.ApplicationProperties["ChunkIndex"];
            Console.WriteLine("Received" + fileName + " chunk " + chunkIndex);
            
            if (!files[fileName].chunks.ContainsKey(chunkIndex))
            {
                files[fileName].chunks.Add(chunkIndex, message.Body.ToArray());
                await receiver.CompleteMessageAsync(message);
            }
            else
            {
                Console.WriteLine($"Duplicate chunk {chunkIndex} for file {fileName} detected and ignored.");
            }

            if (files[fileName].chunks.Count == files[fileName].totalChunk)
            {
                Console.WriteLine("All chunks are received, starting to compile a file");
                await WriteFile(files[fileName], outputFilePath);
                files.Remove(fileName);
            }

        }
    }
}
