using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;

#if !UNITY_EDITOR
using Windows.Networking.Sockets;
#endif

public class CloudReceiver : MonoBehaviour
{
    private Mesh mesh;
    private Text textInGui;

    // Use this for initialization
    void Start()
    {

        //mesh = new Mesh();
        //GetComponent<MeshFilter>().mesh = mesh;

#if !UNITY_EDITOR
        textInGui = GameObject.FindObjectOfType<Text>();

        SetupClient();
#endif
    }

#if !UNITY_EDITOR
    // Die Adresse des PCs, auf dem PC_Server läuft
    private string serverAdress = "172.17.20.64";

    private async void SetupClient()
    {
        try
        {
            //Create the StreamSocket and establish a connection to the echo server.
            Windows.Networking.Sockets.StreamSocket socket = new Windows.Networking.Sockets.StreamSocket();

            //The server hostname that we will be establishing a connection to. We will be running the server and client locally,
            //so we will use localhost as the hostname.
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(serverAdress);

            //Every protocol typically has a standard port number. For example HTTP is typically 80, FTP is 20 and 21, etc.
            //For the echo server/client application we will use a random port 1337.
            string serverPort = "6670";
            await socket.ConnectAsync(serverHost, serverPort);

            //Write data to the echo server.
            Stream streamOut = socket.OutputStream.AsStreamForWrite();
            StreamWriter writer = new StreamWriter(streamOut);
            string request = "test";
            await writer.WriteLineAsync(request);
            await writer.FlushAsync();

            //Read data from the echo server.
            Stream streamIn = socket.InputStream.AsStreamForRead();
            StreamReader reader = new StreamReader(streamIn);
            string response = await reader.ReadLineAsync();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            textInGui.text = e.Message;
        }
    }
#endif
}