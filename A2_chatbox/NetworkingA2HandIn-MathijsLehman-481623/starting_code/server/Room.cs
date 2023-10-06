using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class Room
    {
        public string roomName;
        public List<TcpClient> members;
        public List<byte[]> log = new List<byte[]>();

        public Room(string pRoomName)
        {
            this.roomName = pRoomName;
            members = new List<TcpClient>();
        }

        public void JoinRoom()
        {
            foreach(TcpClient client in members)
            {
                TCPServerSample.Write(client, TCPServerSample.GetClientValues(client, TCPServerSample.clientDictionary).name + " has joined the server");
            }
        }

        public void LeaveRoom()
        {
            foreach (TcpClient client in members)
            {
                TCPServerSample.Write(client, TCPServerSample.GetClientValues(client, TCPServerSample.clientDictionary).name + " has left the server");
            }
        }

    }
}
