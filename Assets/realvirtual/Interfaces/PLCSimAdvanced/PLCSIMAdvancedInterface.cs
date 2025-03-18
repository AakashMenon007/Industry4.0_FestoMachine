// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license  
// Test    

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using System;
using Debug = UnityEngine.Debug;

#if UNITY_STANDALONE_WIN
namespace realvirtual
{
    [System.Serializable]
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/plcsim-advanced")]
    public class PLCSIMAdvancedInterface : InterfaceThreadedBaseClass
    {
        public string InstanceName;
        [HideInInspector] public string RemoteAdress = "";
        public float SyncCycleMs = 10;
        public float SyncPointMs = -1;
        private bool StartStopWithUnity = false;
        public bool PauseWithUnity = false;
        public bool HMIOnly = false;
        public bool DebugMode = false;
        

        public string SiemensDLL =
            "C:/Program Files (x86)/Common Files/Siemens/PLCSIMADV/API/2.0/Siemens.Simatic.Simulation.Runtime.Api.x86.dll";

        [ReadOnly] public string PLCStatus = "Not connected";
        private string Status;
        private int Version;
        [HideInInspector] public int Cycle;
        [ReadOnly] public int PlcSimCycleCounter;
        private int size;
        private int headersize;
        [ReadOnly] public float PlcSimSeconds;
        private byte plcstatus;
        public float plcscale;
        private byte plcconfig;
        public List<SHMSignal> Signals;
        private int mutexlength;
        private int offset;
        private MemoryMappedFile file;
        private MemoryMappedViewAccessor accessor;
        private float _lastupdate;
        private int _lastcyclecounter;
        private float _lastreconnect;
        private Process process;
        private int UnityStatus = 0; // 0 stopped, 1 playing, 2 paused
        private bool readsignals = false;
        private string dll;
        private string MutexName;
        private Mutex localMutex;
        private bool SignalsImported = false;


#if UnityEditor
         PLCSIMAdvancedInterface()
        {
            EditorApplication.pauseStateChanged += PauseStateEvent;
        }
#endif

   

        //! Sets all signals to connected
        public void SetAllSignalsNotConneted()
        {
            Signal[] signals = GetComponentsInChildren<Signal>();
            for (int i = 0; i < signals.Length; i++)
            {
                signals[i].SetStatusConnected(false);
            }
        }

        protected string ReadString(MemoryMappedViewAccessor accessor, long pos, int size)
        {
            string res = "";

            byte[] buffer = new byte[size];
            int count = accessor.ReadArray<byte>(pos, buffer, 0, (byte) size);
            if (count == size)
            {
                res = Encoding.Default.GetString(buffer);
            }

            return res;
        }

