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
        private List<Block> chain2 = new List<Block>();
        private string connectionString;

        public Blockchain(string connectionString)
        {
            this.connectionString = connectionString;
        }
        //Atualiza o BD físico do nó cliente
        public void InitializeBlockchain(List<Block> receivedChain)
        {
            foreach (Block block in receivedChain)
            {
                chain.Add(block);
            }

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                Boolean vazio = false;
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
                                Block receivedBlock = new Block
                                {
                                    Nonce = reader.GetInt32(0),
                                    Timestamp = reader.GetDateTime(1),
                                    SensorId = reader.GetInt32(2),
                                    Address = reader.GetString(3),
                                    MotionDetected = reader.GetBoolean(4),
                                    PreviousHash = reader.GetString(5),
                                    Hash = reader.GetString(6)
                                };
                                chain2.Add(receivedBlock);
                            }
                        }
                        else
                        {
                            vazio = true;
                        }
                    }
                }
                // Verifica se a chain física está com algum bloco errado em relação a chain recebida do servidor
                var errorBlocks = chain2
            .Where(blocoAtrasado => !chain
                .Any(blocoAtualizado =>
                    blocoAtualizado.PreviousHash == blocoAtrasado.PreviousHash &&
                    blocoAtualizado.Hash == blocoAtrasado.Hash))
            .ToList();

                // Se o BD físico estiver vázio, apenas adiciona a chain recebida no BD
                if (vazio)
                {
                    foreach (Block block in receivedChain)
                    {
                        string query1 = "INSERT INTO Blocks (Nonce, Timestamp, SensorId, Address, MotionDetected, PreviousHash, Hash) VALUES (@Nonce, @Timestamp, @SensorId, @Address, @MotionDetected, @PreviousHash, @Hash)";
                        using (NpgsqlCommand command4 = new NpgsqlCommand(query1, connection))
                        {
                            command4.Parameters.AddWithValue("@Nonce", block.Nonce);
                            command4.Parameters.AddWithValue("@Timestamp", block.Timestamp);
                            command4.Parameters.AddWithValue("@SensorId", block.SensorId);
                            command4.Parameters.AddWithValue("@Address", block.Address);
                            command4.Parameters.AddWithValue("@MotionDetected", block.MotionDetected);
                            command4.Parameters.AddWithValue("@PreviousHash", block.PreviousHash);
                            command4.Parameters.AddWithValue("@Hash", block.Hash);

                            command4.ExecuteNonQuery();
                        }
                    }
                }

                // Se existir um bloco errado, o BD é limpo e recebe a chain atual
                else if (errorBlocks.Any())
                {
                    using (NpgsqlCommand command1 = new NpgsqlCommand($"TRUNCATE TABLE blocks", connection))
                    {
                        command1.ExecuteNonQuery();
                    }

                    foreach (Block block in chain)
                    {
                        string query2 = "INSERT INTO Blocks (Nonce, Timestamp, SensorId, Address, MotionDetected, PreviousHash, Hash) VALUES (@Nonce, @Timestamp, @SensorId, @Address, @MotionDetected, @PreviousHash, @Hash)";
                        using (NpgsqlCommand command2 = new NpgsqlCommand(query2, connection))
                        {
                            command2.Parameters.AddWithValue("@Nonce", block.Nonce);
                            command2.Parameters.AddWithValue("@Timestamp", block.Timestamp);
                            command2.Parameters.AddWithValue("@SensorId", block.SensorId);
                            command2.Parameters.AddWithValue("@Address", block.Address);
                            command2.Parameters.AddWithValue("@MotionDetected", block.MotionDetected);
                            command2.Parameters.AddWithValue("@PreviousHash", block.PreviousHash);
                            command2.Parameters.AddWithValue("@Hash", block.Hash);

                            command2.ExecuteNonQuery();
                        }
                    }
                }
                // Se não tiver blocos errados, apenas calcula os blocos faltantes no BD físico e adiciona, basicamente só atualiza o BD físico
                else
                {
                    List<Block> attChain = chain
            .Where(blocoAtualizado => !chain2
                .Any(blocoAtrasado =>
                    blocoAtrasado.Hash == blocoAtualizado.Hash))
            .ToList();

                    foreach (Block block in attChain)
                    {
                        string query2 = "INSERT INTO Blocks (Nonce, Timestamp, SensorId, Address, MotionDetected, PreviousHash, Hash) VALUES (@Nonce, @Timestamp, @SensorId, @Address, @MotionDetected, @PreviousHash, @Hash)";
                        using (NpgsqlCommand command3 = new NpgsqlCommand(query2, connection))
                        {
                            command3.Parameters.AddWithValue("@Nonce", block.Nonce);
                            command3.Parameters.AddWithValue("@Timestamp", block.Timestamp);
                            command3.Parameters.AddWithValue("@SensorId", block.SensorId);
                            command3.Parameters.AddWithValue("@Address", block.Address);
                            command3.Parameters.AddWithValue("@MotionDetected", block.MotionDetected);
                            command3.Parameters.AddWithValue("@PreviousHash", block.PreviousHash);
                            command3.Parameters.AddWithValue("@Hash", block.Hash);

                            command3.ExecuteNonQuery();
                        }
                    }
                }


                connection.Close();
            }
        }
        //Adiciona o bloco na blockchain e BD
        public void AddBlock(Block Sendedblock)
        {
            
            chain.Add(Sendedblock);

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO Blocks (Nonce, Timestamp, SensorId, Address, MotionDetected, PreviousHash, Hash) VALUES (@Nonce, @Timestamp, @SensorId, @Address, @MotionDetected, @PreviousHash, @Hash)";
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Nonce", Sendedblock.Nonce);
                    command.Parameters.AddWithValue("@Timestamp", Sendedblock.Timestamp);
                    command.Parameters.AddWithValue("@SensorId", Sendedblock.SensorId);
                    command.Parameters.AddWithValue("@Address", Sendedblock.Address);
                    command.Parameters.AddWithValue("@MotionDetected", Sendedblock.MotionDetected);
                    command.Parameters.AddWithValue("@PreviousHash", Sendedblock.PreviousHash);
                    command.Parameters.AddWithValue("@Hash", Sendedblock.Hash);

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
        //Valida se o bloco é valido
        public bool IsValidBlock(Block block)
        {
            if (block.Nonce > 0)
            {
                // Validate o hash do bloco
                string expectedHash = CalculateHash(block);
                if (block.Hash != expectedHash)
                {
                    return false;
                }

                // Certifica se o hash anterior do bloco atual é igual ao hash do bloco anterior
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

                if (IsValidBlock(currentBlock) == false)
                {
                    return "A blockchain está com algum bloco inválido!";
                }
            }

            return "A blockchain está válida!";
        }
        //Altera o status do sensor
        public Block ChangeSensorStatus(int sensorId, bool newStatus)
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

            return newBlock;
        }
        //Busca o bloco mais recente que possui um determinado ID de sensor
        public Block GetLatestBlockForSensor(int sensorId)
        {
            return chain.LastOrDefault(b => b.SensorId == sensorId);
        }
        // Pega o hash anterior
        public string GetPreviousHash()
        {
            return chain.Last().Hash;
        }
        // Pega o Nonce anterior
        public int GetPreviousNonce()
        {
            return chain.Last().Nonce;
        }
    }

}
