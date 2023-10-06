using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using shared.src.protocol;

/**
 * This class implements a simple tcp echo server.
 * Read carefully through the comments below.
 * Note that the server does not contain any sort of error handling.
 */
class TCPServerSample
{
    const float Deg2Rad = (float)Math.PI / 180f;
    public int port = 55555;
    public int currentID = 0;

    private string[] commands =
    {
        "/whisper"
    };


    public static void Main(string[] args)
    {
        TCPServerSample server = new TCPServerSample();
        server.run();
    }

    private TcpListener _listener;
    private Dictionary<TcpClient, ClientObject> _clients = new Dictionary<TcpClient, ClientObject>();

    private void run()
    {
        Console.WriteLine("Server started on port: " + port);

        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        while (true)
        {
            processFaultyClients();
            processNewClients();
            processExistingClients();

            //Although technically not required, now that we are no longer blocking, 
            //it is good to cut your CPU some slack
            Thread.Sleep(100);
        }
    }
    private void processFaultyClients()
    {
        if (_clients.Count > 0)
        {
            List<TcpClient> disconnectedClients = new List<TcpClient>();
            DisconnectObjectCollection disconnectObjectCollection = new DisconnectObjectCollection();
            foreach (KeyValuePair<TcpClient, ClientObject> client in _clients)
            {
                try
                {
                    EmptyCheckObject emptyCheckObject = new EmptyCheckObject();
                    sendObject(client.Key, emptyCheckObject, false);
                }
                catch (Exception e)
                {
                    //Delete said avatar from other places and delete the client object from the list
                    disconnectedClients.Add(client.Key);
                    DisconnectObject d = new DisconnectObject();
                    d.ID = client.Value.ID;
                    disconnectObjectCollection.DisconnectObjects.Add(d);
                }
            }
            for (int i = disconnectedClients.Count - 1; i >= 0; i--)
            {
                _clients.Remove(disconnectedClients[i]);
            }
            if (disconnectObjectCollection.DisconnectObjects.Count > 0)
            {
                foreach (KeyValuePair<TcpClient, ClientObject> client in _clients)
                {
                    sendObject(client.Key, disconnectObjectCollection);
                }
            }
        }
    }


    private void processNewClients()
    {
        while (_listener.Pending())
        {
            ClientObject newClient = new ClientObject(IDGenerator(), skinGenerator(), generateRandomPosition());
            _clients.Add(_listener.AcceptTcpClient(), newClient);
            handleNewClientJoining(newClient);
        }
    }

    private void processExistingClients()
    {
        foreach (KeyValuePair<TcpClient, ClientObject> client in _clients)
        {
            if (client.Key.Available == 0) continue;

            Console.WriteLine("Processing existing client " + client);
            //read bytes from the receiving client
            byte[] inBytes = StreamUtil.Read(client.Key.GetStream());
            //put bytes into a packet
            Packet inPacket = new Packet(inBytes);
            //put packet into object
            ISerializable inObject = inPacket.ReadObject();
            Console.WriteLine("Received: " + inObject);

            //handle based on what kind of object it is
            if (inObject is HandlePosition) { handleClickedPosition(client.Key, inObject as HandlePosition); }
            else if (inObject is SimpleMessage) { handleSendMessage(client.Value, inObject as SimpleMessage); }
        }
    }

    private void handleClickedPosition(TcpClient pClient, HandlePosition pPosition)
    {
        //Construct a reply with a check of position gotten is valid
        HandlePosition newPosition = new HandlePosition();
        newPosition.vector3 = pPosition.vector3;
        foreach (KeyValuePair<TcpClient, ClientObject> client in _clients)
        {
            if (client.Key == pClient)
            {
                newPosition.uniqueID = client.Value.ID;
                client.Value.vector3 = pPosition.vector3;
            }
        }
        if (validPosition(newPosition))
        {
            foreach (KeyValuePair<TcpClient, ClientObject> client in _clients)
            {
                sendObject(client.Key, newPosition);
            }
        }
    }

    private void handleSendMessage(ClientObject pClient, SimpleMessage message)
    {
        message.messageAuthorID = pClient.ID;
        if (message.text.StartsWith("/"))
        {
            handleCommand(pClient, message.text);
        }
        else
        {
            foreach (KeyValuePair<TcpClient, ClientObject> client in _clients)
            {
                sendObject(client.Key, message);
            }
        }
    }

