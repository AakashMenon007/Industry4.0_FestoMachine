// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

#if UNITY_STANDALONE_WIN
#pragma warning disable 0168
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using System.Threading;

namespace realvirtual
{
    //!  Interface to ABB Robot Studio
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/abb-robotstudio")]
    public class ABBRobotStudioInterface : InterfaceSHMBaseClass
    {
        public string SharedMemoryName="SIMITShared Memory"; //!< Name of the Shared memory

        [ReadOnly]
        public string
            Status; //!< Status of the interface during simulation "SHM connected" if everythin is OK (ReadOnly)
        public bool DebugMode = false; 
        [ReadOnly] public int CycleCounter; //!< The counter of the current cycle  (ReadOnly)
        [ReadOnly] public string MutexName; //!< The Simit Mutex name (ReadOnly)
        [ReadOnly] public int sizememoryarea; //!< The size of the shared memory (ReadOnly)
        [ReadOnly] public int sizeheaderarea; //!< The headersize of the shared memory (ReadOnly)
        public List<SHMSignal> Signals; //!< The signals in the shared memory
        private int mutexlength;
        private int offset;
        private MemoryMappedFile file;
        private MemoryMappedViewAccessor accessor;
        private float _lastreconnect;
        private Mutex localMutex;
        private float _lastupdate;
        private int _lastcyclecounter;

        //!< Imports all Signal from the shared memory and generates as sub gameobjects signal objects if not already existing.

        public void CreateSharedMemory()
        {
            var signals = GetComponentsInChildren<Signal>();
            MutexName = SharedMemoryName + "Mutex";
            bool mutexcreated = false;
            localMutex = new Mutex(true, MutexName, out mutexcreated);
            if (DebugMode)
                Debug.Log($"ABBRobotStudioInterface - New Mutex [{MutexName}]created");
            if (!mutexcreated)
            {
                localMutex.WaitOne();
            }
            Signals = new List<SHMSignal>();
            var lengthsignalheaderdescription = 0;
            var lengthsignalheaderdescriptions = 0;
            var lengthsignals = 0;

            foreach (var signal in signals)
            {
                var sizesignal = 0;
                if (signal.GetType() == typeof(PLCInputBool) || signal.GetType() == typeof(PLCOutputBool))
                {
                    sizesignal = 1;
                }
                if (signal.GetType() == typeof(PLCInputFloat) || signal.GetType() == typeof(PLCOutputFloat))
                {
                    sizesignal = 4;
                }
                if (signal.GetType() == typeof(PLCInputInt) || signal.GetType() == typeof(PLCOutputInt))
                {
                    sizesignal = 4;
                }
                lengthsignalheaderdescription = 1+signal.name.Length  + 4 + 1 + 1; // lengthsignalname(1) + signalname + adressoffsignal(4) + ioidentifier(1)+typeidentifier(1) 
                lengthsignalheaderdescriptions = lengthsignalheaderdescriptions + lengthsignalheaderdescription;
                lengthsignals = lengthsignals + sizesignal;
            }

            lengthsignalheaderdescriptions = lengthsignalheaderdescriptions + 1; // endidentifier of signal list
            sizeheaderarea = 17 + MutexName.Length + lengthsignalheaderdescriptions;
            sizememoryarea = lengthsignals;
            
            file = MemoryMappedFile.CreateOrOpen(SharedMemoryName,sizeheaderarea+sizememoryarea);
            if (DebugMode)
                Debug.Log($"ABBRobotStudioInterface - Shared Memory [{SharedMemoryName}] created with size {sizeheaderarea+sizememoryarea}");
            accessor = file.CreateViewAccessor();
            
            /// Header
            ///    if (DebugMode)
            Debug.Log($"ABBRobotStudioInterface - Shared Memory [{SharedMemoryName}] Size Memory is {sizememoryarea}");
            accessor.Write(0,(UInt32)sizememoryarea+sizeheaderarea);
            accessor.Write(4,(UInt32)sizeheaderarea);
            accessor.Write(8, (UInt16)0); // Version
            accessor.Write(10,(UInt32)Time.fixedDeltaTime*1000); // Scanning Cycle ms
            accessor.Write(14,(UInt16)0); // Cycle counter
            accessor.Write(16,(byte)MutexName.Length);
            WriteString(accessor,MutexName,17);
            
            // SignalList
            var posheader = 17 + MutexName.Length;
            var posmemory = 0;
            var bit = 0;
            var lastbit = false;
            foreach (var signal in signals)
            {
                var shm = new SHMSignal();
                // Namelength
                accessor.Write(posheader, (byte) signal.name.Length);
                posheader++;
                // Signalname
                WriteString(accessor, signal.name, posheader);
                posheader = posheader + signal.name.Length;
                // PosMemoryArea
                accessor.Write(posheader, (UInt32) posmemory);
                posheader = posheader + 4;
                /// Signaldirection
                if (signal.IsInput())
                {
                    shm.direction = SIGNALDIRECTION.INPUT;
                    accessor.Write(posheader, (byte) 1);
                }
                else
                {
                    shm.direction = SIGNALDIRECTION.OUTPUT;
                    accessor.Write(posheader, (byte) 0);
                }
                
                posheader++;

                // Add it to the Active Signals list
                SIGNALTYPE signaltype;
                shm.name = signal.name;
            
                shm.mempos = sizeheaderarea + posmemory;
                shm.signal = signal;
                /// Type
                if (signal.GetType() == typeof(PLCInputBool) || signal.GetType() == typeof(PLCOutputBool))
                {
                    signal.Comment = $"M{posmemory}.{bit}";
                    lastbit = true;
                    accessor.Write(posheader, (byte) bit);
                    shm.bit = (byte)bit;
                    bit++;
                    shm.type = SIGNALTYPE.BOOL;
                    if (bit > 7)
                    {
                        bit = 0;
                        lastbit = false;
                        posmemory = posmemory + 1;
                    }
                }
                if (signal.GetType() == typeof(PLCInputFloat) || signal.GetType() == typeof(PLCOutputFloat))
                {
                    if (lastbit)
                        posmemory = posmemory + 1;
                    bit = 0;
                    shm.bit = 0;
                    lastbit = false;
                    signal.Comment = $"MD{posmemory}";
                    accessor.Write(posheader, (byte) 13);
                 
                    shm.type = SIGNALTYPE.REAL;
                    posmemory = posmemory + 4;
                }
                if (signal.GetType() == typeof(PLCInputInt) || signal.GetType() == typeof(PLCOutputInt))
                {
                    if (lastbit)
                        posmemory = posmemory + 1;
                    bit = 0;
                    shm.bit = 0;
                    lastbit = false;
                    signal.Comment = $"MD{posmemory}";
                    accessor.Write(posheader, (byte) 10);
                    shm.type = SIGNALTYPE.INT;
                    posmemory = posmemory + 4;
                }
            
                Signals.Add(shm);
                if (DebugMode)
                    Debug.Log($"ABBRobotStudioInterface - Signal [{name}] created");
                posheader++;
            }

        
            accessor.Write(posheader,(byte)0);
            if (DebugMode)
                Debug.Log($"ABBRobotStudioInterface - Last Pos of header is {posheader}");
            byte[] memory = new byte[sizeheaderarea+sizememoryarea];
            accessor.ReadArray(0, memory, 0, sizeheaderarea+sizememoryarea);
            localMutex.ReleaseMutex();
            if (accessor != null)
                IsConnected = true;
        }

