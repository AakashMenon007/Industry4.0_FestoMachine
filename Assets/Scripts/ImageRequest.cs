using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
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
        if (request.isNetworkError || request.isHttpError)
            Debug.Log(request.error);
        else
            Debug.Log("Request success");
            camTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            //Sprite tempSprite = Sprite.Create(camTexture, new Rect(0, 0, camTexture.width, camTexture.height), Vector2.zero, 0);
            //imageFromMachine.sprite = tempSprite;
    }
}