        public void ImportPLCSimSignals()
        {
            SetAllSignalsNotConneted();
            Signals = new List<SHMSignal>();
            if (InstanceName == "")
            {
                Log("PLCSIM  Interface inactive because of empty instance name", this);
                Status = "no name - inactive";
                PLCStatus = "Not connected";
                return;
            }

            try
            {
                /// HEADER
                file = MemoryMappedFile.OpenExisting("PLCSimAdvanced-" + InstanceName);
                accessor = file.CreateViewAccessor();
                size = (int) accessor.ReadUInt32(0);
                headersize = (int) accessor.ReadUInt32(4);
                Version = (int) accessor.ReadUInt32(8);
                Cycle = (int) accessor.ReadUInt32(10);
                PlcSimCycleCounter = accessor.ReadUInt16(14);
                PlcSimSeconds = accessor.ReadSingle(16); // 16-19
                plcstatus = accessor.ReadByte(20); // 20
                plcscale = accessor.ReadSingle(21); // 21-24
                plcconfig = accessor.ReadByte(25); //25
                // Simscale 26-29
                // SimStatus 30
                accessor.Write(30, (byte) UnityStatus);
                // Reserve 33-49
                mutexlength = (int) accessor.ReadByte(50);
                MutexName = ReadString(accessor, 51, mutexlength);
                offset = 51 + mutexlength;

                // SIGNALS
                var name = "";
                var namelength = 0;
                var curroffset = offset;
                var finished = false;
                var signalpos = 0;
                var io = 0;
                byte type = 0;
                SIGNALTYPE signaltype;
                SIGNALDIRECTION signaldirection = SIGNALDIRECTION.NOTDEFDINED;
                byte signalbit = 0;
                do
                {
                    namelength = accessor.ReadByte(curroffset);
                    if (namelength > 0)
                    {
                        curroffset++;

                        name = ReadString(accessor, curroffset, namelength);
                        curroffset = curroffset + namelength;

                        signalpos = (int) accessor.ReadUInt32(curroffset);
                        curroffset = curroffset + 4;

                        io = accessor.ReadByte(curroffset);
                        switch (io)
                        {
                            case -1:
                                signaldirection = SIGNALDIRECTION.NOTDEFDINED;
                                break;
                            case 0:
                                signaldirection = SIGNALDIRECTION.INPUT;
                                break;
                            case 1:
                                signaldirection = SIGNALDIRECTION.OUTPUT;
                                break;
                        }

                        curroffset++;
                        type = accessor.ReadByte(curroffset);
                        curroffset++;
                        signaltype = SIGNALTYPE.UNDEDIFINED;
                        if ((type >= 0) && (type <= 7))
                        {
                            signalbit = type;
                            signaltype = SIGNALTYPE.BOOL;
                        }
                        else
                        {
                            signalbit = 0;
                            switch (type)
                            {
                                case 8:
                                    signaltype = SIGNALTYPE.BYTE;
                                    break;
                                case 9:
                                    signaltype = SIGNALTYPE.WORD;
                                    break;
                                case 10:
                                    signaltype = SIGNALTYPE.INT;
                                    break;
                                case 11:
                                    signaltype = SIGNALTYPE.DWORD;
                                    break;
                                case 12:
                                    signaltype = SIGNALTYPE.DINT;
                                    break;
                                case 13:
                                    signaltype = SIGNALTYPE.REAL;
                                    break;
                            }
                        }

                        /// Create Signals Object if not existing
                        Signal signal = CreateSignalObject(name, signaltype, signaldirection);
                        // Add it to the Active Signals list
                        var shm = new SHMSignal();
                        shm.name = name;
                        shm.bit = signalbit;
                        shm.mempos = headersize + signalpos;
                        shm.signal = signal;
                        shm.direction = signaldirection;
                        shm.type = signaltype;
                        Signals.Add(shm);
                    }
                    else
                    {
                        finished = true;
                    }
                } while (finished == false);

                SignalsImported = true;
                Status = "SHM connected";
            }
            catch (FileNotFoundException)
            {
                Status = "SHM File not existing - reconnecting";
                PLCStatus = "Not connected";
                return;
            }
        }


