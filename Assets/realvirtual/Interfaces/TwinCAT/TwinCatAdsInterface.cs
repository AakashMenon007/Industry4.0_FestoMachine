// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System;
using TwinCAT.Ads;
using NaughtyAttributes;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using TwinCAT.TypeSystem;
using UnityEditor;
using Time = UnityEngine.Time;

namespace realvirtual
{
#pragma warning disable 0618
#pragma warning disable 0219  
    
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/twincat")]
    //! TwinCAT ADS interface to Beckhoff PLCs (real and virtual)
    public class TwinCatAdsInterface : InterfaceThreadedBaseClass
    {
        public enum updatemode
        {
            Cyclic,
            OnChange,
            CyclicSumCommand
        };

        #region PublicVariables

        [BoxGroup("PLC")] public string NetId = "127.0.0.1.1.1"; //!< The TwinCAT NetID of the PLC to communicate with
        [BoxGroup("PLC")] public int Port = 851; //!< The TwinCAT Port of the PLC to communicate with

        [BoxGroup("Mode")] [OnValueChanged("ChangeUpdateMode")]
        public updatemode
            UpdateMode =
                updatemode
                    .CyclicSumCommand; //! Specifies if the event should be fired cyclically or only if the variable has changed.

        [BoxGroup("Mode")] [HideIf("SumCommand")]
        public int
            PLCOutputsMinUpdateCycleMs =
                40; //! The ADS server checks whether the variable has changed after this time interval. Unit is in ms.

        [BoxGroup("Mode")] [HideIf("SumCommand")]
        public int
            PLCOutputsMaxUpdateDelay =
                40; // The AdsNotification event is fired at the latest when this time has elapsed. The unit is ms.

        [BoxGroup("Mode")] [ShowIf("SumCommand")]
        public int MaxNumberADSSubCommands = 1000;

        [BoxGroup("Mode")] [HideIf("SumCommand")]
        public bool ReadAllOutputsOnStart = false;

        [BoxGroup("Mode")] [HideIf("SumCommand")]
        public bool WriteAllInputsOnStart = false;

        [BoxGroup("Mode")] public bool DebugMode = false;

        [Foldout("ADS Status")] [ReadOnly]
        public string ConnectionStatus; //!< The connection Status to the PLC - Connected if everything is ok (ReadOnly)

        [Foldout("ADS Status")] [ReadOnly] public string RouterStatus; //!< The  Status to the Router -
        [Foldout("ADS Status")] [ReadOnly] public string PLCStatus;

        [Foldout("ADS Status")] [ReadOnly]
        public int NumberInputs; //!< The number of Inputs in the interface (ReadOnly)

        [Foldout("ADS Status")] [ReadOnly]
        public int NumberOutputs; //!< The number of Outputs in the interface (ReadOnly)

        [Foldout("ADS Status")] [ReadOnly]
        public int StreamLength; //!< The number of Outputs in the interface (ReadOnly)

        [Foldout("Signal Import Options")] [ReorderableList]
        public List<string>
            RegExImportSignals; //! Regex to limit the imported signals to certain symbols. If nothing is in this list every symbol is imported 

        [Foldout("Signal Import Options")] [ReorderableList]
        public List<string>
            RegExSkipSignals; //! Regex to limit the imported signals to certain symbols. All Signals which are matchting these Regexs are not imported;

        [Foldout("Signal Import Options")] [ReorderableList]
        public List<string>
            RegExSymbolIsPLCInput; //! Regex to define which variables are PLCInputs. For some variables like globals which are not linked directly to an input or output it can not be detected automatically. 

        [Foldout("Signal Import Options")]
        public bool CreateSubFolders = true; //! Create Subfolders for the Symbols with "." separating the name.

        [Foldout("Signal Import Options")]
        public bool ReadSignalValuesOnImport = true; //! Reads the signal values on import

        #endregion

        #region Private Variables

        private class ReadBufferItem
        {
            public bool BoolValue;
            public int IntValue;
            public float FloatValue;
            public Signal signal;
        }

        struct VariableInfo
        {
            public int indexGroup;
            public int indexOffset;
            public int length;
        }


        private List<ReadBufferItem> readbuffer = new List<ReadBufferItem>();
        private TcAdsClient _adsClient = null;
        private AdsNotificationEventHandler _adsNotificationEventHandler;
        private AdsStream _dataStream;
        private AdsStream _statusStream;
        private BinaryReader _binaryReader;
        private BinaryReader _statusReader;
        private int _notificationHandle = 0;
        private Dictionary<int, InterfaceSignal> _handles;
        private PLCOutputInt plcoutputint;
        private PLCOutputBool plcoutputbool;
        private PLCOutputFloat plcoutputfloat;
        private PLCInputInt plcinputint;
        private PLCInputFloat plcinputfloat;
        private PLCInputBool plcinputbool;
        private bool importsignals = false;
        private bool reconnecting = false;
        private string lastrouterstatus;
        private float _lastreconnect;
        private Thread closethread;
        private bool isrunning = false;
        private List<VariableInfo> varinforead;
        private List<TwinCATSignal> TwinCATSignals;
        private List<TwinCATSignal> TwinCATOutputSignals;
        private List<TwinCATSignal> TwinCATInputSignals;

