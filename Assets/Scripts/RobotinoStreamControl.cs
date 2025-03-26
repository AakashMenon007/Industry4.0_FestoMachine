using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class RobotinoStreamController : MonoBehaviour
{
    [Header("Stream Settings")]
    [Tooltip("Base URL from Robotino")]
    public string baseUrl = "http://172.21.10.90/fcgi-bin/?";

    [Tooltip("Target frames per second")]
    [Range(1, 30)] public int targetFPS = 15;

    [Header("UI Reference")]
    [SerializeField] private RawImage displayImage;

    private Texture2D currentTexture;
    private Coroutine streamingCoroutine;
    private bool isStreaming = false;

    void Awake()
    {
        InitializeTexture();
    }

    void OnEnable()
    {
        StartStreaming();
    }

    void OnDisable()
    {
        StopStreaming();
    }

    void InitializeTexture()
    {
        if (currentTexture == null)
        {
            currentTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        }
        displayImage.texture = currentTexture;
    }

    void StartStreaming()
    {
        if (isStreaming) return;

        isStreaming = true;
        streamingCoroutine = StartCoroutine(StreamUpdateLoop());
    }

    void StopStreaming()
    {
        if (!isStreaming) return;

        isStreaming = false;
        if (streamingCoroutine != null)
        {
            StopCoroutine(streamingCoroutine);
            streamingCoroutine = null;
        }
    }

    IEnumerator StreamUpdateLoop()
    {
        while (isStreaming)
        {
            yield return StartCoroutine(FetchCameraFrame());
            yield return new WaitForSeconds(1f / targetFPS);
        }
    }

    IEnumerator FetchCameraFrame()
    {
        string jsonParams = $"{{\"TYPE\":\"getimage\",\"A\":{DateTime.Now.Ticks}}}";
        string encodedParams = Uri.EscapeUriString(jsonParams);
        string fullUrl = baseUrl + encodedParams;

        using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(fullUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var newTexture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                UpdateDisplayTexture(newTexture);
            }
            else
            {
                Debug.LogError($"Stream Error: {request.error}");
            }
        }
    }

    void UpdateDisplayTexture(Texture2D newTexture)
    {
        if (currentTexture == null || currentTexture.width != newTexture.width || currentTexture.height != newTexture.height)
        {
            currentTexture = new Texture2D(newTexture.width, newTexture.height, TextureFormat.RGB24, false);
            displayImage.texture = currentTexture;
        }

        currentTexture.LoadRawTextureData(newTexture.GetRawTextureData());
        currentTexture.Apply();
    }
}
