// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license


#if UNITY_STANDALONE_WIN
#pragma warning disable 0168
#pragma warning disable 0649
using UnityEngine;
using System.Threading;
using NaughtyAttributes;
#if REALVIRTUAL_SIEMENSSIMIT
using CouplingToolbox;
using CouplingToolbox.Simit;
#endif


//!  Shared memory interface for an interface based on Siemens Simit shared memory structure (see Simit documentation)
namespace realvirtual
{
[HelpURL("https://game4automation.com/documentation/current/simit.html")]
#if REALVIRTUAL_SIEMENSSIMIT
[RequireComponent(typeof(realvirtualCoupler))]
#endif
public class SiemensSimitInterface : InterfaceBaseClass
{
#if !REALVIRTUAL_SIEMENSSIMIT
        [InfoBox("To use the Siemens Simit Interface, you need to install the Siemens Simit SDK and add the define GAME4AUTOMATION_SIEMENSSIMIT to the Scripting Define Symbols in the Player Settings (Edit->Project Settings->Player->Other Settings->Scripting Define Symbols")]

#endif
    public GameObject SimitConnection; //! Reference to the Simit Connection
#if REALVIRTUAL_SIEMENSSIMIT
    [realvirtual.ReadOnly] private string SimitConnectionStatus;
    private realvirtualCoupler simitcoupler;
    private SimitConnector simitconnector;
    private TimeController timecontroller;
#endif
    public override void OpenInterface()
    {
    }
#if REALVIRTUAL_SIEMENSSIMIT
    // Use this for initialization
    void Start()
    {
        simitcoupler = GetComponent<realvirtualCoupler>();
        simitconnector = SimitConnection.GetComponent<SimitConnector>();
        if (simitconnector == null)
        {
            Error("Siemens Simit Interface - No Simit Connection with included Simit Connector referenced!");
            return;
        }

        OpenInterface();
    }

    void FixedUpdate()
    {
        simitcoupler.UpdateSignals();
    }

    void Update()
    {
        if (simitconnector.ConnectionStatus == ConnectionStatus.Connected)
        {
            IsConnected = true;
            OnConnected();
        }

        if (IsConnected && simitconnector.ConnectionStatus != ConnectionStatus.Connected)
        {
            IsConnected = false;
            OnDisconnected();
        }

        SimitConnectionStatus = simitconnector.ConnectionStatus.ToString();
    }
    
    
    [Button("Update Simit Signals")]
    public void UpdateSimitSignals()
    {
        var coupler = GetComponent<realvirtualCoupler>();
        coupler.Init();
        SimitSignalListWriter.Write();
    }
#endif
    public override void CloseInterface()
    {
        OnDisconnected();
    }
}
}
#endif

