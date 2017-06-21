using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using Windows.Kinect;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public enum DepthViewMode
{
    SeparateSourceReaders,
    MultiSourceReader,
}

public class DepthSourceView : MonoBehaviour
{
    public DepthViewMode ViewMode = DepthViewMode.SeparateSourceReaders;
    
    public GameObject ColorSourceManager;
    public GameObject DepthSourceManager;
    public GameObject MultiSourceManager;
    
    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;
    private Mesh _Mesh;
    private Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Triangles;
    
    private const int _DownsampleSize = 2;
    private const double _DepthScale = 0.1f;
    private const int _Speed = 50;
    
    private MultiSourceManager _MultiManager;
    private ColorSourceManager _ColorManager;
    private DepthSourceManager _DepthManager;

    private byte[] _recieveBuffer = new byte[32 * 1024 * 1024];
    private Text textInGui;
    private int messageCount = 0;
    private System.Object readyToSendLock = new System.Object();
    private bool readyToSendMesh = false;

    public Socket m_socListener, m_socWorker = null;

    void Start()
    {
        textInGui = GameObject.FindObjectOfType<Text>();

        StartListening();

        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;
            var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

            // Downsample to lower resolution
            CreateMesh(frameDesc.Width / _DownsampleSize, frameDesc.Height / _DownsampleSize);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }

    void CreateMesh(int width, int height)
    {
        _Mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _Mesh;

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
    
    void OnGUI()
    {
        GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
        GUI.TextField(new Rect(Screen.width - 250 , 10, 250, 20), "DepthMode: " + ViewMode.ToString());
        GUI.EndGroup();
    }

    public void StartListening()
    {
        try
        {
            //create the listening socket...
            m_socListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, 6670);
            //IPEndPoint ipLocal = new IPEndPoint(IPAddress.Loopback, 6670);
            //bind to local IP Address...
            m_socListener.Bind(ipLocal);
            //start listening...
            m_socListener.Listen(4);
            // create the call back for any client connections...
            m_socListener.BeginAccept(new AsyncCallback(OnClientConnect), null);
            textInGui.text = String.Format("Listening on {0}...", ipLocal);
        }
        catch (SocketException ex)
        {
            Debug.Log(ex.Message);
            textInGui.text = ex.Message;
        }
    }

    void Update()
    {
        if (_Sensor == null)
        {
            return;
        }
        
        if (Input.GetButtonDown("Fire1"))
        {
            if(ViewMode == DepthViewMode.MultiSourceReader)
            {
                ViewMode = DepthViewMode.SeparateSourceReaders;
            }
            else
            {
                ViewMode = DepthViewMode.MultiSourceReader;
            }
        }
        
        float yVal = Input.GetAxis("Horizontal");
        float xVal = -Input.GetAxis("Vertical");

        transform.Rotate(
            (xVal * Time.deltaTime * _Speed), 
            (yVal * Time.deltaTime * _Speed), 
            0, 
            Space.Self);
            
        if (ViewMode == DepthViewMode.SeparateSourceReaders)
        {
            if (ColorSourceManager == null)
            {
                return;
            }
            
            _ColorManager = ColorSourceManager.GetComponent<ColorSourceManager>();
            if (_ColorManager == null)
            {
                return;
            }
            
            if (DepthSourceManager == null)
            {
                return;
            }
            
            _DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();
            if (_DepthManager == null)
            {
                return;
            }
            
            gameObject.GetComponent<Renderer>().material.mainTexture = _ColorManager.GetColorTexture();
            RefreshData(_DepthManager.GetData(),
                _ColorManager.ColorWidth,
                _ColorManager.ColorHeight);
        }
        else
        {
            if (MultiSourceManager == null)
            {
                return;
            }
            
            _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();
            if (_MultiManager == null)
            {
                return;
            }
            
            gameObject.GetComponent<Renderer>().material.mainTexture = _MultiManager.GetColorTexture();
            
            RefreshData(_MultiManager.GetDepthData(),
                        _MultiManager.ColorWidth,
                        _MultiManager.ColorHeight);
        }
    }
    
    private void RefreshData(ushort[] depthData, int colorWidth, int colorHeight)
    {
        var frameDesc = _Sensor.DepthFrameSource.FrameDescription;
        
        ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];
        CameraSpacePoint[] cameraSpace = new CameraSpacePoint[depthData.Length];
        _Mapper.MapDepthFrameToColorSpace(depthData, colorSpace);
        _Mapper.MapDepthFrameToCameraSpace(depthData, cameraSpace);

