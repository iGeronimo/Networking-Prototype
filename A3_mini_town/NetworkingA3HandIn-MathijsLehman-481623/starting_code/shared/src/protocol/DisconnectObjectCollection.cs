using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared.src.protocol
{
    public class DisconnectObjectCollection : ISerializable
    {
        public List<DisconnectObject> DisconnectObjects = new List<DisconnectObject>();

        public void Deserialize(Packet pPacket)
        {
            DisconnectObjects = new List<DisconnectObject>();

            int count = pPacket.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                DisconnectObjects.Add(pPacket.Read<DisconnectObject>());
            }
        }

        public void Serialize(Packet pPacket)
        {
            int count = (DisconnectObjects == null ? 0 : DisconnectObjects.Count);

            pPacket.Write(count);

            for (int i = 0; i < count; i++)
            {
                pPacket.Write(DisconnectObjects[i]);
            }
        }
    }
}
