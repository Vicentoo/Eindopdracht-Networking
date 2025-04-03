using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.IO;
using LibData;

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
    private static readonly string ConfigFile = "../Setting.json";
    private static readonly Setting? Setting = JsonSerializer.Deserialize<Setting>(File.ReadAllText(ConfigFile));

    public static void Start()
    {
        if (Setting == null || string.IsNullOrEmpty(Setting.ServerIPAddress))
        {
            Console.WriteLine("Invalid configuration.");
            return;
        }

        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(Setting.ServerIPAddress), Setting.ServerPortNumber);
        using Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        SendAndAcknowledge(clientSocket, serverEndPoint, new Message { MsgId = GetUniqueId(), MsgType = MessageType.Hello, Content = "Hello from client" });

        string[] workingdomains = { "www.test.com", "example.com" };

        foreach (var domain in workingdomains)
        {
            Message request = new Message { MsgId = GetUniqueId(), MsgType = MessageType.DNSLookup, Content = domain };
            SendAndAcknowledge(clientSocket, serverEndPoint, request);
        }

        string invalidDomainString = "unknown.domain";
        Message invalidRequest1 = new Message { MsgId = GetUniqueId(), MsgType = MessageType.DNSLookup, Content = invalidDomainString };
        SendAndAcknowledge(clientSocket, serverEndPoint, invalidRequest1);

        var invalidDomainObject = new { Type = "A", Value = "www.example.com" };
        Message invalidRequest2 = new Message { MsgId = GetUniqueId(), MsgType = MessageType.DNSLookup, Content = invalidDomainObject };
        SendAndAcknowledge(clientSocket, serverEndPoint, invalidRequest2);

        Message serverReply = ReceiveMessage(clientSocket);
        if (serverReply.MsgType == MessageType.End)
        {
            clientSocket.Close();
        }
    }

    private static int Ackcount = 0;
    private static void SendAndAcknowledge(Socket socket, IPEndPoint endPoint, Message message)
    {
        SendMessage(socket, endPoint, message);
        Message reply = ReceiveMessage(socket);
        Console.WriteLine($"Client received: {JsonSerializer.Serialize(reply)}");

        if (reply == null || reply.Content == null)
        {
            Console.WriteLine("Invalid reply, no ack sent");
            return;
        }

        if (reply.MsgType == MessageType.Hello || reply.MsgType == MessageType.Welcome)
        {
            return;
        }

        string contentString = reply.Content.ToString();

        if (contentString.Contains("Domain not found", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Invalid Reply, no Ack sent");
            return;
        }

        Ackcount++;
        Console.WriteLine($"Client sent ack: {Ackcount}");
    }

    private static void SendMessage(Socket socket, IPEndPoint endPoint, Message message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);
        Console.WriteLine($"Client sent: {jsonMessage}");
        byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
        socket.SendTo(data, endPoint);
    }

    private static Message ReceiveMessage(Socket socket)
    {
        byte[] buffer = new byte[1024];
        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        int receivedBytes = socket.ReceiveFrom(buffer, ref remoteEndPoint);
        string receivedJson = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
        return JsonSerializer.Deserialize<Message>(receivedJson);
    }

    private static int GetUniqueId()
    {
        return new Random().Next(0, 100);
    }
}