        #endregion
        
        #region Public Methods

        //! Opens a connection to TwinCAT ADS Client and initializes the stream readers and Router and ADS Notifications
        public override void OpenInterface()
        {
            NumberInputs = 0;
            NumberOutputs = 0;
            RouterStatus = "";
            PLCStatus = "";
            ConnectionStatus = "";
            _adsClient = new TcAdsClient();

            TwinCATSignals = new List<TwinCATSignal>();
            TwinCATOutputSignals = new List<TwinCATSignal>();
            TwinCATInputSignals = new List<TwinCATSignal>();
            _lastreconnect = UnityEngine.Time.time;
            try
            {
                _adsClient.Connect(new TwinCAT.Ads.AmsNetId(NetId), this.Port);
                _statusStream = new AdsStream(2); /* Stream zum Speichern des ADS Status der SPS */
                _statusReader = new BinaryReader(_statusStream);
                _adsClient.AmsRouterNotification +=
                    new AmsRouterNotificationEventHandler(AmsRouterNotificationCallback);

                _notificationHandle = _adsClient.AddDeviceNotification(
                    (int) AdsReservedIndexGroups.DeviceData, /* index group des Gerätestatus */
                    (int) AdsReservedIndexOffsets.DeviceDataAdsState, /*index offset des Gerätestatus */
                    _statusStream, /* stream zum Speichern des Gerätestatus */
                    AdsTransMode.OnChange, /* transfer mode: bei Änderung */
                    0, /* bei Änderungen sofort senden */
                    0,
                    null);
                /* Callback zur Reaktion auf Notifications anmelden */
                _adsClient.AdsNotification += new AdsNotificationEventHandler(OnAdsNotification);
            }
            catch (Exception e)
            {
                var error = e;
                reconnecting = true;
                OnDisconnected();
            }

            try
            {
                StateInfo stateInfo = this._adsClient.ReadState();

                if (stateInfo.AdsState == TwinCAT.Ads.AdsState.Stop
                    || stateInfo.AdsState == TwinCAT.Ads.AdsState.Start
                    || stateInfo.AdsState == TwinCAT.Ads.AdsState.Run
                )
                {
                    Log("TwinCAT interface is connected to " + NetId);
                    ConnectionStatus = "Connected to " + NetId;
                    OnConnected();
                }
                else
                {
                    reconnecting = true;
                    OnDisconnected();
                    return;
                }
            }
            catch (TwinCAT.Ads.AdsException e)
            {
                Error(("TwinCAT Error " + e.Message), this);
                ConnectionStatus = e.Message;
                if (_adsClient != null)
                {
                    _adsClient.Disconnect();
                    _adsClient.Dispose();
                }

                OnDisconnected();
                return;
            }

            //! Creates a new List of InterfaceSignals based on the Components under this Interface GameObject

            TwinCATSignals.Clear();

            var signals = gameObject.GetComponentsInChildren(typeof(Signal), true);
            foreach (Signal signal in signals)
            {
                var isignal = signal.GetInterfaceSignal();
                TwinCATSignal twincatsignal = new TwinCATSignal();
                twincatsignal.Name = isignal.Name;
                twincatsignal.SymbolName = isignal.SymbolName;
                twincatsignal.Direction = isignal.Direction;
                twincatsignal.Type = isignal.Type;
                twincatsignal.Signal = isignal.Signal;
                twincatsignal.OriginDataType = isignal.OriginDataType;
                TwinCATSignals.Add(twincatsignal);
            }

            // Read only one time all signals for Init
            if (UpdateMode != updatemode.CyclicSumCommand)
            {
                if (!importsignals)
                {
                    SubscribeOutputsAndInputs();

                    if (WriteAllInputsOnStart)
                        Invoke("WriteAllADSSignals", 0.1f);
                    if (UpdateMode != updatemode.Cyclic)
                        if (ReadAllOutputsOnStart)
                            Invoke("ReadAllADSSignals", 0.2f);
                }

                if (DebugMode)
                    Debug.Log(
                        $"TwinCAT - Subscriptions to {NumberOutputs} plc outputs and {NumberInputs} plc inputs created");
            }
            else
            {
                PrepareBlockReadWrite();
            }

            reconnecting = false;
            if (DebugMode)
                Debug.Log("TwinCAT - finished OpenInferface");


            if (UpdateMode == updatemode.CyclicSumCommand)
            {
                base.OpenInterface(); /// Starts Multithreading
            }
        }

        protected override void CommunicationThreadUpdate()
        {
            if (!IsConnected)
                return;
            /// Block Read
            var high = 1;
            var low = 1;
            var remaining = NumberOutputs;
            var write = 1;
            while (remaining > 0)
            {
                if (remaining <= MaxNumberADSSubCommands)
                    write = remaining;
                else
                    write = MaxNumberADSSubCommands;

                high = low + write - 1;
                BlockRead(low, high);
                remaining = remaining - write;
                low = high + 1;
            }

            // Block Write
            high = 1;
            low = 1;
            remaining = NumberInputs;
            write = 1;
            while (remaining > 0)
            {
                if (remaining <= MaxNumberADSSubCommands)
                    write = remaining;
                else
                    write = MaxNumberADSSubCommands;

                high = low + write - 1;
                BlockWrite(low, high);
                remaining = remaining - write;
                low = high + 1;
            }
        }
        
