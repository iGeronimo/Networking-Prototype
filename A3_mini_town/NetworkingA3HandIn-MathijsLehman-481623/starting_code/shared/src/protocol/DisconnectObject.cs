using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class DisconnectObject : ISerializable
    {
        public int ID;
        public void Deserialize(Packet pPacket)
        {
            //throw new NotImplementedException();
            ID = pPacket.ReadInt32();
        }

        public void Serialize(Packet pPacket)
        {
            //throw new NotImplementedException();
            pPacket.Write(ID);
        }
    }
}