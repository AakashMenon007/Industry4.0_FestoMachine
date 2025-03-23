using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageRequest : MonoBehaviour
{
    public string url;
    public Texture2D camTexture;
    public RawImage imageFromMachine;

    private void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            SendImageRequest();
        }
        imageFromMachine.texture = camTexture;
    }

    public void SendImageRequest()
    {
        StartCoroutine(DownloadImage(url));
    }

    IEnumerator DownloadImage(string MediaUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
        yield return request.SendWebRequest();

        // Updated error handling to use UnityWebRequest.Result
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Request error: {request.error}");
        }
        else
        {
            Debug.Log("Request success");
            camTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        }
    }
}
