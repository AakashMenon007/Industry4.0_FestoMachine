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
    //!  Shared memory interface for an interface based on Siemens Simit shared memory structure (see Simit documentation)
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/simit-shared-memory")]
    public class SharedMemoryInterface : InterfaceSHMBaseClass
    {
        public string SHMName; //!< Name of the Shared memory

        [ReadOnly]
        public string
            Status; //!< Status of the interface during simulation "SHM connected" if everythin is OK (ReadOnly)
        public int KeepAliveSeconds = 5;
        [ReadOnly] public int Version; //!< Version of the interface (ReadOnly)
        [ReadOnly] public int Cycle; //!< The cycle in ms the interface signals are written by Simit (ReadOnly)
        [ReadOnly] public int CycleCounter; //!< The counter of the current cycle  (ReadOnly)
        [ReadOnly] public string MutexName; //!< The Simit Mutex name (ReadOnly)
        [ReadOnly] public int size; //!< The size of the shared memory (ReadOnly)
        [ReadOnly] public int headersize; //!< The headersize of the shared memory (ReadOnly)
        public List<SHMSignal> Signals; //!< The signals in the shared memory
        private int mutexlength;
        private int offset;
        private MemoryMappedFile file;
        private MemoryMappedViewAccessor accessor;
        private float _lastupdate;
        private int _lastcyclecounter;
        private float _lastreconnect;
        private Mutex localMutex;
        private bool reconnecting;

        //!< Imports all Signal from the shared memory and generates as sub gameobjects signal objects if not already existing.
        public void ImportSignals(bool simstart)
        {
            SetAllSignalStatus(false);
            Signals = new List<SHMSignal>();
            if (SHMName == "")
            {
                Log("SHM Interface inactive because of empty name", this);
                Status = "no name - inactive";
                return;
            }

            try
            {
                /// HEADERmemory = {byte[]} byte[100] 
                file = MemoryMappedFile.OpenExisting(SHMName);
                accessor = file.CreateViewAccessor();
                size = (int) accessor.ReadUInt32(0);
                headersize = (int) accessor.ReadUInt32(4);
                Version = (int) accessor.ReadUInt16(8);
                Cycle = (int) accessor.ReadUInt32(10);
                mutexlength = (int) accessor.ReadByte(16);
                MutexName = ReadString(accessor, 17, mutexlength);
                offset = 17 + mutexlength;
                byte[] memory = new byte[size];
                accessor.ReadArray(0, memory, 0, size);
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

                Status = "Connected";
                OnConnected();
            }
            catch (FileNotFoundException)
            {
                OnDisconnected();
                Error("SHM File not existing", gameObject);
                Status = "SHM File not existing - reconnecting";
                return;
            }
        }

        //!<
         void UpdateSignals()
        {
            if (accessor == null)
            {
                Status = "SHM File not existing";
                if (Time.realtimeSinceStartup - _lastreconnect > 5)
                {
                    // Disable Signal Status
                    foreach (SHMSignal signal in Signals)
                    {
                        signal.signal.SetStatusConnected(false);
                    }

                    Status = "SHM File not existing - reconnecting";
                    reconnecting = true;
                    CloseInterface();
                    _lastreconnect = Time.realtimeSinceStartup;
                }

                return;
            }

            try
            {
                localMutex = Mutex.OpenExisting(MutexName);
            }
            catch (Exception e)
            {
                Status = "SHM File not existing - reconnecting";
                reconnecting = true;
                CloseInterface();
                _lastreconnect = Time.realtimeSinceStartup;
            }

            localMutex.WaitOne();
            byte valbyte = 0;
            int valint = 0;
            float valfloat = 0;


            PLCOutputInt plcoutputint = null;
            PLCOutputBool plcoutputbool = null;
            PLCOutputFloat plcoutputfloat = null;
            PLCInputInt plcinputint = null;
            PLCInputFloat plcinputfloat = null;
            CycleCounter = (int) accessor.ReadUInt16(14);
            if (CycleCounter != _lastcyclecounter)
            {
                Status = "Connected";
                if (IsConnected==false)
                    OnConnected();
            }
            else
            {
                if ((Time.realtimeSinceStartup - _lastupdate) > KeepAliveSeconds)
                {
                    OnDisconnected();
                }
            }

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

            _lastupdate = Time.realtimeSinceStartup;
            _lastcyclecounter = CycleCounter;
            localMutex.ReleaseMutex();
        }

        //!<
        public override void OpenInterface()
        {
            ImportSignals(true);
        }
        
        public void ImportSignals()
        {
            ImportSignals(false);
        }


        // Use this for initialization
        void Start()
        {
            OpenInterface();
        }
        
        void FixedUpdate()
        {
            if (reconnecting && (Time.time-_lastreconnect)>1)
                OpenInterface();
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