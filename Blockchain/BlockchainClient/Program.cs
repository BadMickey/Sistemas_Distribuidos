using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Model;
using Banco;

public class NodeClient
{
    private const string serverIPAddress = "192.168.0.217"; // Substitua pelo IP do servidor
    private const int serverPort = 13000;
    static string connectionString = "Server=localhost;Port=5433;User Id=postgres;Password=0000;database=Blockchain";


    private TcpClient client;
    static Blockchain blockchain = new Blockchain(connectionString);

    public NodeClient()
    {
        client = new TcpClient();
    }

    public void Start()
    {
        client.Connect(serverIPAddress, serverPort);
        Console.WriteLine("Conectado ao servidor em " + serverIPAddress + ":" + serverPort);

        RequestChain();


        while (true)
        {
            Console.WriteLine("Você pode escolher entre:");
            Console.WriteLine("1 - Enviar um bloco para a rede");
            Console.WriteLine("2 - Verificar se a cadeia local está válida");
            Console.WriteLine("3 - Alterar um bloco");
            int opcao = Convert.ToInt32(Console.ReadLine());

            switch (opcao)
            {
                case 1:
                    Console.Clear();
                    Console.WriteLine("Por favor digite o id do sensor: ");
                    int sensorid = Convert.ToInt32(Console.ReadLine());
                    Console.WriteLine("Por favor digite o endereço do sensor: ");
                    string address = Console.ReadLine();
                    Block newBlock = CreateNewBlock(sensorid, address);
                    SendBlock(newBlock);
                    ReceiveBlock();
                    Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                    Console.ReadKey();
                    break;
                case 2:
                    Console.Clear();
                    Console.WriteLine(blockchain.IsChainValid());
                    Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                    Console.ReadKey();
                    break;
                case 3:
                    break;
            }
        }

        // Solicita a cadeia de blocos do servidor

        // Crie e adicione um bloco ao servidor
        //Block newBlock = CreateNewBlock();
        //SendBlock(newBlock);

        // Aguarde a resposta do servidor
    }

    private void RequestChain()
    {
        NetworkStream stream = client.GetStream();
        byte[] requestChain = Encoding.UTF8.GetBytes("REQUEST_CHAIN");
        stream.Write(requestChain, 0, requestChain.Length);

        // Aguarde a resposta do servidor
        ReceiveChain();
    }

    private void ReceiveChain()
    {
        NetworkStream stream = client.GetStream();
        MemoryStream outputStream = new MemoryStream();
        string teste = stream.ToString();
        byte[] buffer = new byte[2048];
        while (stream.DataAvailable)
        {
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            outputStream.Write(buffer, 0, bytesRead);
        }
       // int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string chainData = Encoding.UTF8.GetString(outputStream.ToArray());

        List<Block> receivedChain = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Block>>(chainData);
        Console.WriteLine("Cadeia de blocos recebida do nó de servidor!");

        blockchain.InitializeBlockchain(receivedChain);
    }

    private Block CreateNewBlock(int sensorid, string address)
    {

        Block newBlock = new Block
        {
            Nonce = blockchain.GetPreviousNonce() + 1, // Substitua pelo índice correto
            Timestamp = DateTime.Now,
            SensorId = sensorid,
            Address = address,
            MotionDetected = true,
            PreviousHash = blockchain.GetPreviousHash(),
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
        byte[] blockBytes = Encoding.UTF8.GetBytes("ADD_BLOCK:" + blockData);
        stream.Write(blockBytes, 0, blockBytes.Length);
        Console.WriteLine("Bloco enviado ao servidor: " + block.Nonce);
    }

    private void ReceiveBlock()
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        if (dataReceived.StartsWith("ADD_BLOCK:"))
        {
            string blockData = dataReceived.Replace("ADD_BLOCK:", "");
            Block receivedBlock = Newtonsoft.Json.JsonConvert.DeserializeObject<Block>(blockData);

            blockchain.AddBlock(receivedBlock);
            Console.WriteLine("Bloco aceito pela blockchain e propagado!");
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
