using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Model;
using Banco;

public class NodeClient
{
    private const string serverIPAddress = "IP_DO_SERVIDOR"; // Substitua pelo IP do servidor
    private const int serverPort = 13000;

    private TcpClient client;
    private Blockchain blockchain;

    public NodeClient()
    {
        client = new TcpClient();
    }

    public void Start()
    {
        client.Connect(serverIPAddress, serverPort);
        Console.WriteLine("Conectado ao servidor em " + serverIPAddress + ":" + serverPort);

        // Solicita a cadeia de blocos do servidor
        RequestChain();

        // Crie e adicione um bloco ao servidor
        Block newBlock = CreateNewBlock();
        SendBlock(newBlock);

        // Aguarde a resposta do servidor
        ReceiveBlock();
    }

    private void RequestChain()
    {
        NetworkStream stream = client.GetStream();
        byte[] requestChain = Encoding.ASCII.GetBytes("REQUEST_CHAIN");
        stream.Write(requestChain, 0, requestChain.Length);

        // Aguarde a resposta do servidor
        ReceiveChain();
    }

    private void ReceiveChain()
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string chainData = Encoding.ASCII.GetString(buffer, 0, bytesRead);

        List<Block> receivedChain = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Block>>(chainData);
        Console.WriteLine("Cadeia de blocos recebida do servidor:");

        foreach (Block block in receivedChain)
        {
            Console.WriteLine($"Bloco {block.Nonce}");
        }
    }

    private Block CreateNewBlock()
    {
        Block newBlock = new Block
        {
            Nonce = 1, // Substitua pelo índice correto
            Timestamp = DateTime.Now,
            SensorId = 1,
            Address = "a",
            MotionDetected = true,
            PreviousHash = "Hash do bloco anterior",
            Hash = string.Empty
        };

        // Calcula o hash do novo bloco
        newBlock.Hash = blockchain.CalculateHash(newBlock);

        return newBlock;
    }

    private void SendBlock(Block block)
    {
        NetworkStream stream = client.GetStream();
        string blockData = Newtonsoft.Json.JsonConvert.SerializeObject(block);
        byte[] blockBytes = Encoding.ASCII.GetBytes("ADD_BLOCK:" + blockData);
        stream.Write(blockBytes, 0, blockBytes.Length);
        Console.WriteLine("Bloco enviado ao servidor: " + block.Nonce);
    }

    private void ReceiveBlock()
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

        if (dataReceived.StartsWith("ADD_BLOCK:"))
        {
            string blockData = dataReceived.Replace("ADD_BLOCK:", "");
            Block receivedBlock = Newtonsoft.Json.JsonConvert.DeserializeObject<Block>(blockData);

            // Você pode verificar se o bloco foi aceito pelo servidor aqui
            Console.WriteLine("Bloco recebido do servidor: " + receivedBlock.Nonce);
        }
    }
}

class Program
{
    static void Main()
    {
        NodeClient client = new NodeClient();
        client.Start();
    }
}
