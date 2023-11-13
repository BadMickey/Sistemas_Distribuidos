using Banco;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoBlockchain
{
    public class Node1
    {
        static string connectionString = "Server=localhost;Port=5433;User Id=postgres;Password=0000;database=Blockchain";
        private const int listenPort = 13000;

        private TcpListener listener;
        private Blockchain blockchain;
        private List<TcpClient> clients;

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

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                clients.Add(client);

                Console.WriteLine("Novo cliente conectado: " + ((IPEndPoint)client.Client.RemoteEndPoint).Address);

                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

           while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (dataReceived.StartsWith("ADD_BLOCK:"))
                {
                    string blockData = dataReceived.Replace("ADD_BLOCK:", "");
                    Block receivedBlock = Newtonsoft.Json.JsonConvert.DeserializeObject<Block>(blockData);

                    if (blockchain.IsValidBlock(receivedBlock))
                    {
                        blockchain.AddBlock(receivedBlock);
                        Console.WriteLine("Bloco adicionado com sucesso: " + receivedBlock.Nonce);
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

            clients.Remove(client);
            client.Close();
        }

        private void PropagateBlock(string blockData, TcpClient senderClient)
        {
            foreach (TcpClient client in clients)
            {
                if (client == senderClient)
                {
                    NetworkStream stream = client.GetStream();
                    byte[] blockBytes = Encoding.UTF8.GetBytes("ADD_BLOCK:" + blockData);
                    stream.Write(blockBytes, 0, blockBytes.Length);
                }
            }
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