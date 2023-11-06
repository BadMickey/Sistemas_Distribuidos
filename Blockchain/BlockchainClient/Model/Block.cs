using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    //Modelo do bloco
    public class Block
    {
        public int Nonce { get; set; }
        public DateTime Timestamp { get; set; }
        public int SensorId { get; set; }
        public string Address { get; set; }
        public bool MotionDetected { get; set; }
        public string PreviousHash { get; set; }
        public string Hash { get; set; }
    }
}
