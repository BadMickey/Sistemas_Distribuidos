using Banco;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;


namespace ProjetoBlockchain
{
    public class Node1
    {
        static string connectionString = "Server=localhost;Port=5433;User Id=postgres;Password=0000;database=Blockchain";
        private const int listenPort = 13000;

        private TcpListener listener;
        private Blockchain blockchain;
        private List<TcpClient> clients;
        private static object clientListLock = new object();

        public Node1()
        {
            blockchain = new Blockchain(connectionString);
            clients = new List<TcpClient>();
        }

        public void Start()
        {
            listener = new TcpListener(IPAddress.Any, listenPort);
            listener.Start();

            Console.WriteLine("Node 1 está ouvindo na porta " + listenPort);

            Thread menuSender = new Thread(MenuSender);
            menuSender.Start();


            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client);

                Console.WriteLine("Novo cliente conectado: " + ((IPEndPoint)client.Client.RemoteEndPoint).Address);

                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }
        public void MenuSender()
        {
            while (true)
            {
                Console.WriteLine("Bem-vindo a central de controle do nó servidor, você tem as seguintes ações:");
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
                        if (blockchain.IsValidBlock(newBlock))
                        {
                            blockchain.AddBlock(newBlock);
                            Console.WriteLine("Bloco aceito e adicionado com sucesso: ");
                            string blockData = Newtonsoft.Json.JsonConvert.SerializeObject(newBlock);
                            PropagateBlock(blockData);
                        }
                        Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    case 2:
                        Console.Clear();
                        Console.WriteLine(blockchain.IsChainValid());
                        Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    case 3:
                        /*Console.Clear();
                        Console.WriteLine("Por favor digite o id do sensor a ter o status alterado: ");
                        sensorid = Convert.ToInt32(Console.ReadLine());
                        Console.WriteLine("Por favor digite o novo status: ");
                        bool NewStatus = bool.Parse(Console.ReadLine());
                        //Block modBlock = blockchain.ChangeSensorStatus(sensorid, NewStatus);
                        Console.WriteLine("Bloco com novo status do sensor montado!");
                        //SendBlock(modBlock);
                        //ReceiveBlock();*/
                        Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    case 4:
                        /*Console.Clear();
                        Console.Write("Digite o ID do sensor para localizar: ");
                        int SensorLocalize = Convert.ToInt32(Console.ReadLine());
                        Block latestBlock = blockchain.GetLatestBlockForSensor(SensorLocalize);
                        Console.WriteLine($"Último bloco com esse Id está com o seguinte status de alarme: {latestBlock?.MotionDetected}");*/
                        Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    default:
                        Console.Clear();
                        break;
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    lock (clientListLock)
                    {
                        string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        if (dataReceived.StartsWith("ADD_BLOCK:"))
                        {
                            string blockData = dataReceived.Replace("ADD_BLOCK:", "");
                            Block receivedBlock = Newtonsoft.Json.JsonConvert.DeserializeObject<Block>(blockData);

                            if (blockchain.IsValidBlock(receivedBlock))
                            {
                                blockchain.AddBlock(receivedBlock);
                                Console.WriteLine("Bloco recebido por um nó cliente foi validado e adicionado com sucesso!");
                                PropagateBlock(blockData, client);

                            }

                            // Propague o bloco para outros clientes
                        }

                        if (dataReceived == "REQUEST_CHAIN")
                        {
                            List<Block> chain = blockchain.GetChain();
                            string chainData = Newtonsoft.Json.JsonConvert.SerializeObject(chain);
                            byte[] chainBytes = Encoding.UTF8.GetBytes(chainData);
                            stream.Write(chainBytes, 0, chainBytes.Length);
                        }
                    }
                }
            }
            catch (IOException)
            {
                clients.Remove(client);
                Console.WriteLine("Cliente com o ip " + ((IPEndPoint)client.Client.RemoteEndPoint).Address + " foi desconectado!");
                client.Close();
            }
        }

        private void PropagateBlock(string blockData, TcpClient senderClient)
        {
            lock (clientListLock)
            {
                if (clients.Count > 0)
                {
                    foreach (TcpClient client in clients)
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] blockBytes = Encoding.UTF8.GetBytes("ADD_BLOCK:" + blockData);
                        stream.Write(blockBytes, 0, blockBytes.Length);
                    }
                }
                else
                {
                    Console.WriteLine("Não há nenhum nó cliente conectado para propagar o bloco, quando algum se conectar, ele automaticamente receberá a chain atualizada!");
                }
            }
        }
        private void PropagateBlock(string blockData)
        {
            lock (clientListLock)
            {
                if (clients.Count > 0)
                {
                    foreach (TcpClient client in clients)
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] blockBytes = Encoding.UTF8.GetBytes("ADD_BLOCK:" + blockData);
                        stream.Write(blockBytes, 0, blockBytes.Length);
                    }
                }
                else
                {
                    Console.WriteLine("Não há nenhum nó cliente conectado para propagar o bloco, quando algum se conectar, ele automaticamente receberá a chain atualizada!");
                }
            }

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
    }

    class Program
    {
        static void Main()
        {
            Node1 server = new Node1();
            Thread serverThread = new Thread(server.Start);
            serverThread.Start();

            // Aguarde a execução do servidor
            serverThread.Join();
        }
    }
}