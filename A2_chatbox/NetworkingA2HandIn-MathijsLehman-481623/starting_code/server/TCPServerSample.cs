using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Text;
using System.ComponentModel.Design;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using server;

public static class TCPServerSample
{
    public static Dictionary<TcpClient, ClientValues> clientDictionary = new Dictionary<TcpClient, ClientValues>();
    /**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */
    public static void Main(string[] args)
    {
        string[] commands =
        {
            "/nick",
            "/list",
            "/help",
            "/whisper",
            "/join",
            "/listrooms",
            "/listroom"
        };

        Console.WriteLine("Server started on port 55555");

        TcpListener listener = new TcpListener(IPAddress.Any, 55555);
        listener.Start();

        List<TcpClient> clients = new List<TcpClient>();

        

        List<TcpClient> miaClients = new List<TcpClient>();

        Room general = new Room("general");

        List<Room> rooms = new List<Room>
        {
            general
        };

        int i = 1;

        while (true)
        {
            //First big change with respect to example 001
            //We no longer block waiting for a client to connect, but we only block if we know
            //a client is actually waiting (in other words, we will not block)
            //In order to serve multiple clients, we add that client to a list
            while (listener.Pending())
            {
                general.members.Add(AddClient(listener, i, clientDictionary, rooms));
                i++;
            }

            //Second big change, instead of blocking on one client, 
            //we now process all clients IF they have data available
            if (!CheckForCommands(commands, clientDictionary, rooms)) CheckFaultyClients(clientDictionary, miaClients, rooms);

            CleanUpClients(miaClients, clientDictionary);
            //disconnectMessage();

            ChatSync(rooms);

            //Although technically not required, now that we are no longer blocking, 
            //it is good to cut your CPU some slack
            Thread.Sleep(100);
        }
    }

    public static byte[] ReverseMessage(NetworkStream s)
    {
        byte[] reverseStream = StreamUtil.Read(s);
        Array.Reverse(reverseStream, 0, reverseStream.Length);
        return reverseStream;
    }

    public static TcpClient AddClient(TcpListener listener, int listenerNumber, Dictionary<TcpClient, ClientValues> clientDictionary, List<Room> rooms)
    {
        TcpClient joiningClient = listener.AcceptTcpClient();
        ClientValues newClient = new ClientValues();
        newClient.roomName = "general";
        newClient.name = "guest" + listenerNumber;
        clientDictionary.Add(joiningClient, newClient);
        JoinServerMessage(listenerNumber, clientDictionary, rooms);
        Console.WriteLine("Accepted new client.");
        return joiningClient;
    }

    public static void AddClientToRoom(KeyValuePair<TcpClient, ClientValues> client, string roomName, List<Room> rooms)
    {
        GetClientRoom(client.Key, rooms).members.Remove(client.Key);
        //foreach (Room currentRoom in rooms)
        //{
        //    if (currentRoom.roomName == roomName)
        //    {
        //        currentRoom.members.Remove(client.Key);
        //    }
        //}
        foreach (Room room in rooms)
        {
            if(room.roomName == roomName)
            {
                room.members.Add(client.Key);
                client.Value.roomName = roomName;
                Write(client.Key, "You joined room: " + roomName);
                return;
            }
        }
        Room newRoom = CreateRoom(roomName, rooms);
        newRoom.members.Add(client.Key);
        client.Value.roomName = newRoom.roomName;
        Write(client.Key, "You joined room: " + roomName);
    }

    public static Room GetClientRoom(TcpClient client, List<Room> rooms)
    {
        foreach(Room room in rooms)
        {
            foreach(TcpClient roomClient in room.members)
            {
                if(roomClient == client)
                {
                    return room;
                }
            }
        }
        return null;
    }

    public static void ChatSync(List<Room> rooms)
    {
        foreach(Room room in rooms)
        {
            if (room.log.Count > 0)
            {
                foreach (TcpClient entry in room.members)
                {
                    Write(entry, room.log[0]);
                }

                room.log.Clear();
            }
        }
    }

    public static string BytesToString(byte[] message)
    {
        return Encoding.UTF8.GetString(message);
    }

    public static byte[] StringToBytes(string message)
    {
        return Encoding.UTF8.GetBytes(message);
    }

