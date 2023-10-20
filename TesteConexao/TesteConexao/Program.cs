using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static TcpListener listener;
    static TcpClient client;

    static void Main()
    {
        string remoteIP = "192.168.0.217";

        // Inicie o servidor
        IPAddress localAddr = IPAddress.Parse("192.168.0.143"); // Escuta em todas as interfaces
        int port = 13000;
        listener = new TcpListener(localAddr, port);
        listener.Start();

        // Conecte-se ao outro cliente (máquina)
        client = new TcpClient();
        client.Connect(remoteIP, port);

        // Inicie a thread para receber mensagens do servidor
        Thread receiveThread = new Thread(ReceiveMessages);
        receiveThread.Start();

        // Loop para enviar mensagens
        while (true)
        {
            Console.Write("Digite uma mensagem para enviar ao outro cliente: ");
            string message = Console.ReadLine();
            SendMessage(message);
        }
    }

    static void ReceiveMessages()
    {
        NetworkStream stream = client.GetStream();
        while (true)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Mensagem recebida: " + dataReceived);
        }
    }

    static void SendMessage(string message)
    {
        NetworkStream stream = client.GetStream();
        byte[] data = Encoding.ASCII.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }
}
