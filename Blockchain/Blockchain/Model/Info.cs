using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainServer.Model
{
    //Modelo que vai ser usado para receber e deserializar o objeto da info da API
    public class Info
    {
        public int SensorId { get; set; }
        public string Address { get; set; }
        public bool MotionDetected { get; set; }

    }
}
