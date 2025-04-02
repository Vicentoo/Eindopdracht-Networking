using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LibData;
using System;

class Program
{
    static void Main(string[] args)
    {
        ServerUDP.Start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ServerUDP
{
    static string configFile = "../Setting.json";
    static string DNSrecordsFile = "DNSrecords.json";
    static Setting? setting;
    static List<DNSRecord> dnsRecords;

    static void LoadConfiguration()
    {
        string configContent = File.ReadAllText(configFile);
        setting = JsonSerializer.Deserialize<Setting>(configContent);
    }

    static void LoadDNSRecords()
    {
        string recordsContent = File.ReadAllText(DNSrecordsFile);
        dnsRecords = JsonSerializer.Deserialize<List<DNSRecord>>(recordsContent);
    }

    public static void Start()
    {
        LoadConfiguration();
        LoadDNSRecords();

        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        serverSocket.Bind(serverEndPoint);

        Console.WriteLine("UDP Server is running...");

        while (true)
        {
            byte[] buffer = new byte[1024];
            EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            int receivedBytes = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
            string ClientInformation = $"{setting.ClientIPAddress}:{setting.ClientPortNumber}";

            Message message = JsonSerializer.Deserialize<Message>(receivedMessage);
            Console.WriteLine($"Received from {ClientInformation}: {receivedMessage}");

            if (message.MsgType == MessageType.Hello)
            {
                SendResponse(serverSocket, clientEndPoint, new Message
                {
                    MsgId = new Random().Next(),
                    MsgType = MessageType.Welcome,
                    Content = "Welcome from server"
                });
            }
            else if (message.MsgType == MessageType.DNSLookup)
            {
                HandleDNSLookup(serverSocket, clientEndPoint, message);
            }
            else if (message.MsgType == MessageType.End)
            {
                SendResponse(serverSocket, clientEndPoint, new Message 
                { 
                    MsgId = new Random().Next(), 
                    MsgType = MessageType.End, 
                    Content = "End of DNSLookup" 
                });

                // After handling all DNS lookups, send the "End" message.
                Console.WriteLine("End message sent. Waiting for new clients...");
            }
            else
            {
                Console.WriteLine("Unknown message type received.");
            }
        }
    }

    static void HandleDNSLookup(Socket serverSocket, EndPoint clientEndPoint, Message message)
    {
        string domainName = message.Content.ToString();
        DNSRecord record = dnsRecords.Find(r => r.Name == domainName);

        if (record != null)
        {
            SendResponse(serverSocket, clientEndPoint, new Message { MsgId = message.MsgId, MsgType = MessageType.DNSLookupReply, Content = record });
        }
        else
        {
            SendResponse(serverSocket, clientEndPoint, new Message { MsgId = new Random().Next(), MsgType = MessageType.Error, Content = "Domain not found" });
        }
    }

    static void SendResponse(Socket socket, EndPoint clientEndPoint, Message response)
    {
        string responseMessage = JsonSerializer.Serialize(response);
        byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
        socket.SendTo(responseBytes, clientEndPoint);
        Console.WriteLine($"Server Sent: {responseMessage}");
    }
}