        //! Closes the connection to TwinCAT ADS Client 
        public override void CloseInterface()
        {
            if (!IsConnected)
                return;
            if (DebugMode)
                Debug.Log("TwinCAT Start Disconnecting");
            closethread = new Thread(CloseThread);
            closethread.Start();
            OnDisconnected();
            base.CloseInterface();
        }

        //! Imports all signal objects under the interface gameobject
        public void ImportSignals(bool simstart)
        {
            OpenInterface();
            var numsignals = 0;
            var numsymbols = 0;
#if UNITY_EDITOR
            if (!IsConnected)
            {
                EditorUtility.DisplayDialog("ERROR",
                    "No connection to TwinCAT, please make sure, that you configured the correct NetID and PortID and that TwinCAT configuration is activated!",
                    "OK", "");
                return;
            }
#endif
            if (IsConnected)
            {
                try
                {
#if UNITY_EDITOR
                    if (!simstart)
                        EditorUtility.DisplayProgressBar("Please wait", "Getting symbols", 0);
#endif
#pragma warning disable 0618
                    TcAdsSymbolInfoLoader adsSymbolInfoLoader = _adsClient.CreateSymbolInfoLoader();
#pragma warning restore 0618
                    IEnumerable<TcAdsSymbolInfo> allSymbols =
                        adsSymbolInfoLoader.GetSymbols(true).Cast<TcAdsSymbolInfo>();
                    float num = allSymbols.Count();
                    foreach (TcAdsSymbolInfo adsSymbol in allSymbols)
                    {
#if UNITY_EDITOR
                        if (!simstart)
                        {
                            numsymbols++;
                            float progress = (float) numsymbols / (float) allSymbols.Count();
                            EditorUtility.DisplayProgressBar("Please wait",
                                $"Importing Symbols in progress - {numsymbols} of {num} ", progress);
                        }
#endif
                        if (adsSymbol.Category == DataTypeCategory.Primitive)
                        {
                            // Check with Regex if Signal has to be imported, if no Regex import everything
                            var toimport = false;
                            if (RegExImportSignals != null)
                            {
                                if (RegExImportSignals.Count > 0)
                                {
                                    foreach (var regexstring in RegExImportSignals)
                                    {
                                        Regex regex = new Regex(regexstring);
                                        if (regex.IsMatch(adsSymbol.Name))
                                        {
                                            toimport = true;
                                        }
                                    }
                                }
                                else
                                {
                                    toimport = true;
                                }
                            }
                            else
                            {
                                toimport = true;
                            }

                            if (RegExSkipSignals != null && toimport)
                            {
                                if (RegExSkipSignals.Count > 0)
                                {
                                    foreach (var regexstring in RegExSkipSignals)
                                    {
                                        Regex regex = new Regex(regexstring);
                                        if (regex.IsMatch(adsSymbol.Name))
                                        {
                                            toimport = false;
                                        }
                                    }
                                }
                            }

                            if (toimport)
                            {
                                var signal = new InterfaceSignal();
                                signal.Name = adsSymbol.Name;
                                signal.SymbolName = adsSymbol.Name;
                                signal.Direction = InterfaceSignal.DIRECTION.OUTPUT;
                                signal.OriginDataType = adsSymbol.DataType.ToString();

                                // Check with Regex if Signal has to be a PLCInput
                                if (RegExSymbolIsPLCInput != null)
                                {
                                    var match = false;
                                    if (RegExSymbolIsPLCInput.Count > 0)
                                    {
                                        foreach (var regexstring in RegExSymbolIsPLCInput)
                                        {
                                            Regex regex = new Regex(regexstring);
                                            if (regex.IsMatch(adsSymbol.Name))
                                            {
                                                match = true;
                                            }
                                        }

                                        if (match)
                                        {
                                            signal.Direction = InterfaceSignal.DIRECTION.INPUT;
                                        }
                                    }
                                }

                                switch (adsSymbol.IndexGroup)
                                {
                                    case (long) AdsReservedIndexGroups.IOImageRWIB:
                                    case (long) AdsReservedIndexGroups.IOImageRWIX:
                                        signal.Direction = InterfaceSignal.DIRECTION.INPUT;
                                        break;
                                    case (long) AdsReservedIndexGroups.IOImageRWOB:
                                    case (long) AdsReservedIndexGroups.IOImageRWOX:
                                        signal.Direction = InterfaceSignal.DIRECTION.OUTPUT;
                                        break;
                                }

                                switch (adsSymbol.DataTypeId)
                                {
                                    case AdsDatatypeId.ADST_BIT:
                                        signal.Type = InterfaceSignal.TYPE.BOOL;
                                        break;
                                    case AdsDatatypeId.ADST_UINT8:
                                    case AdsDatatypeId.ADST_UINT16:
                                    case AdsDatatypeId.ADST_UINT32:
                                    case AdsDatatypeId.ADST_UINT64:
                                    case AdsDatatypeId.ADST_INT8:
                                    case AdsDatatypeId.ADST_INT16:
                                    case AdsDatatypeId.ADST_INT32:
                                    case AdsDatatypeId.ADST_INT64:
                                        signal.Type = InterfaceSignal.TYPE.INT;
                                        break;
                                    case AdsDatatypeId.ADST_REAL32:
                                    case AdsDatatypeId.ADST_REAL64:
                                        signal.Type = InterfaceSignal.TYPE.REAL;
                                        break;
                                }

                                /// Create Signals Object if not existing
                                numsignals++;
                                AddSignal(signal);
                            }
                        }
                    }
                }
                catch (TwinCAT.Ads.AdsException e)
                {
                    Debug.LogError(("TwinCAT Error " + e.Message));
                }
            }

            Log("TwinCAT " + numsignals + " Signals are imported!");
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Please wait", $"Rearranging Signals ", 100);
#endif
            ArrangeSignals();
            if (ReadSignalValuesOnImport)
            {
#if UNITY_EDITOR
                EditorUtility.DisplayProgressBar("Please wait", $"Reading current signal values ", 100);
#endif
                ReadAllADSSignals();
            }

            var signals = GetComponentsInChildren<Signal>();

            CloseInterface();
#if UNITY_EDITOR
            if (!simstart)
                EditorUtility.ClearProgressBar();
#endif
        }