        public void UpdateSignals()
        {
            if (MutexName != null)
            {
                try
                {
                    localMutex = Mutex.OpenExisting(MutexName);
                    localMutex.WaitOne();
                }
                catch (Exception e)
                {
                    var error = e;
                }
            }

            byte valbyte = 0;
            int valint = 0;
            float valfloat = 0;
            if (accessor == null)
            {
                return;
            }

            PLCOutputInt plcoutputint = null;
            PLCOutputBool plcoutputbool = null;
            PLCOutputFloat plcoutputfloat = null;
            PLCInputInt plcinputint = null;
            PLCInputFloat plcinputfloat = null;
            PlcSimCycleCounter = (int) accessor.ReadUInt16(14);
            PlcSimSeconds = accessor.ReadSingle(16); // 16-19
            plcstatus = accessor.ReadByte(20); // 20

            // Operationg States
            // 5 (Freeze) 6 = running
            // 3 = stop
            // 7,2 MRES
            // 0 = PLCSim not running    
            // 1 = starting

            switch (plcstatus)
            {
                case 0:
                    PLCStatus = "Not connected";
                    break;
                case 1:
                    PLCStatus = "Powered off";
                    break;
                case 2:
                    PLCStatus = "Reseting";
                    break;
                case 3:
                    PLCStatus = "Stopped";
                    break;
                case 5:
                    PLCStatus = "Running";
                    break;
                case 6:
                    PLCStatus = "Running";
                    break;
                case 7:
                    PLCStatus = "Reseting";
                    break;
            }

            plcscale = accessor.ReadSingle(21); // 21-24
            plcconfig = accessor.ReadByte(25); //25
            // SimStatus 30
            if (StartStopWithUnity)
            {
                accessor.Write(30, (byte) UnityStatus);
            }

            if (PlcSimCycleCounter != _lastcyclecounter)
            {
                IsConnected = true;
            }
            else

                Status = "connected";

            foreach (SHMSignal signal in Signals)
            {
                // Only Update Signals which are active
                if (signal.signal.Settings.Active)
                {
                    if (signal.direction == SIGNALDIRECTION.OUTPUT)
                    {
                        switch (signal.type)
                        {
                            case SIGNALTYPE.BOOL:
                                valbyte = accessor.ReadByte(signal.mempos);
                                plcoutputbool = (PLCOutputBool) signal.signal;
                                plcoutputbool.Value = ((valbyte >> signal.bit) & 1) != 0;
                                plcoutputbool.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.INT:
                                valint = accessor.ReadInt16(signal.mempos);
                                plcoutputint = (PLCOutputInt) signal.signal;
                                plcoutputint.Value = valint;
                                plcoutputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.DINT:
                                valint = accessor.ReadInt32(signal.mempos);
                                plcoutputint = (PLCOutputInt) signal.signal;
                                plcoutputint.Value = valint;
                                plcoutputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.BYTE:
                                valint = accessor.ReadByte(signal.mempos);
                                plcoutputint = (PLCOutputInt) signal.signal;
                                plcoutputint.Value = valint;
                                plcoutputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.WORD:
                                valint = (int) accessor.ReadUInt16(signal.mempos);
                                plcoutputint = (PLCOutputInt) signal.signal;
                                plcoutputint.Value = valint;
                                plcoutputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.DWORD:
                                valint = (int) accessor.ReadUInt32(signal.mempos);
                                plcoutputint = (PLCOutputInt) signal.signal;
                                plcoutputint.Value = valint;
                                plcoutputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.REAL:
                                valfloat = accessor.ReadSingle(signal.mempos);
                                plcoutputfloat = (PLCOutputFloat) signal.signal;
                                plcoutputfloat.Value = valfloat;
                                plcoutputfloat.Status.Connected = IsConnected;
                                break;
                        }
                    }

                    if (signal.direction == SIGNALDIRECTION.INPUT)
                    {
                        switch (signal.type)
                        {
                            case SIGNALTYPE.BOOL:
                                byte byteval = 0;
                                bool valbool = ((PLCInputBool) signal.signal).Value;
                                valbyte = accessor.ReadByte(signal.mempos);
                                if (valbool == true)
                                {
                                    byteval = (byte) (valbyte | (byte) (0x01 << signal.bit));
                                }
                                else
                                {
                                    byteval = (byte) (valbyte & ~(1 << signal.bit));
                                }

                                accessor.Write(signal.mempos, (byte) byteval);
                                ((PLCInputBool) signal.signal).Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.INT:
                                plcinputint = (PLCInputInt) signal.signal;
                                accessor.Write(signal.mempos, (short) plcinputint.Value);
                                plcinputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.DINT:
                                plcinputint = (PLCInputInt) signal.signal;
                                accessor.Write(signal.mempos, plcinputint.Value);
                                plcinputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.BYTE:
                                plcinputint = (PLCInputInt) signal.signal;
                                accessor.Write(signal.mempos, (byte) plcinputint.Value);
                                plcinputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.WORD:
                                plcinputint = (PLCInputInt) signal.signal;
                                accessor.Write(signal.mempos, (ushort) plcinputint.Value);
                                plcinputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.DWORD:
                                plcinputint = (PLCInputInt) signal.signal;
                                accessor.Write(signal.mempos, (uint) plcinputint.Value);
                                plcinputint.Status.Connected = IsConnected;
                                break;
                            case SIGNALTYPE.REAL:
                                plcinputfloat = (PLCInputFloat) signal.signal;
                                accessor.Write(signal.mempos, (float) plcinputfloat.Value);
                                plcinputfloat.Status.Connected = IsConnected;
                                break;
                        }
                    }
                }
            }

            // _lastupdate = Time.realtimeSinceStartup;
            _lastcyclecounter = PlcSimCycleCounter;
            localMutex.ReleaseMutex();
        }

        public void StartCoupler()
        {
            if (process != null)
            {
                if (DebugMode)
                    Log($"Starting Coupler but Coupler Process is allready running - first killing process");
                StopCoupler();
            }
               
#if UNITY_EDITOR
            var path = Application.dataPath + "/realvirtual/private/Interfaces/PLCSimAdvanced/Ressources/PLCSimAdvancedCoupler.exe";
            if (!System.IO.File.Exists(path))
            {
                var ok = EditorUtility.DisplayDialog("Please Get PLCSimAdvancedCoupler.exe",
                    "You need to download PLCSimAdvancedCoupler.exe and place it into the \\PLCSimAdvanced\\Ressources\\ folder ",
                    "OK - open Download", "Cancel");
                if (ok)
                    Application.OpenURL("https://realvirtual.io/download/PLCSimAdvancedCoupler.exe");
                return;
            }


            var streamingAssets = Application.streamingAssetsPath;
            dll = System.IO.Path.Combine(streamingAssets, "Siemens.Simatic.Simulation.Runtime.Api.x86.dll");
            if (!Directory.Exists(streamingAssets))
            {
                Directory.CreateDirectory(streamingAssets);
            }

            try
            {
                FileUtil.CopyFileOrDirectory(SiemensDLL, dll);
            }
            catch
            {
            }

            ;


            var destexe = System.IO.Path.Combine(Application.streamingAssetsPath, "PLCSimAdvancedCoupler.exe");
            var exe = Application.dataPath +
                      "/realvirtual/private/Interfaces/PLCSimAdvanced/Ressources/PLCSimAdvancedCoupler.exe";

            try
            {
                FileUtil.DeleteFileOrDirectory(destexe);
            }
            catch
            {
            }

            ;

            try
            {
                FileUtil.CopyFileOrDirectory(exe, destexe);
            }
            catch
            {
            }

            ;

#endif

            UnityStatus = 1;
            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "PLCSimAdvancedCoupler.exe");
            if (DebugMode)
                Log($"PLCSIMAdvanced FilePath [{filePath}]");

