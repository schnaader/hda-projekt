using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using UnityEngine.UI;
#if !UNITY_EDITOR
//using ZXing;
#endif
public class QRCode : MonoBehaviour {
    private Text gui;
    // Use this for initialization
    PhotoCapture photoCaptureObject = null;
    void Start()
    {
        gui = GameObject.FindObjectOfType<Text>();
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);

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
    }
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