        #endregion

        private bool SumCommand()
        {
            if (UpdateMode == updatemode.CyclicSumCommand)
                return true;

            return false;
        }
        
        #region PrivateMethods
           /* Callback-Funktion: Wird bei Statusänderungen des Routers aufgerufen */
        void OnAdsNotification(object sender, AdsNotificationEventArgs e)
        {
            if (e.NotificationHandle == _notificationHandle)
            {
                TwinCAT.Ads.AdsState
                    plcState = (TwinCAT.Ads.AdsState) _statusReader
                        .ReadInt16(); /* Status wurde in den Stream geschrieben */
                PLCStatus = plcState.ToString();
                Log("TwinCAT PLC state changed to " + PLCStatus);
            }
        }

        /* Callback-Funktion: Wird bei Statusänderungen der SPS aufgerufen */
        void AmsRouterNotificationCallback(object sender, AmsRouterNotificationEventArgs e)
        {
            RouterStatus = e.State.ToString();
        }

        void CloseThread()
        {
            if (_adsClient == null)
                return;
            try
            {
                if (_adsClient.Disposed != true)
                {
                    _adsClient.AdsNotification -= OnAdsNotification;
                    _adsClient.AmsRouterNotification -= AmsRouterNotificationCallback;
                    _adsClient.DeleteDeviceNotification(_notificationHandle);
                    if (_handles != null)
                    {
                        foreach (var handle in _handles.ToArray())
                        {
                            _adsClient.DeleteDeviceNotification(handle.Key);
                            _handles.Remove(handle.Key);
                        }
                    }
                }
                Log("TwinCAT interface is disconnected from " + NetId);
                ConnectionStatus = "Disconnected";
            }
            catch (TwinCAT.Ads.AdsException ex)
            {
                Warning(ex.Message, this);
            }

            try
            {
                _adsClient.Dispose();
            }
            catch (TwinCAT.Ads.AdsException ex)
            {
                Warning(ex.Message, this);
            }

            RouterStatus = "";
            PLCStatus = "";
        }
        
        private void PrepareBlockReadWrite()
        {
            varinforead = new List<VariableInfo>();
            if (DebugMode)
                Debug.Log("TwinCAT - Prepare BlockReadWrite");

            _handles = new Dictionary<int, InterfaceSignal>();
            List<VariableInfo> varinfo = new List<VariableInfo>();
            foreach (TwinCATSignal signal in TwinCATSignals)
            {
                if (signal.Signal.GetStatusConnected())
                {
                    try
                    {
                        var handle = 0;

                        handle = _adsClient.CreateVariableHandle(signal.SymbolName);


                        signal.indexGroup = (int) AdsReservedIndexGroups.SymbolValueByHandle;
                        signal.indexOffset = handle;
                        signal.length = GetTypeLength(signal.OriginDataType);
                    }
                    catch (TwinCAT.Ads.AdsException ex)
                    {
                        Warning(ex.Message, this);
                    }

                    if (signal.Direction == InterfaceSignal.DIRECTION.INPUT)
                    {
                        TwinCATInputSignals.Add(signal);
                        NumberInputs++;
                    }

                    if (signal.Direction == InterfaceSignal.DIRECTION.OUTPUT)
                    {
                        TwinCATOutputSignals.Add(signal);
                        NumberOutputs++;
                    }
                }
            }

            try
            {
                _adsClient.AdsNotification += OnADSSignalChanged;
            }
            catch (TwinCAT.Ads.AdsException ex)
            {
                Warning(ex.Message, this);
            }
            if (DebugMode)
                Debug.Log($"TwinCAT -  BlockReadWrite for {NumberInputs} Inputs and {NumberOutputs} Outputs prepared");
        }

