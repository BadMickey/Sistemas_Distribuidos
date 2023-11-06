using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Net.Sockets;

namespace Banco
{
    public class Blockchain
    {
        private List<Block> chain = new List<Block>();
        private string connectionString;

        public Blockchain(string connectionString)
        {
            this.connectionString = connectionString;
            InitializeBlockchain();
        }
        //Inicializa a blockchain
        private void InitializeBlockchain()
        {
            Boolean genesis = false;
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM blocks ORDER BY Nonce ASC";
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        //Carrega a blockchain do BD para a memória
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Block genesisBlock = new Block
                                {
                                    Nonce = reader.GetInt32(0),
                                    Timestamp = reader.GetDateTime(1),
                                    SensorId = reader.GetInt32(2),
                                    Address = reader.GetString(3),
                                    MotionDetected = reader.GetBoolean(4),
                                    PreviousHash = reader.GetString(5),
                                    Hash = reader.GetString(6)
                                };
                                chain.Add(genesisBlock);
                            }
                        }
                        else
                        {
                            //Cria o bloco gênese
                            Block genesisBlock = new Block
                            {
                                Nonce = 0,
                                Timestamp = DateTime.Now,
                                SensorId = 0,
                                Address = "Bloco gênese",
                                MotionDetected = false,
                                PreviousHash = string.Empty,
                                Hash = string.Empty
                            };
                            genesisBlock.Hash = CalculateHash(genesisBlock);
                            chain.Add(genesisBlock);
                            genesis = true;
                        }
                    }
                }
                //Insere o bloco gênese no BD
                if (genesis == true)
                {
                    string query1 = "INSERT INTO Blocks (Nonce, Timestamp, SensorId, Address, MotionDetected, PreviousHash, Hash) VALUES (@Nonce, @Timestamp, @SensorId, @Address, @MotionDetected, @PreviousHash, @Hash)";
                    using (NpgsqlCommand command = new NpgsqlCommand(query1, connection))
                    {
                        command.Parameters.AddWithValue("@Nonce", chain[0].Nonce);
                        command.Parameters.AddWithValue("@Timestamp", chain[0].Timestamp);
                        command.Parameters.AddWithValue("@SensorId", chain[0].SensorId);
                        command.Parameters.AddWithValue("@Address", chain[0].Address);
                        command.Parameters.AddWithValue("@MotionDetected", chain[0].MotionDetected);
                        command.Parameters.AddWithValue("@PreviousHash", chain[0].PreviousHash);
                        command.Parameters.AddWithValue("@Hash", chain[0].Hash);

                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        //Adiciona o bloco na blockchain e BD
        public void AddBlock(int sensorId, string address, bool motionDetected)
        {
            Block previousBlock = chain[chain.Count - 1];
            int newNonce = previousBlock.Nonce + 1;
            DateTime newTimestamp = DateTime.Now;

            Block newBlock = new Block
            {
                Nonce = newNonce,
                Timestamp = newTimestamp,
                SensorId = sensorId,
                Address = address,
                MotionDetected = motionDetected,
                PreviousHash = previousBlock.Hash,
                Hash = string.Empty
            };


            string newHash = CalculateHash(newBlock);

            newBlock.Hash = newHash;
            chain.Add(newBlock);

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Blocks (Nonce, Timestamp, SensorId, Address, MotionDetected, PreviousHash, Hash) VALUES (@Nonce, @Timestamp, @SensorId, @Address, @MotionDetected, @PreviousHash, @Hash)";
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nonce", newNonce);
                    command.Parameters.AddWithValue("@Timestamp", newTimestamp);
                    command.Parameters.AddWithValue("@SensorId", sensorId);
                    command.Parameters.AddWithValue("@Address", address);
                    command.Parameters.AddWithValue("@MotionDetected", motionDetected);
                    command.Parameters.AddWithValue("@PreviousHash", previousBlock.Hash);
                    command.Parameters.AddWithValue("@Hash", newHash);

                    command.ExecuteNonQuery();
                }
            }
        }
        //Calcula o hash
        public string CalculateHash(Block block)
        {
            string input = $"{block.Nonce}-{block.Timestamp}-{block.SensorId}-{block.Address}-{block.MotionDetected}-{block.PreviousHash}";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
        public bool IsValidBlock(Block block)
        {
            if (block.Nonce > 0)
            {
                // Validate the block's hash
                string expectedHash = CalculateHash(block);
                if (block.Hash != expectedHash)
                {
                    return false;
                }

                // Ensure the previous hash matches the last block's hash
                Block previousBlock = chain[block.Nonce - 1];
                if (block.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }
            }
            return true;
        }
        //Verifica se a blockchain está válida
        public string IsChainValid()
        {
            for (int i = 1; i < chain.Count; i++)
            {
                Block currentBlock = chain[i];

                if (IsValidBlock(currentBlock))
                {
                    return "A blockchain está com algum bloco inválido!";
                }
            }

            return "A blockchain está válida!";
        }
        //Altera o status do sensor
        public void ChangeSensorStatus(int sensorId, bool newStatus)
        {
            Block OldBlock = chain.LastOrDefault(b => b.SensorId == sensorId);
            Block previousBlock = chain[chain.Count - 1];
            int newIndex = previousBlock.Nonce + 1;
            DateTime newTimestamp = DateTime.Now;
            string address = OldBlock.Address;

            Block newBlock = new Block
            {
                Nonce = newIndex,
                Timestamp = newTimestamp,
                SensorId = sensorId,
                Address = address,
                MotionDetected = newStatus,
                PreviousHash = previousBlock.Hash,
                Hash = string.Empty
            };

            string newHash = CalculateHash(newBlock);

            newBlock.Hash = newHash;
            chain.Add(newBlock);

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Blocks (Nonce, Timestamp, SensorId, Address, MotionDetected, PreviousHash, Hash) VALUES (@Nonce, @Timestamp, @SensorId, @Address, @MotionDetected, @PreviousHash, @Hash)";
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nonce", newIndex);
                    command.Parameters.AddWithValue("@Timestamp", newTimestamp);
                    command.Parameters.AddWithValue("@SensorId", sensorId);
                    command.Parameters.AddWithValue("@Address", address);
                    command.Parameters.AddWithValue("@MotionDetected", newStatus);
                    command.Parameters.AddWithValue("@PreviousHash", previousBlock.Hash);
                    command.Parameters.AddWithValue("@Hash", newHash);

                    command.ExecuteNonQuery();
                }
            }
        }
        //Busca o bloco mais recente que possui um determinado ID de sensor
        public Block GetLatestBlockForSensor(int sensorId)
        {
            return chain.LastOrDefault(b => b.SensorId == sensorId);
        }
    }

}
