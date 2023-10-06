namespace shared
{
    public class SimpleMessage : ISerializable
    {
        public string text;
        public int messageAuthorID;

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(text);
            pPacket.Write(messageAuthorID);
        }

        public void Deserialize(Packet pPacket)
        {
            text = pPacket.ReadString();
            messageAuthorID = pPacket.ReadInt32();
        }
    }
}