        private void BlockRead(int from, int to)
        {
            // Allocate memory
            var numberoutputs = to - from + 1;
            int rdLength = numberoutputs * 4;
            int wrLength = numberoutputs * 12;
            BinaryWriter writer = new BinaryWriter(new AdsStream(wrLength));
            // Write data for handles into the ADS Stream
            for (int i = from; i < to + 1; i++)
            {
                var signal = TwinCATOutputSignals[i - 1];
                writer.Write(signal.indexGroup);
                writer.Write(signal.indexOffset);
                writer.Write(signal.length);
                rdLength += signal.length;
            }
            // Sum command to read variables from the PLC
            AdsStream rdStream = new AdsStream(rdLength);
            _adsClient.ReadWrite(0xF080, numberoutputs, rdStream, (AdsStream) writer.BaseStream);

            BinaryReader reader = new BinaryReader(rdStream);
            // Check Status
            for (int i = from; i < to + 1; i++)
            {
                var signal = TwinCATOutputSignals[i - 1];
                int error = reader.ReadInt32();
                if (error != (int) AdsErrorCode.NoError)
                    ThreadStatus = ($"Unable to read variable [{signal.Name} [Error = {error}]");
            }

            for (int i = from; i < to + 1; i++)
            {
                var signal = TwinCATOutputSignals[i - 1];
                ReadFromADSStream(signal, reader);
            }
        }

        private void BlockWrite(int from, int to)
        {
            // Allocate memory
            var numberinputs = to - from + 1;
            int rdLength = numberinputs * 4;
            var varmemory = 0;
            for (int i = from; i < to + 1; i++)
            {
                var signal = TwinCATInputSignals[i - 1];
                varmemory += signal.length;
            }

            int wrLength = numberinputs * 12 + varmemory;
            BinaryWriter writer = new BinaryWriter(new AdsStream(wrLength));
            // Write data for handles into the ADS Stream
            for (int i = from; i < to + 1; i++)
            {
                var signal = TwinCATInputSignals[i - 1];
                writer.Write(signal.indexGroup);
                writer.Write(signal.indexOffset);
                writer.Write(signal.length);
                rdLength += signal.length;
            }

            // Write data to send to PLC behind the structure
            for (int i = from; i < to + 1; i++)
            {
                var signal = TwinCATInputSignals[i - 1];
                WriteToADSSteam(signal, writer);
            }

            AdsStream rdStream = new AdsStream(rdLength);
            int error = _adsClient.ReadWrite(0xF081, numberinputs, rdStream, (AdsStream) writer.BaseStream);
        }
        //! Arranges the signal in a structure in CreateSubFolders is true. For all names before "." a folder is generated
        private void ArrangeSignals()
        {
            if (!CreateSubFolders)
                return;
            var signals = GetComponentsInChildren<Signal>();
            foreach (var signal in signals)
            {
                var name = signal.Name;
                int index;
                string folder, newname;
                if (name.Contains('.'))
                {
                    index = name.IndexOf('.');
                    if (index > 0)
                    {
                        folder = name.Substring(0, index);
                        newname = name.Substring(index + 1);
                        var obj = GetChildByName(folder);
                        if (obj == null)
                        {
                            obj = new GameObject(folder);
                            obj.transform.parent = gameObject.transform;
                        }

                        signal.gameObject.transform.parent = obj.transform;
                        signal.gameObject.name = newname;
                    }
                }
            }
        }

#if UNITY_EDITOR
        [Button("Delete all signals")]
        private void DeleteAllSignals()
        {
            if (EditorUtility.DisplayDialog("Are you sure?", "Do you want to delete all Signals?", "YES", "CANCEL"))
                DestroyAllSignals();
        }
#endif


        [Button("Import signals")]
        private void Import()
        {
            importsignals = true;
            ImportSignals(false);
            importsignals = false;
            ConnectionStatus = "";
            RouterStatus = "";
            PLCStatus = "";
        }

        //! Gets the length in Bytes of the Beckhoff Datatypes
        private int GetTypeLength(string type)
        {
            var length = 0;
            switch (type)
            {
                case "BOOL":
                    length = 1;
                    break;
                case "BYTE":
                    length = 1;
                    break;
                case "WORD":
                    length = 2;
                    break;
                case "DWORD":
                    length = 4;
                    break;
                case "SINT":
                    length = 1;
                    break;
                case "USINT":
                    length = 1;
                    break;
                case "INT":
                    length = 2;
                    break;
                case "UINT":
                    length = 2;
                    break;
                case "UDINT":
                    length = 4;
                    break;
                case "DINT":
                    length = 4;
                    break;
                case "REAL":
                    length = 4;
                    break;
                case "LREAL":
                    length = 8;
                    break;
            }

            return length;
        }


