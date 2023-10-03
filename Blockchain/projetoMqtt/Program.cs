using System;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Npgsql;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Data;

class Program
{
    static MqttClient mqttClient;

    static async Task Main(string[] args)
    {
        //Aqui setamos as informações necessárias
        string connectionString = "Server=localhost;Port=5433;User Id=postgres;Password=0000;database=Blockchain";
        List<int> sensores = new List<int>();
        Random random = new Random();
        int posicao;
        //Setamos o cliente MQTT
        mqttClient = new MqttClient("localhost");
        string clientId = Guid.NewGuid().ToString();
        mqttClient.Connect(clientId);

        // Loop para enviar requisições MQTT a cada 30 segundos
        while (true)
        {
            //Aqui consultamos os sensores que está no banco de dados e montamos uma lista com os ids
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string sql = "SELECT sensorid FROM blocks";
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int sensor = reader.GetInt32(0);
                            sensores.Add(sensor);
                        }
                    }
                }
                connection.Close();
            }
            posicao = random.Next(1, sensores.Count);
            bool status = random.Next(2) == 0;
            // Construir os dados da requisição MQTT
            string dados = SerializeMessage(sensores[posicao], status);

            // Enviar a requisição MQTT para o Projeto A com o tópico "data"
            mqttClient.Publish("sensorid", System.Text.Encoding.UTF8.GetBytes(dados), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
            sensores.Clear();

            // Aguardar 30 segundos
            Thread.Sleep(TimeSpan.FromSeconds(10));
        
        }
    }
    //Aqui transformamos o objeto em um JSON
    static string SerializeMessage(int sensorid, bool status)
    {
        var messageObj = new
        {
            Sensorid = sensorid,
            Status = status
        };

        return JsonConvert.SerializeObject(messageObj);
    }
}