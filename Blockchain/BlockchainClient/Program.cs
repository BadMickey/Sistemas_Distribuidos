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
        Thread receiverBlocks = new Thread(ReceiverBlocks);
        receiverBlocks.Start();

        while (true)
        {
            Console.WriteLine("Bem-vindo a central de controle do nó cliente, você tem as seguintes ações:");
            Console.WriteLine("1 - Enviar um bloco para a rede");
            Console.WriteLine("2 - Verificar se a cadeia local está válida");
            Console.WriteLine("3 - Alterar um bloco");
            Console.WriteLine("4 - Localizar o bloco mais recente de um determinado sensor");
            Console.WriteLine("Digite a opção apenas usando número, caso não faça, não irá executar nada!!");
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
                    Console.Clear();
                    Console.WriteLine("Por favor digite o id do sensor a ter o status alterado: ");
                    sensorid = Convert.ToInt32(Console.ReadLine());
                    Console.WriteLine("Por favor digite o novo status: ");
                    bool NewStatus = bool.Parse(Console.ReadLine());
                    Block modBlock = blockchain.ChangeSensorStatus(sensorid, NewStatus);
                    Console.WriteLine("Bloco com novo status do sensor montado!");
                    SendBlock(modBlock);
                    ReceiveBlock();
                    Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                    Console.ReadKey();
                    break;
                case 4:
                    Console.Clear();
                    Console.Write("Digite o ID do sensor para localizar: ");
                    int SensorLocalize = Convert.ToInt32(Console.ReadLine());
                    Block latestBlock = blockchain.GetLatestBlockForSensor(SensorLocalize);
                    Console.WriteLine($"Último bloco com esse Id está com o seguinte status de alarme: {latestBlock?.MotionDetected}");
                    Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                    Console.ReadKey();
                    break;
                default:
                    Console.Clear();
                    break;
            }
        }
    }

    private void RequestChain()
    {
        NetworkStream stream = client.GetStream();
        byte[] requestChain = Encoding.UTF8.GetBytes("REQUEST_CHAIN");
        stream.Write(requestChain, 0, requestChain.Length);

        // Aguarde a resposta do servidor
        Task.Delay(3000).Wait();
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
    private void ReceiverBlocks()
    {
        while (true)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (dataReceived.StartsWith("ADD_BLOCK:"))
                {
                    string blockData = dataReceived.Replace("ADD_BLOCK:", "");
                    Block receivedBlock = Newtonsoft.Json.JsonConvert.DeserializeObject<Block>(blockData);
                    blockchain.AddBlock(receivedBlock);
                    // Você pode verificar se o bloco foi aceito pelo servidor aqui
                    Console.WriteLine($"Bloco recebido do servidor: {receivedBlock.Nonce}");
                }
            }
            catch (IOException)
            {
                // Ocorre quando a conexão é fechada pelo servidor
                Console.WriteLine("Conexão com o servidor encerrada.");
                break;
            }
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
