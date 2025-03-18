using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary> 
/// TCP Client-Server Connection Example.
/// 2 way communication: Client (your app) connects to the server and can send and receive messages.
/// Simply place this script on an empty gameobject in your scene. Note your firewall may need to be disabled.
/// The OrderCompleteNewPhone method is an example of how you could use it, e.g., calling it from a UI button.
/// <summary> 

public class SendOrder : MonoBehaviour
{
    #region private members 	
    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    #endregion

    #region public members 	

    // Address of the server PC connected to the Festo machine. This should not need to be changed.
    public string serverAddress = "172.21.0.90";

    // Example of a message to send to the MES server.
    // This message places a new order of the complete mobile phone (known as part number 210). See documentation on Canvas for a full breakdown of how this string is formatted.
    // Additional part numbers can be found on the Festo PC.   

    public string partNumber;
    public string qty;

    public string newOrderMessage;
    #endregion

    // Use this for initialization 	
    void Start()
    {
        ConnectToTcpServer();
    }

    // Method to initiate the connection to the TCP server
    private void ConnectToTcpServer()
    {
        try
        {
            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }

    // Method to listen for incoming data from the TCP server
    private void ListenForData()
    {
        try
        {
            socketConnection = new TcpClient(serverAddress, 2000);
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                // Get a stream object for reading 				
                using (NetworkStream stream = socketConnection.GetStream())
                {
                    int length;
                    // Read incoming stream into byte array 					
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incomingData = new byte[length];
                        Array.Copy(bytes, 0, incomingData, 0, length);
                        // Convert byte array to string message 						
                        string serverMessage = Encoding.ASCII.GetString(incomingData);

                        // Log the received server message
                        Debug.Log("Server message received as: " + serverMessage);
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    // Method to send a message to the TCP server
    private void SendMessageToServer(string message)
    {
        if (socketConnection == null)
        {
            return;
        }
        try
        {
            // Get a stream object for writing 			
            NetworkStream stream = socketConnection.GetStream();
            if (stream.CanWrite)
            {
                string clientMessage = message;
                // Convert string message to byte array                 
                byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
                // Write byte array to socketConnection stream                 
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                Debug.Log("Message has been sent by client - should be received by the server");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    // Method to send an order to the factory
    public void SendOrderToFactory()
    {
        newOrderMessage = "444;RequestID=0;MClass=101;MNo=2;ErrorState=0;#PNo= " + partNumber + ";#Aux1Int=" + qty.ToString() + "\r";
        SendMessageToServer(newOrderMessage);
        Debug.Log("New phone order sent to the factory");
    }

    public void RequestImage()
    {

    }
}
