using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using UnityEngine.UI;
using System;
#if !UNITY_EDITOR
//using ZXing;
#endif
public class QRCode : MonoBehaviour {
    private Text gui;
    private GameObject image;
    private Material mat;
    // Use this for initialization
    PhotoCapture photoCaptureObject = null;
    Texture2D ImageTexture = null;
    void Start()
    {
        gui = GameObject.FindObjectOfType<Text>();
        image = GameObject.Find("Image");
        if (image == null)
        {
            gui.text = "Could not find image object";
            return;
        }
        mat = image.GetComponent<Renderer>().material;
        if (mat == null)
        {
            gui.text = "Could not find renderer or material";
            return;
        }
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        ImageTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
    }
    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        gui.text = "Start Capture";
        photoCaptureObject = captureObject;

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }
    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        gui.text = "Stop Capture";
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
            gui.text = "Unable to start photo mode!";
        }
    }
    /*
    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            string filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);



            photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
            gui.text = "Picture saved to:" + filePath;
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
            gui.text = "Unable to start photo mode!";
        }
    }*/
    /*
    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            Debug.Log("Saved Photo to disk!");
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
        else
        {
            Debug.Log("Failed to save Photo to disk");
            gui.text = "Failed to save Photo to disk";
        }
    }
   */
        void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
           /* List<byte> imageBufferList = new List<byte>();
            // Copy the raw IMFMediaBuffer data into our empty byte list.
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

            // In this example, we captured the image using the BGRA32 format.
            // So our stride will be 4 since we have a byte for each rgba channel.
            // The raw image data will also be flipped so we access our pixel data
            // in the reverse order.
            int stride = 4;
            float denominator = 1.0f / 255.0f;
            List<Color> colorArray = new List<Color>();
            for (int i = imageBufferList.Count - 1; i >= 0; i -= stride)
            {
                float a = (int)(imageBufferList[i - 0]) * denominator;
                float r = (int)(imageBufferList[i - 1]) * denominator;
                float g = (int)(imageBufferList[i - 2]) * denominator;
                float b = (int)(imageBufferList[i - 3]) * denominator;

                colorArray.Add(new Color(r, g, b, a));
            }*/
            gui.text = "Do Something";
            // Now we could do something with the array such as texture.SetPixels() or run image processing on the list
            photoCaptureFrame.UploadImageDataToTexture(ImageTexture);
            mat.mainTexture = ImageTexture;
        }
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }





    /*
    #if !UNITY_EDITOR
    // create a barcode reader instance
    BarcodeReader barcodeReader = new BarcodeReader { AutoRotate = true };

        //Used to detect a QR code in a byte array image, return the decoded text
        public void DecodeFromByteArray(byte[] arrayByte, int Width, int Height)
        {

            try
            {
                // Locatable camera from the HoloLens use BGRA32 format in the MRCManager class 
                Result TextResult = barcodeReader.Decode(arrayByte, Width, Height, RGBLuminanceSource.BitmapFormat.BGRA32);
                //If QR code detected 
                if (TextResult != null)
                {
                    Debug.Log("Result decoding: " + TextResult.Text);

                    //
                    // Do what you want with the result here
                    //
                }
                //If QR code not detected
                else
                {
                    Debug.Log("No QR code detected");
                }

            }
            //If error while decoding
            catch (Exception e)
            {
                Debug.Log("Exception:" + e);
            }

        }
    #endif

        */




    // Update is called once per frame
    void Update () {
		
	}
}
