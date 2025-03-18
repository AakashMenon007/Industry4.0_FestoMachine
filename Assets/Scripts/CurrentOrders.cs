using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CurrentOrders : MonoBehaviour
{
    public List<string> CurrentOrderData = new List<string>();

    public CurrentOrderJSON[] currentOrdersObjectArray;

    public string listInfo;

    public TMP_Text currentOrdersInfo;

    // Method to receive data from the server and update UI
    public void ReceieveData(string CurrentOrderStringPHPMany)
    {
        // Ensure JSON is formatted correctly
        string newCurrentOrderStringPHPMany = fixJson(CurrentOrderStringPHPMany);

        Debug.LogWarning(newCurrentOrderStringPHPMany);

        // Parse JSON data into an array of objects
        currentOrdersObjectArray = JsonHelper.FromJson<CurrentOrderJSON>(newCurrentOrderStringPHPMany);

        CurrentOrderData.Clear();
        listInfo = "";

        // Iterate through the array, log each order, and add to the list
        for (int i = 0; i < currentOrdersObjectArray.Length; i++)
        {
            Debug.Log("ONo:" + currentOrdersObjectArray[i].ONo + ", Company:" + currentOrdersObjectArray[i].Company + ", Planned Start:" + currentOrdersObjectArray[i].PlannedStart + ", Planned End:" + currentOrdersObjectArray[i].PlannedEnd + ", State:" + currentOrdersObjectArray[i].State);

            CurrentOrderData.Add("Order Number: " + currentOrdersObjectArray[i].ONo + ", Company Name: " + currentOrdersObjectArray[i].Company + ", Planned Start Time: " + currentOrdersObjectArray[i].PlannedStart + ", Planned End Time: " + currentOrdersObjectArray[i].PlannedEnd + ", Build State: " + currentOrdersObjectArray[i].State);
        }

        // Concatenate the list items into a string with new lines
        foreach (var listMember in CurrentOrderData)
        {
            listInfo += listMember.ToString() + "\n" + "\n";
        }

        // Update the UI text field
        currentOrdersInfo.text = listInfo;
    }

    // Ensure JSON format compatibility
    string fixJson(string value)
    {
        value = "{\"Items\":" + value + "}";
        return value;
    }

    // Coroutine to make a GET request to the server
    public void GetCurrentOrders()
    {
        StartCoroutine(GetRequest("http://172.21.0.90/SQLData.php?Command=currentOrders"));     //calls coroutine and sets string
    }

    // Coroutine to handle the GET request and update UI based on response
    IEnumerator GetRequest(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = url.Split('/');
            int page = pages.Length - 1;

            // Handle different results of the web request
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                    // Process received data
                    ReceieveData(webRequest.downloadHandler.text);
                    Debug.LogError("Current Orders Success");
                    break;
            }
        }
    }
}
