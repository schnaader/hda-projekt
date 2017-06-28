using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using System.Text;

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

#if !UNITY_EDITOR
    private Mesh _Mesh;
    private Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Triangles;

    private Text textInGui;
    private int readyCount = 0;

    private StreamSocket socket;
    private bool guiTextChanged = false;
    private System.Object guiTextLock = new System.Object();
    private String guiText;

    private bool meshChanged = false;
    private System.Object meshChangeLock = new System.Object();

    private Material colorViewMaterial;
    private Material meshMaterial;
    private Texture2D colorTexture;
    private byte[] jpgBuf;

    const int depthWidth = 256, depthHeight = 212;

    // Use this for initialization
    void Start()
    {
        CreateMesh(depthWidth, depthHeight);
        colorTexture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);

        textInGui = GameObject.FindObjectOfType<Text>();

        colorViewMaterial = GameObject.Find("ColorView").GetComponent<Renderer>().material;
        meshMaterial = GameObject.Find("Mesh").GetComponent<Renderer>().material;
        if ((colorViewMaterial == null) || (meshMaterial == null))
        {
            textInGui.text = "Could not find ColorView, Mesh, renderer or material";
            return;
        }

        var setupClientTask = Task.Run(SetupClient);
        setupClientTask.Wait();
        TaskResult result = setupClientTask.Result;
        textInGui.text = result.message;
        if (!result.succeeded) return;

        Task.Run(StartAsyncReceiveLoop);
    }

    void CreateMesh(int width, int height)
    {
        _Mesh = new Mesh();
        GetComponentInChildren<MeshFilter>().mesh = _Mesh;

        _Vertices = new Vector3[width * height];
        _UV = new Vector2[width * height];
        _Triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                _Vertices[index] = new Vector3(x, -y, 0);
                _UV[index] = new Vector2(((float)x / (float)width), ((float)y / (float)height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    _Triangles[triangleIndex++] = topLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomRight;
                }
            }
        }

        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
    }

    private void Update()
    {
        lock (guiTextLock)
        {
            if (guiTextChanged)
            {
                textInGui.text = guiText;
                guiTextChanged = false;
            }
        }

        lock (meshChangeLock) {
            if (meshChanged)
            {
                _Mesh.vertices = _Vertices;
                _Mesh.uv = _UV;
                _Mesh.triangles = _Triangles;
                _Mesh.RecalculateNormals();

                colorTexture.LoadImage(jpgBuf);
                colorViewMaterial.mainTexture = colorTexture;
                meshMaterial.mainTexture = colorTexture;
                colorViewMaterial.mainTextureScale = new Vector2(1, -1);
                meshMaterial.mainTextureScale = new Vector2(1, -1);

                meshChanged = false;
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
    private string serverAdress = "172.17.5.235";
    //private string serverAdress = "127.0.0.1";

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
        int byteCount = 0, jpgLength = 0;
        int width = 0, height = 0;

        try
        {
            readyCount++;

            // Send ready to the server
            var streamOut = socket.OutputStream.AsStreamForWrite(0);
            StreamWriter writer = new StreamWriter(streamOut);
            string request = "Ready #" + readyCount;
            await writer.WriteLineAsync(request);
            await writer.FlushAsync();
            await streamOut.FlushAsync();

            // Read data from the server
            var streamIn = socket.InputStream.AsStreamForRead(0);
            {
                using (BinaryReader reader = new BinaryReader(streamIn, Encoding.Unicode, true))
                {
                    byte[] buf = reader.ReadBytes(sizeof(Int32));
                    width = BitConverter.ToInt32(buf, 0);
                    buf = reader.ReadBytes(sizeof(Int32));
                    height = BitConverter.ToInt32(buf, 0);

                    byteCount = 5 * sizeof(float) * width * height;
                    buf = reader.ReadBytes(byteCount);

                    lock (meshChangeLock)
                    {
                        int bufIndex = 0;
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int index = y * width + x;

                                _Vertices[index].x = BitConverter.ToSingle(buf, bufIndex);
                                bufIndex += sizeof(float);
                                _Vertices[index].y = BitConverter.ToSingle(buf, bufIndex);
                                bufIndex += sizeof(float);
                                _Vertices[index].z = BitConverter.ToSingle(buf, bufIndex);
                                bufIndex += sizeof(float);
                                _UV[index].x = BitConverter.ToSingle(buf, bufIndex);
                                bufIndex += sizeof(float);
                                _UV[index].y = BitConverter.ToSingle(buf, bufIndex);
                                bufIndex += sizeof(float);
                            }
                        }
                    }

                    buf = reader.ReadBytes(sizeof(Int32));
                    jpgLength = BitConverter.ToInt32(buf, 0);
                    jpgBuf = reader.ReadBytes(jpgLength);

                    meshChanged = true;
                }

                streamIn.Flush();
            }

            response = String.Format("{0} Bytes wurden empfangen", byteCount + jpgLength);
        }
        catch (Exception e)
        {
            return new TaskResult(false, e.Message);
        }

        return new TaskResult(true, response);
    }
#endif
}