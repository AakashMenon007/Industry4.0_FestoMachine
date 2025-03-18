using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LibUA;
using LibUA.Core;
using UnityEngine;

public class OPCUAClient : Client
{
    X509Certificate2 appCertificate = null;
    #if !NET_STANDARD
    RSACng cryptPrivateKey = null;
    #endif
    public delegate void OnSubsriptionValueChangedDel(uint subid, uint clienthandle, object value);
    public OnSubsriptionValueChangedDel OnSubsriptionValueChanged;
    public string Publiccertpath;
    public string Privatecertpath;
    public override X509Certificate2 ApplicationCertificate
    {
        get { return appCertificate; }
    }
#if !NET_STANDARD
    public override RSACng  ApplicationPrivateKey
    {
        get { return cryptPrivateKey; }
    }
#endif

    private void LoadCertificateAndPrivateKey()
    {

        if (Privatecertpath == "" || Publiccertpath == "")
            return;
        try
        {
            #if !NET_STANDARD /// Currently no certificates are supported
            // Try to load existing (public key) and associated private key
            appCertificate = new X509Certificate2(Publiccertpath);
            cryptPrivateKey = new RSACng();
            var file = File.ReadAllBytes(Privatecertpath);
            var base64 = Convert.ToBase64String(file);
            var rsaPrivParams = UASecurity.ImportRSAPrivateKey(base64);
            cryptPrivateKey.ImportParameters(rsaPrivParams);
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError("Failure in certificates " + e.Message);

        }
    }

    public OPCUAClient(string Target, int Port, int Timeout,string publiccert,string privatecert)
        : base(Target, Port, Timeout)
    {
        Publiccertpath = publiccert;
        Privatecertpath = privatecert;
        LoadCertificateAndPrivateKey();
    }
    
    public OPCUAClient(string Target, int Port, string Path, int Timeout,string publiccert,string privatecert)
        : base(Target, Port, Path, Timeout)
    {
        Publiccertpath = publiccert;
        Privatecertpath = privatecert;
        LoadCertificateAndPrivateKey();
    }

    public override void NotifyDataChangeNotifications(uint subscrId, uint[] clientHandles, DataValue[] notifications)
    {
        for (int i = 0; i < clientHandles.Length; i++)
        {
            OnSubsriptionValueChanged.Invoke(subscrId,clientHandles[i],notifications[i].Value);
        }
    }

    public override void NotifyEventNotifications(uint subscrId, uint[] clientHandles, object[][] notifications)
    {
        for (int i = 0; i < clientHandles.Length; i++)
        {
            Console.WriteLine("subscrId {0} handle {1}: {2}", subscrId, clientHandles[i],
                string.Join(",", notifications[i]));
        }
    }
}
