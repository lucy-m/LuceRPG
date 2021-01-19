using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using LuceRPG.Models;
using LuceRPG.Serialisation;
using Microsoft.FSharp.Core;

public class Overlord : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(LoadWorld());
    }

    private IEnumerator LoadWorld()
    {
        Debug.Log("Attempting to load world");
        var webRequest = UnityWebRequest.Get("https://localhost:5001/World");
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            var bytes = webRequest.downloadHandler.data;
            Debug.Log("Received bytes " + bytes.Length);

            var tWorld = WorldSrl.deserialise(bytes);

            if (FSharpOption<DesrlResult.Model<WorldModule.Model>>.get_IsSome(tWorld))
            {
                var world = tWorld.Value.value;
                Debug.Log(world);

                if (WorldLoader.Instance != null)
                {
                    Debug.Log("Loading world");
                    WorldLoader.Instance.LoadWorld(world);
                }
            }
        }
        else
        {
            Debug.Log("Web request error " + webRequest.error);
        }
    }
}
