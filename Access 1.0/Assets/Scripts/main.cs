using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using QRCodeReaderAndGenerator;
using System;

public class main : MonoBehaviour
{
    
    string debugText;

    [SerializeField]
    public RawImage camImage;

    //UI
    public Button scanButton;
    public Button stopScanButton;
    public Image checkImage;

    public GameObject panelLogin;
    public GameObject panelScan;

    public GameObject loginErrorTexts;

    public InputField eventNameInput;
    public InputField standPasswordInput;

    public Text scanText;

    //Credentials data
    public string eventname = "accesstest";

    public string tableConvidats = "accesstest";
    public string tableStands = "accesstestStands";
    public string standName = "ingeniastand5";


    //Private credencials
    public string servername = "quy910.yourstats.es";
    public string username = "quy910";
    public string password = "C0sm02019";
    public string dbname = "quy910";

    //Global

    public string queryResult = "--";
    public string lastBarcodeValue = "123";

    private bool queryEnded = false;
    private bool runningQuery = false;
    private bool QRscanned = false;
    private bool runningQRscanned = false;

    private string lastQueryName = "";

    //Timers
    float lastCheckImage = 0;
    float timeBetweenCaptures = 2f;

    //Audio
    public AudioSource QRbeepedAudio;

    enum iuPanel
    {
        Login,
        Scan
    };

    iuPanel actualPanel;

    IEnumerator StartAuthorization()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
    }

    void changePanel(iuPanel target)
    {
        StopScanning();
        panelLogin.gameObject.SetActive(false);
        panelScan.gameObject.SetActive(false);

        switch (target)
        {
            case iuPanel.Login:
                panelLogin.gameObject.SetActive(true);
                loginErrorTexts.gameObject.SetActive(false);
                break;
            case iuPanel.Scan:
                panelScan.gameObject.SetActive(true);

                stopScanButton.gameObject.SetActive(false);
                scanButton.gameObject.SetActive(true);
                checkImage.gameObject.SetActive(false);

                camImage.gameObject.SetActive(false);

                scanButton.gameObject.SetActive(true);
                break;
        }

        actualPanel = target;
    }

    void Start()
    {
        
        changePanel(iuPanel.Login);

        StartCoroutine(StartAuthorization());
        debugText = "";
        scanText.text = "";


    }

    public void CoroutineManager()
    {
        if (QRscanned)
        {
            columnStandExists();
            QRscanned = false;
        }

        // Since DDBB calls are coroutines the following system manages when these coroutines are finished
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
                    else markColumnStand();
                    break;
                //Called if needed to add stand column
                case "addColumnStand":
                    markColumnStand();
                    break;
                case "markColumnStand":
                    runningQRscanned = false;
                    scanText.text = "Last Scann:" + System.DateTime.Now.ToString();
                    checkImage.gameObject.SetActive(true);
                    lastCheckImage = Time.realtimeSinceStartup;
                    QRbeepedAudio.Play();
                    break;
                case "login":
                    var result = queryResult.Split('|');
                     Debug.Log(result[0]);
                    if (result[0] != "" && result[0].Split(':')[0] != "ERROR")
                    {
                        ConsolidateCredentials(result[0]);
                        changePanel(iuPanel.Scan);
                    }
                    else LoginError();
                    break;
            }

            queryEnded = false;
        }

    }

    private void LoginError()
    {
        debugText += queryResult;
        loginErrorTexts.gameObject.SetActive(true);
    }
    public void AcceptLoginError()
    {
        loginErrorTexts.gameObject.SetActive(true);
    }

    private void ConsolidateCredentials(string standname)
    {
        tableConvidats = eventNameInput.text;
        tableStands = tableConvidats + "Stands";
        standName = standname;
    }

    public void LogIn()
    {
        string sql = "SELECT name FROM " + eventNameInput.text + "Stands" + " WHERE password = " + standPasswordInput.text;
        Debug.Log("bame i pass: " + eventNameInput.text + "," + standPasswordInput.text);
        StartCoroutine(ExecuteQuery(sql, "login", "name"));
    }

    public void Update()
    {

        CoroutineManager();
        if (checkImage.IsActive() && Time.realtimeSinceStartup - lastCheckImage > timeBetweenCaptures)
        {
            checkImage.gameObject.SetActive(false);
        }
    }

    //Creates column on the BBDD with the name of the stand
    void AddColumn()
    {
        string sql = "ALTER TABLE " + dbname + "." + tableConvidats + " ADD COLUMN " + standName + " VARCHAR(30) NOT NULL DEFAULT 0";
        StartCoroutine(ExecuteQuery(sql, "addColumnStand"));
    }

    //Sets to 1 the column of the stand in the participant with the scanned code
    void markColumnStand()
    {
        string sql = "UPDATE " + dbname + "." + tableConvidats + " SET " + standName + " = 1 WHERE CODIGO_BARRAS = " + lastBarcodeValue;
        StartCoroutine(ExecuteQuery(sql, "markColumnStand"));
    }

    //Check if a column with the stand name already exists
    public void columnStandExists()
    {
        string sql = "SELECT COUNT(*) AS TOTAL FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + tableConvidats + "' AND COLUMN_NAME = '" + standName + "'";
        StartCoroutine(ExecuteQuery(sql, "checkColumnExists", "TOTAL"));

    }

    //Query function that takes a query, a name for the query and the columns you want the server php script to return back to the app
    //The return fields have to follow this patron: "return1" or "return1|return2|return3|etc..." and they are returned witht he same format
    //Script is in yourstats.es/html/Unity_App/scripts/query.php
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

            //Global where holds the query return
            queryResult = data.ToString();
          
        }
    }

    IEnumerator ExecuteMultiQuery(string query, string queryName, string returnFields = "CODIGO_BARRAS")
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

            UnityWebRequest www = UnityWebRequest.Post("https://yourstats.es/Unity_App/scripts/multiquery.php", form);
            while (!www.isDone)
            {
                yield return www.SendWebRequest();
            }

            runningQuery = false;
            queryEnded = true;
            var data = www.downloadHandler.text;
            
            queryResult = data.ToString();
           
        }
    }

    public void Scan()
    {
        stopScanButton.gameObject.SetActive(true);
        scanButton.gameObject.SetActive(false);

        camImage.gameObject.SetActive(true);
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
        camImage.gameObject.SetActive(false);

        stopScanButton.gameObject.SetActive(false);
        scanButton.gameObject.SetActive(true);
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
        lastBarcodeValue = barCodeValue;
        if (!runningQRscanned && !checkImage.IsActive())
        {
            QRscanned = true;
            runningQRscanned = true;
        }
    }

    void HandleOnError(string err)
    {
        Debug.LogError(err);
        debugText += err;
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
