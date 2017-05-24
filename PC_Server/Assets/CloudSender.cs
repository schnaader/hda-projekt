using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

public class CloudSender : MonoBehaviour {
    private byte[] _recieveBuffer = new byte[32 * 1024 * 1024];

    public Socket m_socListener, m_socWorker = null;

    void Start()
    {
        StartListening();
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void StartListening()
    {
        try
        {
            //create the listening socket...
            m_socListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, 8221);
            IPEndPoint ipLocal = new IPEndPoint(IPAddress.Loopback, 6670);
            //bind to local IP Address...
            m_socListener.Bind(ipLocal);
            //start listening...
            m_socListener.Listen(4);
            // create the call back for any client connections...
            m_socListener.BeginAccept(new AsyncCallback(OnClientConnect), null);
        }
        catch (SocketException ex)
        {
            Debug.Log(ex.Message);
        }
    }

    byte[] m_DataBuffer = new byte[32*1024*1024];

    public void OnClientConnect(IAsyncResult asyn)
    {
        try
        {
            m_socWorker = m_socListener.EndAccept(asyn);
            // now start to listen for any data...
            m_socWorker.BeginReceive(m_DataBuffer, 0, m_DataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);

            // Testdaten schicken
            SendData(Encoding.ASCII.GetBytes("Hello client"));
        }
        catch (ObjectDisposedException)
        {
            Debug.Log("OnClientConnection: Socket has been closed\n");
        }
        catch (SocketException ex)
        {
            Debug.Log(ex.Message);
        }
    }

    public Socket m_socClient;

    public void ReceiveCallback(IAsyncResult asyn)
    {
        //Check how much bytes are recieved and call EndRecieve to finalize handshake
        int received = m_socWorker.EndReceive(asyn);

        if (received <= 0)
            return;

        //Copy the recieved data into new buffer , to avoid null bytes
        byte[] recData = new byte[received];
        Buffer.BlockCopy(_recieveBuffer, 0, recData, 0, received);

        //Start receiving again
        m_socWorker.BeginReceive(_recieveBuffer, 0, _recieveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
    }

    private void SendData(byte[] data)
    {
        if (m_socWorker == null) return;

        SocketAsyncEventArgs socketAsyncData = new SocketAsyncEventArgs();
        socketAsyncData.SetBuffer(data, 0, data.Length);
        m_socWorker.SendAsync(socketAsyncData);
    }
}
