using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class EmptyCheckObject : ISerializable
    {
        public void Deserialize(Packet pPacket)
        {
            //throw new NotImplementedException();
        }

        public void Serialize(Packet pPacket)
        {
            //throw new NotImplementedException();
        }
    }
}
