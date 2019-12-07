using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using QRCodeReaderAndGenerator;

public class test : MonoBehaviour
{
    [SerializeField]
    public Text debugText;

    [SerializeField]
    public RawImage camImage;

    void Start()
    {
       // StartCoroutine(GetData());

        //comentar si android
       StartCoroutine(StartAuthorization());
        debugText.text = "";
    }

    IEnumerator StartAuthorization()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
    }

    public void InserNewMarc()
    {
        StartCoroutine(GetData());
    }

    IEnumerator GetData()
    {
        //gameObject.guiText.text = "Loading...";

        Debug.Log("Crida");

        WWWForm form = new WWWForm();
        form.AddField("CODIGO_BARRAS", System.DateTime.Now.ToString());

        UnityWebRequest www = UnityWebRequest.Post("https://yourstats.es/Unity_App/scripts/insert.php", form);
        while (!www.isDone)
        {
            yield return www.SendWebRequest();
        }

        var data = www.downloadHandler.text;

       // Debug.Log(data.ToString());

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            debugText.text = www.error;
        }
        else
        {
            Debug.Log("Form upload complete!");
            debugText.text = "Form upload complete!";
        }


    }

    public void Scan()
    {
        if (camImage)
        {
            QRCodeManager.CameraSettings camSettings = new QRCodeManager.CameraSettings();
            string rearCamName = GetRearCamName();
            if (rearCamName != null)
            {
                camSettings.deviceName = rearCamName;
                camSettings.maintainAspectRatio = true;
                camSettings.makeSquare = true;
                camSettings.requestedWidth = 1280;
                camSettings.requestedHeight = 720;
                camSettings.scanType = ScanType.CONTINUOUS;
                QRCodeManager.Instance.ScanQRCode(camSettings, camImage, 1f);
            }
        }
    }

    public void StopScanning()
    {
        QRCodeManager.Instance.StopScanning();
    }

    void OnEnable()
    {
        QRCodeManager.onError += HandleOnError;
        QRCodeManager.onQrCodeFound += HandleOnQRCodeFound;
    }

    void OnDisable()
    {
        QRCodeManager.onError -= HandleOnError;
        QRCodeManager.onQrCodeFound -= HandleOnQRCodeFound;
    }

    void HandleOnQRCodeFound(ZXing.BarcodeFormat barCodeType, string barCodeValue)
    {
        Debug.Log(barCodeType + " __ " + barCodeValue);
        debugText.text = barCodeValue;
    }

    void HandleOnError(string err)
    {
        Debug.LogError(err);
    }

    string GetRearCamName()
    {
        foreach (WebCamDevice device in WebCamTexture.devices)
        {
            if (!device.isFrontFacing)
            {
                return device.name;
            }
        }
        return null;
    }
}