        void UpdateSignals()
        {
            if (accessor == null)
            {
                // Disable Signal Status
                    foreach (SHMSignal signal in Signals)
                    {
                        signal.signal.SetStatusConnected(false);
                    }
                    Status = "ABBRobotStudioInterface - Error SHM File not existing - reconnecting";
                    return;
            }
        
            localMutex.WaitOne();
            if (DebugMode)
                Debug.Log($"ABBRobotStudioInterface - Update Signals");
            byte valbyte = 0;
            int valint = 0;
            float valfloat = 0;
            PLCOutputInt plcoutputint = null;
            PLCOutputBool plcoutputbool = null;
            PLCOutputFloat plcoutputfloat = null;
            PLCInputInt plcinputint = null;
            PLCInputFloat plcinputfloat = null;
            CycleCounter++;
            accessor.Write(14,(UInt16)CycleCounter);

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

                        if (signal.name.StartsWith("Joint") && signal.type == SIGNALTYPE.REAL)
                        {
                            plcoutputfloat.Value = plcoutputfloat.Value * 180/Mathf.PI;
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

            _lastupdate = Time.realtimeSinceStartup;
            _lastcyclecounter = CycleCounter;
            localMutex.ReleaseMutex();
        }

        
        // Use this for initialization
        void Start()
        {
            CreateSharedMemory();
        }
        
        void FixedUpdate()
        {
            if (IsConnected)
                UpdateSignals();
        }

        public override void CloseInterface()
        {
            if (localMutex != null)
            {
                try
                {
                    localMutex.ReleaseMutex();
                }
                catch
                {
                }
            }

            if (file != null)
                file.Dispose();
            OnDisconnected();
        }
    }
}
#endif