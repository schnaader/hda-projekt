using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
using Windows.Kinect;

public class CloudReceiver : MonoBehaviour {

    private Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private byte[] _recieveBuffer = new byte[32*1024*1024];
    private Mesh mesh;
    private Text textInGui;

    private void SetupClient()
    {
        try
        {
            _clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, 6670));
        }
        catch (SocketException ex)
        {
            Debug.Log(ex.Message);
            textInGui.text = ex.Message;
        }

        _clientSocket.BeginReceive(_recieveBuffer, 0, _recieveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);

    }

    private void ReceiveCallback(IAsyncResult AR)
    {
        //Check how much bytes are recieved and call EndRecieve to finalize handshake
        int received = _clientSocket.EndReceive(AR);

        if (received <= 0)
            return;

        //Copy the recieved data into new buffer , to avoid null bytes
        byte[] recData = new byte[received];
        Buffer.BlockCopy(_recieveBuffer, 0, recData, 0, received);

        //Process data here the way you want , all your bytes will be stored in recData
        var receivedString = System.Text.Encoding.Default.GetString(recData);
        textInGui.text = "Empfangener Text: " + receivedString;

        //Start receiving again
        _clientSocket.BeginReceive(_recieveBuffer, 0, _recieveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
    }

    private ushort ReadUInt16(byte[] data, ref int index)
    {
        ushort result  = data[index];
               result += (ushort)(data[index + 1] << 8);

        index += 2;

        return result;
    }

    // Use this for initialization
    void Start() {

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        textInGui = GameObject.FindObjectOfType<Text>();

        SetupClient();
    }

    // Update is called once per frame
    void Update () {
		
	}
}
