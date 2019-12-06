using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class test : MonoBehaviour
{
    public Text debugText;

    void Start()
    {
       // StartCoroutine(GetData());
        debugText.text = "";
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
}