        //! Subscribes to the TwinCAT outputs and inputs on Simulation start.
        //! PLC outputs are subscribed on TwinCAT ADS side - to write to Game4Automation Signal in consequence
        //! PLC inputs are subscribed on Game4Automation Signal - to write to ADS in consequence
        private void SubscribeOutputsAndInputs()
        {
            if (DebugMode)
                Debug.Log("TwinCAT - Subsribe Outputs and Inputs");
            _handles = new Dictionary<int, InterfaceSignal>();
            CalculateStreamLength();
            _dataStream = new AdsStream(StreamLength);
            _binaryReader = new BinaryReader(_dataStream);
            int offset = 0;
            int length = 0;
            var outputs = 0;
            var inputs = 0;
            var mode = AdsTransMode.OnChange;
            if (UpdateMode == updatemode.Cyclic)
                mode = AdsTransMode.Cyclic;

            foreach (var signal in TwinCATSignals)
            {
                if (signal.Signal.GetStatusConnected())
                {
                    // Subscribe for Outputs on ADS
                    if (signal.Direction == InterfaceSignal.DIRECTION.OUTPUT)
                    {
                        try
                        {
                            length = GetTypeLength(signal.OriginDataType);
                            outputs++;
                            var handle = _adsClient.AddDeviceNotification(signal.SymbolName, _dataStream, offset,
                                length,
                                mode, PLCOutputsMinUpdateCycleMs, PLCOutputsMaxUpdateDelay, null);
                            _handles.Add(handle, signal);
                            offset = offset + length;
                        }
                        catch (TwinCAT.Ads.AdsException ex)
                        {
                            Warning(ex.Message, this);
                        }
                    }

                    // Subscribe for Inputs on Game4Automation Signal
                    if (signal.Direction == InterfaceSignal.DIRECTION.INPUT)
                    {
                        inputs++;
                        signal.Signal.SignalChanged += OnPLCInputSignalChanged;
                    }
                }
            }

            try
            {
                _adsClient.AdsNotification += OnADSSignalChanged;
            }
            catch (TwinCAT.Ads.AdsException ex)
            {
                Warning(ex.Message, this);
            }

            NumberInputs = inputs;
            NumberOutputs = outputs;
            if (DebugMode)
                Debug.Log($"TwinCAT - Subscriptions to {outputs} plc outputs and {inputs} plc inputs created");
        }


        private void WriteToADSSteam(InterfaceSignal signal, BinaryWriter writer)
        {
            switch (signal.OriginDataType)
            {
                case "BOOL":
                    plcinputbool = (PLCInputBool) signal.Signal;
                    plcinputbool.Status.Connected = true;
                    writer.Write(plcinputbool.Value);
                    break;
                case "BYTE":
                    plcinputint = (PLCInputInt) signal.Signal;
                    plcinputint.Status.Connected = true;
                    writer.Write((byte) plcinputint.Value);
                    break;
                case "WORD":
                    plcinputint = (PLCInputInt) signal.Signal;
                    plcinputint.Status.Connected = true;
                    writer.Write((short) plcinputint.Value);
                    break;
                case "DWORD":
                    plcinputint = (PLCInputInt) signal.Signal;
                    plcinputint.Status.Connected = true;
                    writer.Write((int) plcinputint.Value);
                    break;
                case "SINT":
                    plcinputint = (PLCInputInt) signal.Signal;
                    plcinputint.Status.Connected = true;
                    writer.Write((sbyte) plcinputint.Value);
                    break;
                case "USINT":
                    plcinputint = (PLCInputInt) signal.Signal;
                    plcinputint.Status.Connected = true;
                    writer.Write((byte) plcinputint.Value);
                    break;
                case "INT":
                    plcinputint = (PLCInputInt) signal.Signal;
                    plcinputint.Status.Connected = true;
                    writer.Write((short) plcinputint.Value);
                    break;
                case "UINT":
                    plcinputint = (PLCInputInt) signal.Signal;
                    plcinputint.Status.Connected = true;
                    writer.Write((ushort) plcinputint.Value);
                    break;
                case "UDINT":
                    plcinputint = (PLCInputInt) signal.Signal;
                    plcinputint.Status.Connected = true;
                    writer.Write((uint) plcinputint.Value);
                    break;
                case "DINT":
                    plcinputint = (PLCInputInt) signal.Signal;
                    plcinputint.Status.Connected = true;
                    writer.Write((int) plcinputint.Value);
                    break;
                case "REAL":
                    plcinputfloat = (PLCInputFloat) signal.Signal;
                    plcinputfloat.Status.Connected = true;
                    writer.Write((float) plcinputfloat.Value);
                    break;
                case "LREAL":
                    plcinputfloat = (PLCInputFloat) signal.Signal;
                    plcinputfloat.Status.Connected = true;
                    writer.Write((double) plcinputfloat.Value);

                    break;
            }
        }

