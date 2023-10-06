using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class ClientObjectCollection : ISerializable
    {
        public List<ClientObject> ClientObjects = new List<ClientObject>();

        public void Deserialize(Packet pPacket)
        {
            ClientObjects = new List<ClientObject>();

            int count = pPacket.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                ClientObjects.Add(pPacket.Read<ClientObject>());
            }
        }

        public void Serialize(Packet pPacket)
        {
            int count = (ClientObjects == null ? 0 : ClientObjects.Count);

            pPacket.Write(count);

            for (int i = 0; i < count; i++)
            {
                pPacket.Write(ClientObjects[i]);
            }
        }
    }
}
