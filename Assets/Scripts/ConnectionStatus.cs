using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Sockets;
using System.Threading.Tasks;

public class IPConnectionManager : MonoBehaviour
{
    [Header("Connection Settings")]
    public string ipAddress = "192.168.1.1";
    public int port = 80;
    public float checkInterval = 2f;

    [Header("UI Elements")]
    public Image connectionPanelImage;
    public TMP_Text connectionStatusTMP;

    [Header("Status Colors")]
    public Color connectedColor = Color.green;
    public Color disconnectedColor = Color.red;

    private bool isConnected = false;

    private void Start()
    {
        // Initialize UI with default connection state
        UpdateConnectionUI();

        // Start the loop to check connection repeatedly
        InvokeRepeating(nameof(CheckAndUpdateConnection), 0f, checkInterval);
    }

    private async void CheckAndUpdateConnection()
    {
        isConnected = await PingAddress(ipAddress, port);

        // Always update UI, even if connection state didn't change
        UpdateConnectionUI();
    }

    private void UpdateConnectionUI()
    {
        if (isConnected)
        {
            Debug.Log("Device is connected!");
            connectionPanelImage.color = connectedColor;
            connectionStatusTMP.text = "Connected";
        }
        else
        {
            Debug.Log("Device is disconnected!");
            connectionPanelImage.color = disconnectedColor;
            connectionStatusTMP.text = "Disconnected";
        }
    }

    private async Task<bool> PingAddress(string ip, int port)
    {
        try
        {
            using (TcpClient client = new TcpClient())
            {
                var task = client.ConnectAsync(ip, port);
                if (await Task.WhenAny(task, Task.Delay(1000)) == task)
                {
                    return client.Connected;
                }
                else
                {
                    Debug.LogWarning($"Ping to {ip}:{port} timed out.");
                    return false;
                }
            }
        }
        catch (SocketException se)
        {
            Debug.LogWarning($"SocketException: {se.Message}");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Exception: {ex.Message}");
            return false;
        }
    }
}
