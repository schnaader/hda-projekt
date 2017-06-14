using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;

#if !UNITY_EDITOR
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
#endif

public class TaskResult
{
    public bool succeeded;
    public string message;

    public TaskResult(bool succeeded, string message)
    {
        this.succeeded = succeeded;
        this.message = message;
    }
}

public class CloudReceiver : MonoBehaviour
{
    //private Mesh mesh;
    private Text textInGui;
    private int readyCount = 0;

#if !UNITY_EDITOR
    private StreamSocket socket;
    private bool guiTextChanged = false;
    private System.Object guiTextLock = new System.Object();
    private String guiText;

    // Use this for initialization
    void Start()
    {

        //mesh = new Mesh();
        //GetComponent<MeshFilter>().mesh = mesh;

        textInGui = GameObject.FindObjectOfType<Text>();

        var setupClientTask = Task.Run(SetupClient);
        setupClientTask.Wait();
        TaskResult result = setupClientTask.Result;
        textInGui.text = result.message;
        if (!result.succeeded) return;

        Task.Run(StartAsyncReceiveLoop);
    }

    private void Update()
    {
        lock(guiTextLock)
        {
            if (guiTextChanged)
            {
                textInGui.text = guiText;
                guiTextChanged = false;
            }

        }
    }

    private void ChangeGuiText(string text)
    {
        lock(guiTextLock)
        {
            guiText = text;
            guiTextChanged = true;
        }

    }

    // Die Adresse des PCs, auf dem PC_Server läuft
    private string serverAdress = "172.17.25.227";

    private async Task<TaskResult> SetupClient()
    {
        try
        {
            //Create the StreamSocket and establish a connection to the echo server.
            socket = new Windows.Networking.Sockets.StreamSocket();

            //The server hostname that we will be establishing a connection to. We will be running the server and client locally,
            //so we will use localhost as the hostname.
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(serverAdress);

            //Every protocol typically has a standard port number. For example HTTP is typically 80, FTP is 20 and 21, etc.
            //For the echo server/client application we will use a random port 1337.
            string serverPort = "6670";
            await socket.ConnectAsync(serverHost, serverPort);
        }
        catch (Exception e)
        {
            return new TaskResult(false, e.Message);
        }

        return new TaskResult(true, "OK");
    }

    private async Task StartAsyncReceiveLoop ()
    {
        TaskResult result;

        do
        {
            var receiveMessageTask = Task.Run(ReceiveMessage);
            receiveMessageTask.Wait();
            result = receiveMessageTask.Result;
            if (result.succeeded)
            {
                ChangeGuiText("Message from server: <" + result.message + ">");
            }
            else
            {
                ChangeGuiText("Error: " + result.message);
            }
        } while (result.succeeded);
    }

    private async Task<TaskResult> ReceiveMessage()
    {
        string response = "no response";

        try
        {
            readyCount++;

            //Write ready to the echo server.
            Stream streamOut = socket.OutputStream.AsStreamForWrite();
            StreamWriter writer = new StreamWriter(streamOut);
            string request = "Ready #" + readyCount;
            await writer.WriteLineAsync(request);
            await writer.FlushAsync();

            //Read data from the echo server.
            Stream streamIn = socket.InputStream.AsStreamForRead();
            StreamReader reader = new StreamReader(streamIn);
            response = await reader.ReadLineAsync();
        } catch (Exception e)
        {
            return new TaskResult(false, e.Message);
        }

        return new TaskResult(true, response);
    }
#endif
}