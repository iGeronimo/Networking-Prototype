using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class WhisperMessage : ISerializable
    {
        public string message;
        public int uniqueID;

        public void Deserialize(Packet pPacket)
        {
            //throw new NotImplementedException();
            message = pPacket.ReadString();
            uniqueID = pPacket.ReadInt32();
        }

        public void Serialize(Packet pPacket)
        {
            //throw new NotImplementedException();
            pPacket.Write(message);
            pPacket.Write(uniqueID);
        }
    }
}
