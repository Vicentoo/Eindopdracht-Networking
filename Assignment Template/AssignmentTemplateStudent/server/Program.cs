using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LibData;
using System.Collections.Generic;
using System.IO;


class Program
{
    static void Main(string[] args)
    {
        ServerUDP.start();
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
    static string configFile = @"../Setting.json";
    static string dnsFile = @"../DNSrecords.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);
    static List<DNSRecord> dnsRecords = JsonSerializer.Deserialize<List<DNSRecord>>(File.ReadAllText(dnsFile));

    public static void start()
    {
        UdpClient udpServer = new UdpClient(setting.ServerPortNumber);
        IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
        Console.WriteLine($"Server started on {setting.ServerIPAddress}:{setting.ServerPortNumber}");

        while (true)
        {
            byte[] receivedBytes = udpServer.Receive(ref clientEndpoint);
            string receivedMessage = Encoding.UTF8.GetString(receivedBytes);
            Message message = JsonSerializer.Deserialize<Message>(receivedMessage);
            Console.WriteLine($"Received {message.MsgType} from client {clientEndpoint}");

            if (message.MsgType == MessageType.Hello)
            {
                Console.WriteLine("Hello message received");
                SendResponse(udpServer, clientEndpoint, new Message { MsgId = message.MsgId, MsgType = MessageType.Welcome });
            }
            else if (message.MsgType == MessageType.DNSLookup)
            {
                Console.WriteLine("DNSLookup message received.");
                HandleDNSLookup(udpServer, clientEndpoint, message);
            }
            else if (message.MsgType == MessageType.Ack)
            {
                Console.WriteLine("Received Ack message.");
                Console.WriteLine("Acknowledgment received.");
            }
            else if (message.MsgType == MessageType.End)
            {
                Console.WriteLine("Received End message.");
                continue;
            }
            else
            {
                Console.WriteLine("Unknown message type received.");
            }
        }
    }

    private static void HandleDNSLookup(UdpClient udpServer, IPEndPoint clientEndpoint, Message requestMessage)
    {
        var requestData = JsonSerializer.Deserialize<DNSRecord>(requestMessage.Content.ToString());
        Console.WriteLine($"Received DNSLookup for {requestData.Type} - {requestData.Name}");

        var matchingRecord = dnsRecords.Find(r => r.Type == requestData.Type && r.Name == requestData.Name);

        // Stuur een DNSLookupReply of Error afhankelijk van de uitkomst
        if (matchingRecord != null)
        {
            Console.WriteLine($"Record found: {matchingRecord.Type} - {matchingRecord.Name}");
            SendResponse(udpServer, clientEndpoint, new Message
            {
                MsgId = requestMessage.MsgId,
                MsgType = MessageType.DNSLookupReply,
                Content = matchingRecord
            });
        }
        else
        {
            Console.WriteLine($"Record not found for {requestData.Type} - {requestData.Name}");
            SendResponse(udpServer, clientEndpoint, new Message
            {
                MsgId = requestMessage.MsgId,
                MsgType = MessageType.Error,
                Content = "Domain not found"
            });
        }
    }

    private static void SendResponse(UdpClient udpServer, IPEndPoint clientEndpoint, Message responseMessage)
    {
        // Serialiseer het antwoordbericht naar JSON
        string jsonResponse = JsonSerializer.Serialize(responseMessage);
        byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);

        // Stuur het bericht naar de client
        udpServer.Send(responseBytes, responseBytes.Length, clientEndpoint);
        Console.WriteLine($"Sent {responseMessage.MsgType} to client {clientEndpoint}");
    }
}
