using System.Collections.Immutable;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;

// SendTo();
class Program
{
    static void Main(string[] args)
    {
        ClientUDP.Start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ClientUDP
{
    static string configFile = @"../Setting.json";
    static Setting? setting;
    static UdpClient client;
    static IPEndPoint serverEndPoint;

    public static void Start()
    {
        LoadConfig();
        InitializeClient();
        SendHelloMessage();
        ReceiveWelcomeMessage();
        PerformDNSLookups();
        ReceiveEndMessage();
    }


    //TODO: [Deserialize Setting.json]
    static void LoadConfig()
    {
        try
        {
            string configContent = File.ReadAllText(configFile);
            setting = JsonSerializer.Deserialize<Setting>(configContent);

            if (setting == null)
            {
                Console.WriteLine("Failed to load settings.");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config: {ex.Message}");
            Environment.Exit(1);
        }
    }

    //TODO: [Create endpoints and socket]

    static void InitializeClient()
    {
        client = new UdpClient(setting.ClientPortNumber);
        serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);

        Console.WriteLine($"Client started on {setting.ClientIPAddress}:{setting.ClientPortNumber}");
        Console.WriteLine($"Connecting to server at {setting.ServerIPAddress}:{setting.ServerPortNumber}");
    }


    //TODO: [Create and send HELLO]
    static void SendHelloMessage()
    {
        Message helloMessage = new Message
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "Hello from client"
        };

        SendMessage(helloMessage);
        Console.WriteLine("Sent HELLO message.");
    }


    //TODO: [Receive and print Welcome from server]
    static void ReceiveWelcomeMessage()
    {
        Message? responseMessage = ReceiveMessage();

        if (responseMessage != null && responseMessage.MsgType == MessageType.Welcome)
        {
            Console.WriteLine("Received WELCOME from server: " + responseMessage.Content);
        }
        else
        {
            Console.WriteLine("Unexpected response from server.");
        }
    }


    // TODO: [Create and send DNSLookup Message]
    static void PerformDNSLookups()
    {
        string[] domainsToLookup = { "google.com", "example.com", "invalid.domain" };
        int msgId = 2;

        foreach (var domain in domainsToLookup)
        {
            SendDNSLookup(domain, msgId++);
            ReceiveDNSLookupReply();
        }
    }


    // TODO: [Create and send DNSLookup Message]
    static void SendDNSLookup(string domain, int msgId)
    {
        Message dnsLookupMessage = new Message
        {
            MsgId = msgId,
            MsgType = MessageType.DNSLookup,
            Content = domain
        };

        SendMessage(dnsLookupMessage);
        Console.WriteLine($"Sent DNSLookup for {domain}");
    }


    //TODO: [Receive and print DNSLookupReply from server]
    static void ReceiveDNSLookupReply()
    {
        Message? responseMessage = ReceiveMessage();

        if (responseMessage != null && responseMessage.MsgType == MessageType.DNSLookupReply)
        {
            Console.WriteLine($"Received DNSLookupReply: {responseMessage.Content}");
            SendAcknowledgment(responseMessage.MsgId);
        }
        else
        {
            Console.WriteLine("Unexpected response for DNSLookup.");
        }
    }


    //TODO: [Send Acknowledgment to Server]
    static void SendAcknowledgment(int msgId)
    {
        Message ackMessage = new Message
        {
            MsgId = msgId,
            MsgType = MessageType.Ack,
            Content = "Acknowledged"
        };

        SendMessage(ackMessage);
        Console.WriteLine("Sent Acknowledgment.");
    }


    // TODO: [eindbericht]
    static void ReceiveEndMessage()
    {
        Message? responseMessage = ReceiveMessage();

        if (responseMessage != null && responseMessage.MsgType == MessageType.End)
        {
            Console.WriteLine("Received END message from server. Closing connection.");
        }
    }


    static void SendMessage(Message message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);
        byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
        client.Send(messageBytes, messageBytes.Length, serverEndPoint);
    }

    static Message? ReceiveMessage()
    {
        IPEndPoint serverResponseEndPoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] responseBytes = client.Receive(ref serverResponseEndPoint);
        string responseJson = Encoding.UTF8.GetString(responseBytes);

        return JsonSerializer.Deserialize<Message>(responseJson);
    }


}

