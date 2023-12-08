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
using BlockchainServer.Model;

namespace ProjetoBlockchain
{
    public class Node1
    {
        //Conexão com banco de dados
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

            //looping para abrir threads para cada conexão
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client);

                //diferencia se é a API
                if(((IPEndPoint)client.Client.RemoteEndPoint).Address.Equals(IPAddress.Parse("10.4.6.30")))
                {
                    Console.WriteLine("API abriu uma conexão");
                }
                else
                {
                    Console.WriteLine("Novo cliente conectado: " + ((IPEndPoint)client.Client.RemoteEndPoint).Address);

                }

                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }
        public void MenuSender()
        {
            //looping dos comandos de menu
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
                    //adiciona sensor
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
                    //verifica a chain               
                    case 2:
                        Console.Clear();
                        Console.WriteLine(blockchain.IsChainValid());
                        Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    //altera o status
                    case 3:
                        Console.Clear();
                        Console.WriteLine("Por favor digite o id do sensor a ter o status alterado: ");
                        sensorid = Convert.ToInt32(Console.ReadLine());
                        Console.WriteLine("Por favor digite o novo status: ");
                        bool NewStatus = bool.Parse(Console.ReadLine());
                        Block modBlock = blockchain.ChangeSensorStatus(sensorid, NewStatus);
                        Console.WriteLine("Bloco com novo status do sensor montado!");
                        if (blockchain.IsValidBlock(modBlock))
                        {
                            blockchain.AddBlock(modBlock);
                            Console.WriteLine("Bloco aceito e adicionado com sucesso: ");
                            string blockData = Newtonsoft.Json.JsonConvert.SerializeObject(modBlock);
                            PropagateBlock(blockData);
                        }
                        Console.WriteLine("Deseja voltar para executar outros comandos? Se sim aperte qualquer tecla!");
                        Console.ReadKey();
                        Console.Clear();
                        break;
                    //localiza o status mais recente
                    case 4:
                        Console.Clear();
                        Console.Write("Digite o ID do sensor para localizar: ");
                        int SensorLocalize = Convert.ToInt32(Console.ReadLine());
                        Block latestBlock = blockchain.GetLatestBlockForSensor(SensorLocalize);
                        Console.WriteLine($"Último bloco com esse Id está com o seguinte status de alarme: {latestBlock?.MotionDetected}");
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
        //gerencia a thread da conexão
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
                        //verifica se o json recebido é para adicionar um bloco
                        if (dataReceived.StartsWith("ADD_BLOCK:"))
                        {
                            string blockData = dataReceived.Replace("ADD_BLOCK:", "");
                            Block receivedBlock = Newtonsoft.Json.JsonConvert.DeserializeObject<Block>(blockData);

                            if (blockchain.IsValidBlock(receivedBlock))
                            {
                                blockchain.AddBlock(receivedBlock);
                                Console.WriteLine("Bloco recebido por um nó cliente foi validado e adicionado com sucesso!");
                                PropagateBlock(blockData);
                            }
                        }
                        //verifica se o json recebido é para adicionar um bloco vindo da API
                        if (dataReceived.StartsWith("ADD_BLOCK_API:"))
                        {
                            string infoData = dataReceived.Replace("ADD_BLOCK_API:", "");
                            Info receivedInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Info>(infoData);
                            Block newBlock = CreateNewBlock(receivedInfo.SensorId, receivedInfo.Address);
                            if (blockchain.IsValidBlock(newBlock))
                            {
                                blockchain.AddBlock(newBlock);
                                Console.WriteLine("Bloco aceito da API e adicionado com sucesso!");
                                string blockData = Newtonsoft.Json.JsonConvert.SerializeObject(newBlock);
                                string message = "Novo sensor recebido, processado e adicionado com sucesso!";
                                PropagateBlock(blockData, message);
                            }
                        }
                        //verifica se o json recebido pela api é para alterar um bloco
                        if (dataReceived.StartsWith("CHANGE_STATUS_API:"))
                        {
                            string infoData = dataReceived.Replace("CHANGE_STATUS_API:", "");
                            Info receivedInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Info>(infoData);
                            Block modBlock = blockchain.ChangeSensorStatus(receivedInfo.SensorId, receivedInfo.MotionDetected);
                            if (blockchain.IsValidBlock(modBlock))
                            {
                                blockchain.AddBlock(modBlock);
                                Console.WriteLine("Bloco com novo status de um sensor enviado pela API foi adicionado!");
                                string blockData = Newtonsoft.Json.JsonConvert.SerializeObject(modBlock);
                                string message = "Novo status do sensor alterado com sucesso!";
                                PropagateBlock(blockData, message);
                            }
                        }
                        //verifica se o json recebido pela api é para verificar a cadeia
                        if (dataReceived.StartsWith("VERIFY_CHAIN_API:"))
                        {
                            string infoData = dataReceived.Replace("VERIFY_CHAIN_API:", "");
                            SendConfirmationMessage(client, blockchain.IsChainValid());
                        }
                        //verifica se o json recebido pela api é para verificar o status de algum sensor

                        if (dataReceived.StartsWith("VERIFY_STATUS_API:"))
                        {
                            string infoData = dataReceived.Replace("VERIFY_STATUS_API:", "");
                            Info receivedInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Info>(infoData);
                            Block latestBlock = blockchain.GetLatestBlockForSensor(receivedInfo.SensorId);
                            SendConfirmationMessage(client,$"Último bloco com esse Id está com o seguinte status de alarme: {latestBlock?.MotionDetected}");
                        }
                        //verifica se o json recebido pela api é para enviar a cadeia para o nó cliente
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
            //Trata a desconexão se é de um nó cliente ou a API
            catch (IOException)
            {
                clients.Remove(client);
                if (((IPEndPoint)client.Client.RemoteEndPoint).Address.Equals(IPAddress.Parse("10.4.6.30")))
                {
                    Console.WriteLine("API fechou a conexão");
                }
                else
                {
                    Console.WriteLine("Cliente com o ip " + ((IPEndPoint)client.Client.RemoteEndPoint).Address + " foi desconectado!");
                }
                client.Close();
            }
        }
        //Propaga o bloco para todos os nós conectados
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
        /*Propaga o bloco para todos os nós conectados, mas está utiliza somente quando é vindo de uma API 
          para evitar que a API acabe recebendo o json do bloco e só receba a mensagem de confirmação mesmo*/
        private void PropagateBlock(string blockData, string message)
        {
            lock (clientListLock)
            {
                if (clients.Count > 0)
                {
                    foreach (TcpClient client in clients)
                    {
                        //verifica se é o IP da API
                        if (((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() == "10.4.6.30")
                        {
                            SendConfirmationMessage(client, message);
                        }
                        else
                        {
                            NetworkStream stream = client.GetStream();
                            byte[] blockBytes = Encoding.UTF8.GetBytes("ADD_BLOCK:" + blockData);
                            stream.Write(blockBytes, 0, blockBytes.Length);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Não há nenhum nó cliente conectado para propagar o bloco, quando algum se conectar, ele automaticamente receberá a chain atualizada!");
                }
            }
        }
        //Aqui cria um novo bloco
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
        //Função para retornar a mensagem para a API
        private static void SendConfirmationMessage(TcpClient client, string message)
        {
            NetworkStream stream = client.GetStream();
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            stream.Write(messageBytes, 0, messageBytes.Length);
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