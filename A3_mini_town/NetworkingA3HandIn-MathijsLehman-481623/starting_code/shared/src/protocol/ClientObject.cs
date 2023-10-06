using System;
using System.Net.Sockets;

namespace shared
{
    public class ClientObject : ISerializable
    {
        public Int16 ID;
        public Int32 SkinNumber;
        public position vector3 = new position();

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(ID);
            pPacket.Write(SkinNumber);
            pPacket.Write(vector3);
        }
        public void Deserialize(Packet pPacket) 
        {
            ID = (Int16)pPacket.ReadInt16();
            SkinNumber = (Int32)pPacket.ReadInt32();
            vector3 = pPacket.Read<position>();
        }

        public ClientObject() { }

        public ClientObject(Int16 _id, int _skinNumber, position _position)
        {
            this.ID = _id;
            this.SkinNumber = _skinNumber;
            this.vector3 = _position;
        }
    }

}
