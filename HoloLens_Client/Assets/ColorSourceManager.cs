using UnityEngine;
using System.Collections;

public class ColorSourceManager : MonoBehaviour 
{
    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }

    private Texture2D _Texture;
    private byte[] _Data;
    
    public Texture2D GetColorTexture()
    {
        return _Texture;
    }
    
    void Start()
    {
            _Texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            _Data = new byte[4 * 1920 * 1080];
    }
    
    /*void Update () 
    {
        if (_Reader != null) 
        {
            var frame = _Reader.AcquireLatestFrame();
            
            if (frame != null)
            {
                frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);
                _Texture.LoadRawTextureData(_Data);
                _Texture.Apply();
                
                frame.Dispose();
                frame = null;
            }
        }
    }*/
}