        private void ReadFromADSStream(InterfaceSignal signal, BinaryReader reader)
        {
            switch (signal.OriginDataType)
            {
                case "BOOL":
                    plcoutputbool = (PLCOutputBool) signal.Signal;
                    plcoutputbool.Value = reader.ReadBoolean();
                    plcoutputbool.Status.Connected = true;
                    break;
                case "BYTE":
                    plcoutputint = (PLCOutputInt) signal.Signal;
                    plcoutputint.Value = reader.ReadByte();
                    plcoutputint.Status.Connected = true;
                    break;
                case "WORD":
                    plcoutputint = (PLCOutputInt) signal.Signal;
                    plcoutputint.Value = reader.ReadUInt16();
                    plcoutputint.Status.Connected = true;
                    break;
                case "DWORD":
                    plcoutputint = (PLCOutputInt) signal.Signal;
                    plcoutputint.Value = (int) reader.ReadUInt32();
                    plcoutputint.Status.Connected = true;
                    break;
                case "SINT":
                    plcoutputint = (PLCOutputInt) signal.Signal;
                    plcoutputint.Value = (int) reader.ReadSByte();
                    plcoutputint.Status.Connected = true;
                    break;
                case "USINT":
                    plcoutputint = (PLCOutputInt) signal.Signal;
                    plcoutputint.Value = (int) reader.ReadByte();
                    plcoutputint.Status.Connected = true;
                    break;
                case "INT":
                    plcoutputint = (PLCOutputInt) signal.Signal;
                    plcoutputint.Value = (int) reader.ReadInt16();
                    plcoutputint.Status.Connected = true;
                    break;
                case "UINT":
                    plcoutputint = (PLCOutputInt) signal.Signal;
                    plcoutputint.Value = (int) reader.ReadUInt16();
                    plcoutputint.Status.Connected = true;
                    break;
                case "UDINT":
                    plcoutputint = (PLCOutputInt) signal.Signal;
                    plcoutputint.Value = (int) reader.ReadUInt32();
                    plcoutputint.Status.Connected = true;
                    break;
                case "DINT":
                    plcoutputint = (PLCOutputInt) signal.Signal;
                    plcoutputint.Value = (int) reader.ReadInt32();
                    plcoutputint.Status.Connected = true;
                    break;
                case "REAL":
                    plcoutputfloat = (PLCOutputFloat) signal.Signal;
                    plcoutputfloat.Value = reader.ReadSingle();
                    plcoutputfloat.Status.Connected = true;
                    break;
                case "LREAL":
                    plcoutputfloat = (PLCOutputFloat) signal.Signal;
                    plcoutputfloat.Value = (float) reader.ReadDouble();
                    plcoutputfloat.Status.Connected = true;
                    break;
            }
        }

        private void ReadFromADSStreamToBuffer(InterfaceSignal signal, BinaryReader reader)
        {
            var read = new ReadBufferItem();
            switch (signal.OriginDataType)
            {
                case "BOOL":
                    read.signal = (PLCOutputBool) signal.Signal;
                    read.BoolValue = reader.ReadBoolean();
                    break;
                case "BYTE":
                    read.signal = (PLCOutputInt) signal.Signal;
                    read.IntValue = reader.ReadByte();
                    break;
                case "WORD":
                    read.signal = (PLCOutputInt) signal.Signal;
                    read.IntValue = reader.ReadUInt16();
                    break;
                case "DWORD":
                    read.signal = (PLCOutputInt) signal.Signal;
                    read.IntValue = (int) reader.ReadUInt32();
                    break;
                case "SINT":
                    read.signal = (PLCOutputInt) signal.Signal;
                    read.IntValue = (int) reader.ReadSByte();
                    break;
                case "USINT":
                    read.signal = (PLCOutputInt) signal.Signal;
                    read.IntValue = (int) reader.ReadByte();
                    break;
                case "INT":
                    read.signal = (PLCOutputInt) signal.Signal;
                    read.IntValue = (int) reader.ReadInt16();
                    break;
                case "UINT":
                    read.signal = (PLCOutputInt) signal.Signal;
                    read.IntValue = (int) reader.ReadUInt16();
                    break;
                case "UDINT":
                    read.signal = (PLCOutputInt) signal.Signal;
                    read.IntValue = (int) reader.ReadUInt32();
                    break;
                case "DINT":
                    read.signal = (PLCOutputInt) signal.Signal;
                    read.IntValue = (int) reader.ReadInt32();
                    break;
                case "REAL":
                    read.signal = (PLCOutputFloat) signal.Signal;
                    read.FloatValue = reader.ReadSingle();
                    break;
                case "LREAL":
                    read.signal = (PLCOutputFloat) signal.Signal;
                    read.FloatValue = (float) reader.ReadDouble();
                    break;
            }

            lock (readbuffer)
            {
                readbuffer.Add(read);
            }
        }

        private void OnADSSignalChanged(object sender, AdsNotificationEventArgs e)
        {
            if (!isrunning)
                return;
            var handle = e.NotificationHandle;
            var signal = _handles[handle];
            e.DataStream.Position = e.Offset;
            /* if (DebugMode)
                Debug.Log("TwinCAT - OnADSSignalchanged " + signal.Name);*/
            ReadFromADSStreamToBuffer(signal, _binaryReader);
        }