            var isdebug = "nodebug";
            var ishmi = "all";
            
            if (HMIOnly)
                ishmi = "hmi";
            if (DebugMode)
                isdebug = "debug";
            
            var psi = new ProcessStartInfo(filePath)
            {
                // CreateNoWindow = true,
                // UseShellExecute = false,
                // WindowStyle = ProcessWindowStyle.Hidden,
                // RedirectStandardOutput = false,
                // RedirectStandardError = true,
                Arguments = InstanceName + " " + SyncCycleMs + " " + SyncPointMs + " " + isdebug + " " + ishmi
            };


            try
            {
                if (DebugMode)
                    Log($"Starting Process for PLCSIMAdvanced [{psi.FileName}] Arguments [{psi.Arguments}]");
                Debug.Log("");
                process = Process.Start(psi);
                Log("PLCSIM Advanced Coupler process started", this);
            }
            catch
            {
                Warning("PLCSIM Advanced Coupler can not be started", this);
            }
        }

        private void StopCoupler()
        {
            if (process != null)
            {
                try
                {
                    process.Kill();
                    Log("PLCSIM Advanced Coupler process terminated", this);
                }
                catch
                {
                    Warning("PLCSIM Advanced Coupler can not be terminated", this);
                }
            }
        }

        public void Start()
        {
            SignalsImported = false;
            process = null;
            StartCoupler();

            ImportSignals(true);
        }

        private void OnApplicationQuit()
        {
            CloseInterface();
            if (StartStopWithUnity == false)
                UnityStatus = 90;
            else
                if (PauseWithUnity)
                    UnityStatus = 0;
                else 
                    UnityStatus = 1;
            if (DebugMode)
                   Log("PLCSIM Advanced Coupler process stopped", this);
            if (accessor != null)
            {
                accessor.Write(30, (byte) UnityStatus);
            }
        }


#if UNITY_EDITOR
        private void PauseStateEvent(PauseState state)
        {
            if (state == PauseState.Paused)
            {
                UnityStatus = 2;
                if (accessor != null && PauseWithUnity)
                {
                    accessor.Write(30, (byte) UnityStatus);
                }
            }

            if (state == PauseState.Unpaused)
            {
                UnityStatus = 1;
                if (accessor != null)
                {
                    accessor.Write(30, (byte) UnityStatus);
                }
            }
        }
#endif

        public override void CloseInterface()
        {
            if (DebugMode)
                Log("PLCSIM Advanced Coupler Close Interface, Releasing Mutex", this);
            try
            {
                localMutex.ReleaseMutex();
            }
            catch
            {
            }
        }

        protected virtual void OnEditorUpdate()
        {
            if (Status != "SHM connected" && readsignals)
            {
                ImportPLCSimSignals();
            }

            if (readsignals && Status == "SHM connected" && readsignals)
            {
                ImportSignals(false);
            }
        }


        public void ImportSignals(bool simstart)
        {
            if (!simstart && readsignals == false)
            {
#if UNITY_EDITOR
                EditorApplication.update += OnEditorUpdate;
#endif
                StartCoupler();
                readsignals = true;
                return;
            }

            ImportPLCSimSignals();

            if (!simstart && readsignals)
            {
                StopCoupler();
                readsignals = false;
#if UNITY_EDITOR
                EditorApplication.update -= OnEditorUpdate;
#endif
            }
        }

        //! Updates all signals in the parallel communication thread
        protected override void CommunicationThreadUpdate()
        {
            if (SignalsImported)
                UpdateSignals();
        }


        public void Update()
        {
            if (SignalsImported == false)
            {
                ImportSignals(true);
            }

         
  
        }
    }
}
#endif