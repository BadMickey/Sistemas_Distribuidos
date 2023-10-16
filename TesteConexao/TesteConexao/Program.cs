﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static TcpClient client;
    static NetworkStream stream;

    static void Main()
    {
        client = new TcpClient();
        client.Connect("10.4.5.4", 13000); // Substitua "EnderecoIPDoCliente2" pelo IP do Cliente 2

        stream = client.GetStream();

        Thread receiveThread = new Thread(ReceiveMessages);
        receiveThread.Start();

        while (true)
        {
            Console.Write("Cliente 1: ");
            string message = Console.ReadLine();
            SendMessage(message);
        }
    }

    static void ReceiveMessages()
    {
        while (true)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Cliente 2: " + dataReceived);
        }
    }

    static void SendMessage(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }
}