        private void ADSWriteSymbol(InterfaceSignal signal)
        {
            if (!IsConnected)
                return;
            if (RouterStatus == "Stop")
                return;
            try
            {
                switch (signal.OriginDataType)
                {
                    case "SINT":
                        _adsClient.WriteSymbol(signal.SymbolName, System.Convert.ToSByte(signal.Signal.GetValue()),
                            false);
                        break;
                    default:
                        _adsClient.WriteSymbol(signal.SymbolName, signal.Signal.GetValue(), false);
                        break;
                }
            }
            catch (Exception e)
            {
                signal.Signal.SetStatusConnected(false);
                if (e.Message.Contains("Port is disabled"))
                    SetAllSignalStatus(false);
                Error(signal.Name + " " + e.Message, this);
            }
        }

        private void OnPLCInputSignalChanged(Signal signal)
        {
            if (IsConnected)
                ADSWriteSymbol(signal.GetInterfaceSignal());
        }

        //! Reads one ADS Signal
        private void ReadADSSignal(InterfaceSignal signal)
        {
            try
            {
                var handle = _adsClient.CreateVariableHandle(signal.SymbolName);
                var dataStream = new AdsStream(64);
                var reader = new BinaryReader(dataStream);
                _adsClient.Read(handle, dataStream);
                dataStream.Position = 0;
                ReadFromADSStream(signal, reader);
                dataStream.Dispose();
                signal.Signal.SetStatusConnected(true);
                _adsClient.DeleteVariableHandle(handle);
            }
            catch (TwinCAT.Ads.AdsException ex)
            {
                signal.Signal.SetStatusConnected(false);
                Warning("Error reading Signal " + signal.SymbolName + " " + ex.Message, this);
            }
        }


        private void CalculateStreamLength()
        {
            foreach (var signal in InterfaceSignals)
            {
                if (signal.Direction == InterfaceSignal.DIRECTION.OUTPUT)
                {
                    StreamLength = StreamLength + GetTypeLength(signal.OriginDataType);
                }
            }
        }


        //! Reads all ADS Signals for first init
        private void ReadAllADSSignals()
        {
            if (DebugMode)
                Debug.Log("TwinCAT - Read all Signals");
            StreamLength = 0;
            foreach (var signal in InterfaceSignals)
            {
                if (signal.Direction == InterfaceSignal.DIRECTION.OUTPUT)
                {
                    StreamLength = StreamLength + GetTypeLength(signal.OriginDataType);
                    ReadADSSignal(signal);
                }
            }
        }

        //! Writes all ADS Signals for first init
        private void WriteAllADSSignals()
        {
            if (DebugMode)
                Debug.Log("TwinCAT - Write all Signals");
            foreach (var signal in InterfaceSignals)
            {
                if (signal.Direction == InterfaceSignal.DIRECTION.INPUT)
                {
                    ADSWriteSymbol(signal);
                }
            }
        }

        private void SetAllReadBufferSignals()
        {
            lock (readbuffer)
            {
                while (readbuffer.Count > 0)
                {
                    var read = readbuffer[0];
                    readbuffer.RemoveAt(0);
                    var type = read.signal.GetType();
                    if (type == typeof(PLCOutputFloat))
                    {
                        var sig = (PLCOutputFloat) read.signal;
                        sig.Value = read.FloatValue;
                    }

                    if (type == typeof(PLCOutputBool))
                    {
                        var sig = (PLCOutputBool) read.signal;
                        sig.Value = read.BoolValue;
                    }

                    if (type == typeof(PLCOutputInt))
                    {
                        var sig = (PLCOutputInt) read.signal;
                        sig.Value = read.IntValue;
                    }
                }
            }
        }

        void Update()
        {
            if (!isrunning)
                isrunning = true;
            if (lastrouterstatus != RouterStatus)
            {
                Log("TwinCAT Router state changed to " + RouterStatus);
                if (RouterStatus == "Stop")
                {
                    CloseInterface();
                    reconnecting = true;
                    OnDisconnected();
                }

                if (RouterStatus == "Start")
                    OnConnected();
            }

            if (reconnecting && (Time.time - _lastreconnect) > 1)
                OpenInterface();

            lastrouterstatus = RouterStatus;
        }

        void FixedUpdate()
        {
            if (UpdateMode != updatemode.CyclicSumCommand)
            {
                // Set all event based signals which are now in the read buffer in LateUpdate to guarantee that they are all set in FixedUpdate Methods for Drives and so on.
                SetAllReadBufferSignals();
            }
        }

        private void Reset()
        {
            ChangeUpdateMode();
        }

        void ChangeUpdateMode()
        {
            if (UpdateMode != updatemode.CyclicSumCommand)
            {
                NoThreading = true;
            }
            else
            {
                NoThreading = false;
            }
        }

        // Use this for initialization
        new void Awake()
        {
            if (DebugMode)
                Debug.Log("TwinCAT - starting Awake in TwinCAT interface");
            _adsClient = null;
            _dataStream = null;
            _statusStream = null;
            _binaryReader = null;
            _statusReader = null;
            lastrouterstatus = "";
            readbuffer.Clear();
            if (_handles != null)
                _handles.Clear();
            base.Awake();
        }

        #endregion
    }
}