    public static byte[] AddNameToMessage(string messenger, byte[] message)
    {
        string tempMessage = BytesToString(message);
        string newMessage = messenger + ": " + tempMessage;
        byte[] newMessageBytes = StringToBytes(newMessage);
        return newMessageBytes;
    }

    public static void JoinServerMessage(int i, Dictionary<TcpClient, ClientValues> clientDictionary, List<Room> rooms)
    {
        rooms[0].log.Add(StringToBytes("guest" + i + " has joined the server"));
        ChatSync(rooms);
        rooms[0].log.Clear();
    }

    public static void CleanUpClients(List<TcpClient> miaClients, Dictionary<TcpClient, ClientValues> clientDictionary)
    {
        if (miaClients.Count > 0)
        {
            foreach (TcpClient clientKey in miaClients)
            {
                clientDictionary.Remove(clientKey);
                Console.WriteLine(clientKey + " has been removed");
            }
        }
        miaClients.Clear();
    }

    public static void Write(TcpClient messenger, string message)
    {
        Write(messenger, StringToBytes(message));
    }

    public static void Write(TcpClient messenger, byte[] message)
    {
        StreamUtil.Write(messenger.GetStream(), message);
    }

    public static void CheckFaultyClients(Dictionary<TcpClient, ClientValues> clientDictionary, List<TcpClient> miaClients, List<Room> rooms)
    {
        foreach (KeyValuePair<TcpClient, ClientValues> entry in clientDictionary)
        {
            try
            {
                Write(entry.Key, "");
            }
            catch { }

            if (!entry.Key.Connected)
            {
                miaClients.Add(entry.Key);
                GetClientRoom(entry.Key, rooms).members.Remove(entry.Key);
                entry.Key.Close();
            }
        }
    }

    public static Dictionary<TcpClient, ClientValues> GetServerDictionary()
    {
        return clientDictionary;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////              Commands                //////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static bool CheckForCommands(string[] commands, Dictionary<TcpClient, ClientValues> dict, List<Room> rooms)
    {
        foreach (KeyValuePair<TcpClient, ClientValues> dicEntry in dict)
        {
            if (dicEntry.Key.Available == 0) continue;
            string messenger = dicEntry.Value.name;
            NetworkStream stream = dicEntry.Key.GetStream();
            byte[] byteMessage = StreamUtil.Read(stream);
            string stringMessage = BytesToString(byteMessage);
            if (stringMessage.StartsWith("/"))
            {
                ExecuteCommands(stringMessage, commands, dict, dicEntry, rooms);
                return true;
            }
            else
            {
                byte[] nameMessage = AddNameToMessage(messenger, byteMessage);
                GetClientRoom(dicEntry.Key, rooms).log.Add(nameMessage);
            }
        }
        return false;
    }

    public static void ExecuteCommands(string message, string[] commands, Dictionary<TcpClient, ClientValues> dict, KeyValuePair<TcpClient, ClientValues> entry, List<Room>rooms)
    {
        string[] splitMessage = message.Split(' ');
        for (int i = 0; i < commands.Length; i++)
        {
            if (splitMessage[0] == commands[i])
            {
                switch (commands[i])
                {
                    case "/list":
                        //list people command
                        ListAllClients(dict, entry);
                        break;
                    case "/help":
                        //list all commands
                        ListAllCommands(entry, commands);
                        break;
                    case "/whisper":
                        //whisper to player
                        WhisperToUser(entry, dict, message);
                        break;
                    case "/join":
                        JoinRoom(entry, message, rooms);
                        break;
                    case "/listrooms":
                        ListAllRooms(rooms, entry);
                        break;
                    case "/listroom":
                        ListAllMembersInRoom(rooms, entry, dict);
                        break;
                    case "/nick":
                        //change nickname
                        ChangeNickname(message, dict, entry);
                        break;
                }
            }
            else
            {

            }
        }
    }
    public static bool RenameKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey oldKey, TKey newKey) // Stole this off of stackoverflow
    {
        TValue value;
        if (!dict.TryGetValue(oldKey, out value)) return false;

        dict.Remove(oldKey);
        dict[newKey] = value;
        return true;
    }

