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
    public GameObject panelDebug;

    public GameObject loginErrorTexts;

    public InputField eventNameInput;
    public InputField standPasswordInput;

    public Text scanText;
    public Text debugTextIU;

    //Credentials data
    private string eventname;

    private string tableConvidats;
    private string tableStands;
    private string standName;


    //Private credencials
    private string servername = "quy910.yourstats.es";
    private string username = "quy910";
    private string password = "C0sm02019";
    private string dbname = "quy910";

    //Global

    public string queryResult = "";
    public string lastBarcodeValue;

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
        Scan,
        Debug
    };

    //Panels
    iuPanel actualPanel;
    iuPanel lastPanel;

    IEnumerator StartAuthorization()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
    }

    void Start()
    {

        changePanel(iuPanel.Login);

        StartCoroutine(StartAuthorization());
        debugText = "";
        scanText.text = "";
    }

    void changePanel(iuPanel target)
    {
        StopScanning();
        panelLogin.gameObject.SetActive(false);
        panelScan.gameObject.SetActive(false);
        panelDebug.gameObject.SetActive(false);

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
            case iuPanel.Debug:
                panelDebug.gameObject.SetActive(true);
                break;
        }

        actualPanel = target;
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
            switch (lastQueryName)
            {
                //Called after checking if the stand column exists
                case "checkColumnExists":
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

            var result = queryResult.Split('|');
            if (result[0].Split(':')[0] == "ERROR") debugText += queryResult + "\n";
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

    public void debugPanelButton()
    {
        if (actualPanel == iuPanel.Login || actualPanel == iuPanel.Scan)
        {
            debugTextIU.text = "eventname: " + eventname + "\n" +
                "tableconvidats: " + tableConvidats + "\n" +
                "tableStands: " + tableStands + "\n" +
                "standName: " + standName + "\n" +
                //"servername: " + servername + "\n" +
                //"username: " + username + "\n" +
                //"password: " + password + "\n" +
                //"dbname: " + dbname + "\n" +
                "---------------------------------\n";
            debugTextIU.text += debugText;

            lastPanel = actualPanel;
            changePanel(iuPanel.Debug);
        }
        else changePanel(lastPanel);
    }

    public void clearDebugText()
    {
        debugTextIU.text = "eventname: " + eventname + "\n" +
               "tableconvidats: " + tableConvidats + "\n" +
               "tableStands: " + tableStands + "\n" +
               "standName: " + standName + "\n" +
               //"servername: " + servername + "\n" +
               //"username: " + username + "\n" +
               //"password: " + password + "\n" +
               //"dbname: " + dbname + "\n" +
               "---------------------------------\n";
        debugText = "";
        debugTextIU.text += debugText;
    }
}
