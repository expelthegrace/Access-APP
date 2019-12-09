using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using QRCodeReaderAndGenerator;

public class main : MonoBehaviour
{
    [SerializeField]
    public Text debugText;

    [SerializeField]
    public RawImage camImage;

    //Buttons
    public Button scanButton;
    public Button stopScanButton;

    //Credentials data
    public string eventname = "accesstest";
    public string servername = "quy910.yourstats.es";
    public string username = "quy910";
    public string password = "C0sm02019";
    public string dbname = "quy910";

    public string tableConvidats = "accesstest";
    public string tableStands = "accesstestStands";
    public string standName = "ingeniastand3";

    //Global
    public string QRlecture = "111";

    public string queryResult = "--";

    private bool queryEnded = false;
    private bool runningQuery = false;
    private string lastQueryName = "";

    void Start()
    {
        // StartCoroutine(GetData());

        //comentar si android
        StartCoroutine(StartAuthorization());
        debugText.text = "";
    }

    public void Update()
    {
        // Since DDBB calls are coroutines the followeing system manages when these coroutines are finished
        //When a DDBB corotutine is called its called alongisde a name, this name is checked here to trigger the proper response to the finished coroutine
        if (queryEnded && !runningQuery)
        {
            Debug.Log("rountine finished: " + lastQueryName);
            switch (lastQueryName)
            {
                //Called after checking if the stand column exists
                case "checkColumnExists":
                    Debug.Log("columnsheck result: " + queryResult);
                    if (queryResult == "0|\n") AddColumn();
                    else checkColumnStand();
                    break;
                //Called if needed to add stand column
                case "addColumnStand":
                    checkColumnStand();
                    break;
            }

            queryEnded = false;
        }
    }

    void AddColumn()
    {
        Debug.Log("1111 ");
        string sql = "ALTER TABLE " + dbname + "." + tableConvidats + " ADD COLUMN " + standName + " VARCHAR(1) NOT NULL DEFAULT 0";
        StartCoroutine(ExecuteQuery(sql, "addColumnStand"));
    }

    void checkColumnStand()
    {
        Debug.Log("222 ");
        string sql = "UPDATE " + dbname + "." + tableConvidats + " SET " + standName + " = 1 WHERE CODIGO_BARRAS = " + QRlecture;
        StartCoroutine(ExecuteQuery(sql, "checkColumnStand"));
    }

    public void checkAssistant(string barcodevalue)
    {
        string sql = "SELECT COUNT(*) AS TOTAL FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + tableConvidats + "' AND COLUMN_NAME = '" + standName + "'";
        StartCoroutine(ExecuteQuery(sql, "checkColumnExists", "TOTAL"));

        //The secuence continues in Update()
    }

    IEnumerator StartAuthorization()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
    }


    IEnumerator ExecuteQuery(string query, string queryName, string returnFields = "CODIGO_BARRAS")
    {
        if (query != "")
        {
            runningQuery = true;
            WWWForm form = new WWWForm();
            form.AddField("servername",servername);
            form.AddField("username",username);
            form.AddField("password",password);
            form.AddField("dbname", dbname);
            form.AddField("query", query);
            form.AddField("returnFields", returnFields);

            UnityWebRequest www = UnityWebRequest.Post("https://yourstats.es/Unity_App/scripts/query.php", form);
            while (!www.isDone)
            {
                yield return www.SendWebRequest();
            }

            var data = www.downloadHandler.text;

            runningQuery = false;
            queryEnded = true;
            lastQueryName = queryName;

            queryResult = data.ToString();
            debugText.text = queryResult;
        }
    }

    IEnumerator ExecuteMultiQuery(string query, string returnFields = "CODIGO_BARRAS")
    {
        if (query != "")
        {
            WWWForm form = new WWWForm();
            form.AddField("servername",servername);
            form.AddField("username",username);
            form.AddField("password",password);
            form.AddField("dbname", dbname);
            form.AddField("query", query);
            form.AddField("returnFields", returnFields);

            UnityWebRequest www = UnityWebRequest.Post("https://yourstats.es/Unity_App/scripts/multiquery.php", form);
            while (!www.isDone)
            {
                yield return www.SendWebRequest();
            }


            var data = www.downloadHandler.text;
            
            queryResult = data.ToString();
            debugText.text = queryResult;
        }
    }

    public void Scan()
    {
        stopScanButton.enabled = true;
        scanButton.enabled = false;

        camImage.enabled = true;
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
        camImage.enabled = false;

        stopScanButton.enabled = false;
        scanButton.enabled = true;
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
       // debugText.text = barCodeValue;
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