    public static void ChangeNickname(string message, Dictionary<TcpClient, ClientValues> clientDictionary, KeyValuePair<TcpClient, ClientValues> entry)
    {
        try
        {
            string lowerMessage = message.ToLower();
            string[] splitMessage = lowerMessage.Split(' ');
            string nickname = splitMessage[1];
            if (nickname == "")
            {
                Write(entry.Key, "Please enter a valid nickname");
                return;
            }
            foreach (KeyValuePair<TcpClient, ClientValues> existingNickName in clientDictionary)
            {
                if (existingNickName.Value.name == nickname)
                {
                    Write(entry.Key, "This nickname is already taken");
                    return;
                }
            }
            foreach (KeyValuePair<TcpClient, ClientValues> dictionaryEntry in clientDictionary)
            {
                if (dictionaryEntry.Value.name == entry.Value.name)
                {
                    dictionaryEntry.Value.name = nickname;
                    Write(entry.Key, "Your nickname has been changed to " + nickname);
                    return;
                }
            }
        }
        catch
        {
            Write(entry.Key, "Something has gone wrong whilst setting your new nickname");
            Write(entry.Key, "Please use this format for setting a new nickname");
            Write(entry.Key, "/nick [new nickname]");
        }
        
    }

    public static void ListAllClients(Dictionary<TcpClient, ClientValues> clientDictionary, KeyValuePair<TcpClient, ClientValues> entry)
    {
        StreamUtil.Write(entry.Key.GetStream(), StringToBytes("People in this room:"));
        foreach (KeyValuePair<TcpClient, ClientValues> existingClient in clientDictionary)
        {
            Write(entry.Key, existingClient.Value.name);
        }
    }

    public static void ListAllCommands(KeyValuePair<TcpClient, ClientValues>entry, string[] commands)
    {
        Write(entry.Key, "Available commands:");
        foreach (string command in commands)
        {
            Write(entry.Key, command);
        }
    }

    public static void WhisperToUser(KeyValuePair<TcpClient, ClientValues> entry, Dictionary<TcpClient, ClientValues> dict ,string message)
    {
        try
        {
            string[] splitMessage = message.Split(' ');
            string whisperMessage = "";
            for (int i = 2; i < splitMessage.Length; i++)
            {
                whisperMessage += splitMessage[i] + " ";
            }
            foreach (KeyValuePair<TcpClient, ClientValues> client in dict)
            {
                if (client.Value.name == splitMessage[1])
                {
                    Write(entry.Key, entry.Value.name + " whispered to " + splitMessage[1] + ": " + whisperMessage);
                    Write(client.Key, entry.Value.name + " whispered to " + splitMessage[1] + ": " + whisperMessage);
                    return;
                }
            }
            Write(entry.Key, splitMessage[1] + " is not in the room at this moment");
        }
        catch (Exception e)
        {
            Write(entry.Key, "Something has gone wrong");
            Write(entry.Key, "Please use this format when using the whisper command");
            Write(entry.Key, "/whisper [receivers username] [message to receiver]");
        }
        
    }
    public static void JoinRoom(KeyValuePair<TcpClient, ClientValues> entry, string message, List<Room> rooms)
    {
        try
        {
            string[] splitMessage = message.Split(' ');
            string roomName = "";
            for(int i = 1; i < splitMessage.Length; i++)
            {
                roomName += splitMessage[i] + " ";
            }
            AddClientToRoom(entry, roomName, rooms);
        }
        catch{}
    }

    public static void ListAllRooms(List<Room> rooms, KeyValuePair<TcpClient,ClientValues> entry)
    {
        Write(entry.Key, "Exising rooms:");
        foreach (Room room in rooms)
        {
            Write(entry.Key, room.roomName);
        }
    }

    public static void ListAllMembersInRoom(List<Room> rooms, KeyValuePair<TcpClient, ClientValues> entry, Dictionary<TcpClient,ClientValues> dict)
    {
        Room room = GetClientRoom(entry.Key, rooms);
        Write(entry.Key, "Members in " + room.roomName);
        foreach(TcpClient client in room.members)
        {
            Write(entry.Key, GetClientValues(client, dict).name);
        }
    }

    public static Room CreateRoom(string roomName, List<Room> existingRooms)
    {
        Room newRoom = new Room(roomName);
        existingRooms.Add(newRoom);
        return newRoom;
    }

    public static ClientValues GetClientValues(TcpClient pClient, Dictionary<TcpClient, ClientValues> dict)
    {
        foreach(KeyValuePair<TcpClient, ClientValues> client in dict)
        {
            if (pClient == client.Key)
            {
                return client.Value;
            }
        }
        return null;
    }
}