        for (int y = 0; y < frameDesc.Height; y += _DownsampleSize)
        {
            for (int x = 0; x < frameDesc.Width; x += _DownsampleSize)
            {
                int indexX = x / _DownsampleSize;
                int indexY = y / _DownsampleSize;
                int smallIndex = (indexY * (frameDesc.Width / _DownsampleSize)) + indexX;
                int bigIndex = y * frameDesc.Width + x;

                _Vertices[smallIndex].x = cameraSpace[bigIndex].X;
                _Vertices[smallIndex].y = cameraSpace[bigIndex].Y;
                _Vertices[smallIndex].z = cameraSpace[bigIndex].Z;

                // Update UV mapping with CDRP
                var colorSpacePoint = colorSpace[bigIndex];
                _UV[smallIndex] = new Vector2(colorSpacePoint.X / colorWidth, colorSpacePoint.Y / colorHeight);
            }
        }
        
        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();

        bool sendMesh = false;
        lock (readyToSendLock)
        {
            sendMesh = readyToSendMesh;
        }

        if (sendMesh)
        {
            var depthCount = depthData.Length;

            // Anzahl der Punkte schicken
            SendDataToClient(BitConverter.GetBytes(depthCount));

            // Punktdaten sammeln und als einzelne Nachricht schicken
            int byteCount = 3 * sizeof(float) * depthCount;
            byte[] dataToSend = new byte[byteCount];
            for (int i = 0; i < depthCount; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(cameraSpace[i].X), 0, dataToSend, i * 3 * sizeof(float), sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(cameraSpace[i].Y), 0, dataToSend, i * 3 * sizeof(float) + sizeof(float), sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(cameraSpace[i].Z), 0, dataToSend, i * 3 * sizeof(float) + 2 * sizeof(float), sizeof(float));
            }

            textInGui.text = String.Format("Sending {0} * {1} = {2} bytes...", 3, sizeof(float), dataToSend.Length);

            SendDataToClient(dataToSend.ToArray());

            lock (readyToSendLock)
            {
                readyToSendMesh = false;
            }
        }
    }
    
    private double GetAvg(ushort[] depthData, int x, int y, int width, int height)
    {
        double sum = 0.0;
        
        for (int y1 = y; y1 < y + _DownsampleSize; y1++)
        {
            for (int x1 = x; x1 < x + _DownsampleSize; x1++)
            {
                int fullIndex = (y1 * width) + x1;
                
                if (depthData[fullIndex] == 0)
                    sum += 4500;
                else
                    sum += depthData[fullIndex];
                
            }
        }

        return sum / (_DownsampleSize * _DownsampleSize);
    }

    byte[] m_DataBuffer = new byte[32 * 1024 * 1024];

    public void OnClientConnect(IAsyncResult asyn)
    {
        try
        {
            textInGui.text = "Client is connecting...";

            m_socWorker = m_socListener.EndAccept(asyn);
            // now start to listen for any data...
            m_socWorker.BeginReceive(m_DataBuffer, 0, m_DataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
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

        //Process data here the way you want , all your bytes will be stored in recData
        var receivedString = System.Text.Encoding.Default.GetString(recData);
        messageCount++;
        textInGui.text = "Ready signal received from client, sending test data #" + messageCount + "...";

        // Flag setzen, dass Mesh gesendet werden kann
        lock(readyToSendLock)
        {
            readyToSendMesh = true;
        }

        //Start receiving again
        m_socWorker.BeginReceive(_recieveBuffer, 0, _recieveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
    }

    private void SendLineToClient(string line)
    {
        SendDataToClient(Encoding.ASCII.GetBytes(line + "\n"));
    }

    private void SendDataToClient(byte[] data)
    {
        if (m_socWorker == null) return;

        SocketAsyncEventArgs socketAsyncData = new SocketAsyncEventArgs();
        socketAsyncData.SetBuffer(data, 0, data.Length);
        m_socWorker.SendAsync(socketAsyncData);
    }

    void OnApplicationQuit()
    {
        if (_Mapper != null)
        {
            _Mapper = null;
        }
        
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}