    private void handleCommand(ClientObject pClient, string pCommand)
    {
        string[] splitMessage = pCommand.Split(' ');
        for (int i = 0; i < commands.Length; i++)
        {
            if (splitMessage[0] == commands[i])
            {
                switch (commands[i])
                {
                    case "/whisper":
                        handleWhisperMessage(pClient, pCommand);
                        break;
                }
            }
        }
    }

    private void handleNewClientJoining(ClientObject pClient)
    {
        ClientObjectCollection clientObjectCollection = new ClientObjectCollection();
        foreach (KeyValuePair<TcpClient, ClientObject> client in _clients)
        {
            Console.WriteLine("Adding clientObject to ClientObjectCollection");
            clientObjectCollection.ClientObjects.Add(client.Value);
        }
        foreach (KeyValuePair<TcpClient, ClientObject> client in _clients)
        {
            string clients = "";
            foreach (ClientObject addedClient in clientObjectCollection.ClientObjects)
            {
                clients += "client " + addedClient.ID + " ";
            }
            Console.WriteLine("Sending " + clients + "to " + client.Value.ID);
            sendObject(client.Key, clientObjectCollection);
        }
        Console.WriteLine("Finished sending newly joined client to other clients");
    }

    private void handleWhisperMessage(ClientObject pClient, string pWhisperMessage)
    {
        WhisperMessage whisperMessage = new WhisperMessage();
        whisperMessage.uniqueID = pClient.ID;
        string[] pWhisperMessageSplit = pWhisperMessage.Split(' ');
        string whisperText = "";
        try
        {
            for (int i = 1; i < pWhisperMessageSplit.Length; i++)
            {
                whisperText += pWhisperMessageSplit[i] + " ";
            }
            whisperMessage.message = whisperText;
            foreach (KeyValuePair<TcpClient, ClientObject> client in _clients)
            {
                if (checkDistanceBetweenPosition(pClient.vector3, client.Value.vector3, 2f))
                {
                    sendObject(client.Key, whisperMessage);
                }
            }
        }
        catch { }
    }

    private void sendObject(TcpClient pClient, ISerializable pOutObject, bool writeInConsole = true)
    {
        if (writeInConsole) Console.WriteLine("Sending object: " + pOutObject);
        Packet outPacket = new Packet();
        outPacket.Write(pOutObject);
        StreamUtil.Write(pClient.GetStream(), outPacket.GetBytes());
    }

    private Int16 IDGenerator()
    {
        currentID++;
        return (Int16)currentID;
    }

    private int skinGenerator()
    {
        Random rand = new Random();
        int skinNumber = rand.Next(0, 1000);
        return skinNumber;
    }

    private bool validPosition(HandlePosition position)
    {
        if (position.vector3.x > 10) return false;
        if (position.vector3.x < -10) return false;
        if (position.vector3.y != 0) return false;
        if (position.vector3.z > 10) return false;
        if (position.vector3.z < -10) return false;
        return true;
    }

    private position generateRandomPosition()
    {
        Random random = new Random();
        position randomPosition = new position();

        float randomAngle = (random.Next(0, 18000) / 100) * Deg2Rad;
        float randomDistance = (float)(random.Next(0, 1000) / 100);

        randomPosition.x = (float)Math.Cos(randomAngle);
        randomPosition.y = 0;
        randomPosition.z = (float)Math.Sin(randomAngle) * randomDistance;

        Console.WriteLine("Randomly generated angle: " + randomAngle);
        Console.WriteLine("Randomly generated distance: " + randomDistance);
        Console.WriteLine("Randomly generated position: " + randomPosition.x + " " + randomPosition.y + " " + randomPosition.z);

        return randomPosition;
    }

    private bool checkDistanceBetweenPosition(position a, position b, float maxDistance)
    {
        if (a == b) return true;
        float distance = (float)Math.Sqrt((float)Math.Pow(a.x - b.x, 2) + (float)Math.Pow(a.y - b.y, 2) + (float)Math.Pow(a.z - b.z, 2));
        return distance <= maxDistance;
    }
}