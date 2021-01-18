using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSNetStandard20Lib;
using UnityEngine.Networking;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log("Text from FS lib: " + Say.aString);

        StartCoroutine(GetText());
    }

    private IEnumerator GetText()
    {
        Debug.Log("Sending web request");
        var webRequest = UnityWebRequest.Get("https://localhost:5001/WeatherForecast/helloWorld");
        yield return webRequest.SendWebRequest();

        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            Debug.Log(webRequest.error);
        }
        else
        {
            var text = webRequest.downloadHandler.text;
            Debug.Log("Text from server: " + text);
        }
    }

    // Update is called once per frame
    private void Update()
    {
    }
}
