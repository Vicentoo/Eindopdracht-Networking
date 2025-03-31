using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LibData; // Zorg ervoor dat je Message, MessageType en DNSRecord definieert

class ServerUDP
{
    static UdpClient udpServer;
    static Dictionary<string, DNSRecord> dnsDatabase = new();

    static void Main()
    {
        LoadDNSRecords();

        udpServer = new UdpClient(11000);
        Console.WriteLine("Server gestart op 127.0.0.1:11000");

        while (true)
        {
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] requestData = udpServer.Receive(ref clientEndpoint);
            string requestJson = Encoding.UTF8.GetString(requestData);
            Message requestMessage = JsonSerializer.Deserialize<Message>(requestJson);

            Console.WriteLine($"Ontvangen {requestMessage.MsgType} van client {clientEndpoint}");

            switch (requestMessage.MsgType)
            {
                case MessageType.Hello:
                    HandleHello(clientEndpoint);
                    break;
                case MessageType.DNSLookup:
                    HandleDNSLookup(clientEndpoint, requestMessage);
                    break;
                case MessageType.Ack:
                    Console.WriteLine($"Ontvangen Ack voor MsgId {requestMessage.Content}");
                    break;
                default:
                    Console.WriteLine("Onbekend berichttype ontvangen.");
                    break;
            }
        }
    }

    static void LoadDNSRecords()
    {
        try
        {
            string json = File.ReadAllText("dnsrecords.json");
            List<DNSRecord> records = JsonSerializer.Deserialize<List<DNSRecord>>(json);

            foreach (var record in records)
            {
                dnsDatabase[record.Name] = record;
            }

            Console.WriteLine("DNS-records succesvol geladen.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fout bij laden van DNS-records: {ex.Message}");
        }
    }

    static void HandleHello(IPEndPoint clientEndpoint)
    {
        Console.WriteLine("Hello bericht ontvangen");

        Message welcomeMessage = new Message
        {
            MsgId = 4,
            MsgType = MessageType.Welcome,
            Content = "Welkom van de server"
        };

        SendMessage(welcomeMessage, clientEndpoint);
        Console.WriteLine("Verstuurde Welkom naar client");
    }

    static void HandleDNSLookup(IPEndPoint clientEndpoint, Message requestMessage)
    {
        Console.WriteLine($"DNSLookup ontvangen voor {requestMessage.Content}");

        if (requestMessage.Content is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
        {
            string domain = jsonElement.GetString();
            if (dnsDatabase.TryGetValue(domain, out DNSRecord record))
            {
                Console.WriteLine($"Record gevonden: {record.Type} - {record.Name}");

                Message reply = new Message
                {
                    MsgId = requestMessage.MsgId,
                    MsgType = MessageType.DNSLookupReply,
                    Content = record
                };

                SendMessage(reply, clientEndpoint);
            }
            else
            {
                Console.WriteLine("Domein niet gevonden");

                Message errorReply = new Message
                {
                    MsgId = requestMessage.MsgId + 1000, // Uniek ID voor foutberichten
                    MsgType = MessageType.Error,
                    Content = "Domein niet gevonden"
                };

                SendMessage(errorReply, clientEndpoint);
            }
        }
    }

    static void SendMessage(Message message, IPEndPoint clientEndpoint)
    {
        string responseJson = JsonSerializer.Serialize(message);
        byte[] responseData = Encoding.UTF8.GetBytes(responseJson);
        udpServer.Send(responseData, responseData.Length, clientEndpoint);
    }
}