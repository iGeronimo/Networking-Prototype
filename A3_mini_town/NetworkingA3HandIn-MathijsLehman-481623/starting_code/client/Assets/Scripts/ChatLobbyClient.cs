using DG.Tweening.Plugins;
using shared;
using shared.src.protocol;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEngine;

/**
 * The main ChatLobbyClient where you will have to do most of your work.
 * 
 * @author J.C. Wichman
 */
public class ChatLobbyClient : MonoBehaviour
{
    //reference to the helper class that hides all the avatar management behind a blackbox
    private AvatarAreaManager _avatarAreaManager;
    //reference to the helper class that wraps the chat interface
    private PanelWrapper _panelWrapper;

    [SerializeField] private string _server = "localhost";
    [SerializeField] private int _port = 55555;

    private List<AvatarView> _views = new List<AvatarView>();
    private TcpClient _client;

    private void Start()
    {
        connectToServer();

        //register for the important events
        _avatarAreaManager = FindObjectOfType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;

        _panelWrapper = FindObjectOfType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += onChatTextEntered;
    }

    private void connectToServer()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_server, _port);
            Debug.Log("Connected to server.");
        }
        catch (Exception e)
        {
            Debug.Log("Could not connect to server:");
            Debug.Log(e.Message);
        }
    }

    private void onAvatarAreaClicked(UnityEngine.Vector3 pClickPosition)
    {
        Debug.Log("ChatLobbyClient: you clicked on " + pClickPosition);
        //TODO pass data to the server so that the server can send a position update to all clients (if the position is valid!!)
        HandlePosition position = new HandlePosition();
        position.vector3.x = pClickPosition.x;
        position.vector3.y = pClickPosition.y;
        position.vector3.z = pClickPosition.z;
        sendObject(position);
    }

    private void onChatTextEntered(string pText)
    {
        _panelWrapper.ClearInput();
        SimpleMessage message = new SimpleMessage();
        message.text = pText;
        sendObject(message);
    }

    private void sendString(string pOutString)
    {
        try
        {
            //we are still communicating with strings at this point, this has to be replaced with either packet or object communication
            Debug.Log("Sending:" + pOutString);
            byte[] outBytes = Encoding.UTF8.GetBytes(pOutString);
            StreamUtil.Write(_client.GetStream(), outBytes);
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    private void sendObject(ISerializable pObject)
    {
        try
        {
            Debug.Log("Sending Object: " + pObject);
            Packet outPacket = new Packet();
            outPacket.Write(pObject);
            StreamUtil.Write(_client.GetStream(), outPacket.GetBytes());
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    // RECEIVING CODE

    private void Update()
    {
        try
        {
            if (_client.Available > 0)
            {
                //we are still communicating with strings at this point, this has to be replaced with either packet or object communication
                byte[] inBytes = StreamUtil.Read(_client.GetStream());
                Packet inPacket = new Packet(inBytes);
                ISerializable inObject = inPacket.ReadObject();

                if (inObject is SimpleMessage)
                {
                    Debug.Log("SimpleMessage Received");
                    showMessage(inObject as SimpleMessage);
                }
                else if (inObject is ClientObjectCollection)
                {
                    Debug.Log("ClientObjectCollection Received");
                    CreateNewAvatar(inObject as ClientObjectCollection);
                }
                else if (inObject is HandlePosition)
                {
                    Debug.Log("HandlePosition Recieived");
                    HandleReceivedPosition(inObject as HandlePosition);
                }
                else if (inObject is DisconnectObjectCollection)
                {
                    Debug.Log("DisconnectObjectCollection Received");
                    handleDisconnectObject(inObject as DisconnectObjectCollection);
                }
                else if (inObject is WhisperMessage)
                {
                    Debug.Log("WhisperMessage Received");
                    handleWhisperMessage(inObject as WhisperMessage);
                }
            }
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    private void handleDisconnectObject(DisconnectObjectCollection disconnectObjectCollection)
    {
        foreach (DisconnectObject toRemoveID in disconnectObjectCollection.DisconnectObjects)
        {
            _avatarAreaManager.RemoveAvatarView(toRemoveID.ID);
        }
    }

    private void handleWhisperMessage(WhisperMessage whisperMessage)
    {
        whisperMessage.message = "Whisper: " + whisperMessage.message;
        showMessage(whisperMessage.message, whisperMessage.uniqueID);
    }

    private void showMessage(string pText, int ID)
    {
        //This is a stub for what should actually happen
        //What should actually happen is use an ID that you got from the server, to get the correct avatar
        //and show the text message through that
        List<int> allAvatarIds = _avatarAreaManager.GetAllAvatarIds();

        if (allAvatarIds.Count == 0)
        {
            Debug.Log("No avatars available to show text through:" + pText);
            return;
        }
        try
        {
            AvatarView avatarView = _avatarAreaManager.GetAvatarView(ID);
            avatarView.Say(pText);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Debug.Log("Something went wrong with the simpleMessage object you received");
        }
    }

    private void showMessage(SimpleMessage message)
    {
        showMessage(message.text, message.messageAuthorID);
    }

    private void CreateNewAvatar(ClientObjectCollection clientCollection)
    {
        Debug.Log("Going through Collection");
        foreach (ClientObject avatar in clientCollection.ClientObjects)
        {
            Debug.Log("Checking Avatar");
            try
            {
                AvatarView testAvatarView = _avatarAreaManager.GetAvatarView(avatar.ID);
                Debug.Log("Avatar with ID " + avatar.ID + " exists, continueing to next client in ClientObjectCollection");
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log("character didn't exist so making new character");
                AvatarView newAvatar = _avatarAreaManager.AddAvatarView(avatar.ID);
                newAvatar.SetSkin(avatar.SkinNumber);
                newAvatar.transform.position = new UnityEngine.Vector3(avatar.vector3.x, avatar.vector3.y, avatar.vector3.z);
                Debug.Log("avatar with id " + avatar.ID + " has been created at position " + newAvatar.transform.position);
            }
        }
    }

    private void HandleReceivedPosition(HandlePosition newPosition)
    {
        try
        {
            Debug.Log("Attempting to move avatar " + newPosition.uniqueID + " to position " + new Vector3(newPosition.vector3.x, newPosition.vector3.y, newPosition.vector3.z));
            AvatarView avatarView = _avatarAreaManager.GetAvatarView(newPosition.uniqueID);
            avatarView.Move(new Vector3(newPosition.vector3.x, newPosition.vector3.y, newPosition.vector3.z));
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Debug.Log("Something went wrong with the receiving of a HandlePosition package");
        }
    }
}
