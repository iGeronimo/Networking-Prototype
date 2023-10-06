namespace shared
{
    public class HandlePosition : ISerializable
    {
        public int uniqueID;
        public position vector3 = new position();

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(uniqueID);
            pPacket.Write(vector3);
        }
        public void Deserialize(Packet pPacket)
        {
            uniqueID = pPacket.ReadInt32();
            vector3 = pPacket.Read<position>();
        }
    }

    public class position : ISerializable
    {
        public float x;
        public float y;
        public float z;

        public void Serialize(Packet pPacket)
        {
            //throw new System.NotImplementedException();
            pPacket.Write(x);
            pPacket.Write(y);
            pPacket.Write(z);
        }

        public void Deserialize(Packet pPacket)
        {
            //throw new System.NotImplementedException();
            x = pPacket.ReadFloat();
            y = pPacket.ReadFloat();
            z = pPacket.ReadFloat();
        }

        public position()
        {
            this.x = 0;
            this.y = 0;
            this.z = 0;
        }

        public position(position newPosition)
        {
            this.x = newPosition.x;
            this.y = newPosition.y;
            this.z = newPosition.z;
        }
    }
}