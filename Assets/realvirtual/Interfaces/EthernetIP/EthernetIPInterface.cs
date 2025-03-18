// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System.Collections.Generic;
using UnityEngine;
using libplctag;
using libplctag.DataTypes;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NaughtyAttributes;
using UnityEditor;

namespace realvirtual
{
#pragma warning disable 4014
#pragma warning disable 1998
    
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/ethernetip")]
    public class EthernetIPSignal : InterfaceSignal
    {
        public Tag<RealPlcMapper, float> tagfloat;
        public Tag<BoolPlcMapper, bool> tagbool;
        public Tag<SintPlcMapper, sbyte> tagsint;
        public Tag<DintPlcMapper, int> tagdint;
        public Tag<IntPlcMapper, short> tagint;

        public void Init()
        {
            switch (OriginDataType)
            {
                case "REAL" : 
                   tagfloat.Initialize();
                   break;
                case "BOOL" :
                    if (Direction==DIRECTION.INPUT)
                    {
                        tagbool.Initialize();
                    } else
                    {
                        tagsint.Initialize();
                    }
   
                    break;
                case "SINT" : 
                    tagsint.Initialize();
                    break;
                case "INT":
                    tagint.Initialize();
                    break;
                case "DINT" : 
                    tagdint.Initialize();
                    break;
            }
        }
     
        public void SetValue()
        {
            switch (OriginDataType)
            {
                case "REAL" :
                    Signal.SetValue(tagfloat.Value);
                    break;
                case "BOOL" :
                    var val = tagsint.Value;
                    bool varbool = false;
                    if (val == 1)
                        varbool = true;
                    Signal.SetValue(varbool);
                    break;
                case "INT":
                    Signal.SetValue(tagint.Value);
                    break;
                case "SINT" :
                    Signal.SetValue(tagsint.Value);
                    break;
                case "DINT" :
                    Signal.SetValue(tagdint.Value);
                    break;
            }
        }
        
        public void GetValue()
        {
            switch (OriginDataType)
            {
                case "REAL" :
                    tagfloat.Value = (float)Signal.GetValue();
                    break;
                case "BOOL" :
                    tagbool.Value = (bool)Signal.GetValue();
                    break;
                case "INT":
                    tagint.Value = Convert.ToInt16((int)Signal.GetValue());
                    break;
                case "SINT" :
                    tagsint.Value = (sbyte)Signal.GetValue();
                    break;
                case "DINT" :
                    tagdint.Value = (int)Signal.GetValue();
                    break;
            }
        }


        public string GetStatus()
        {
            Status status = Status.Ok;
            switch (OriginDataType)
            {
                case "REAL" : 
                    status = tagfloat.GetStatus();
                    break;
                case "BOOL" :
                    if (Direction == DIRECTION.INPUT)
                    {
                        status = tagbool.GetStatus();
                    }
                    else
                    {
                        status = tagsint.GetStatus();
                    }
                    break;
                case "INT":
                    status = tagint.GetStatus();
                    break;
                case "SINT" : 
                    status = tagsint.GetStatus();
                    break;
                case "DINT" : 
                    status = tagdint.GetStatus();
                    break;
            }

            return status.ToString();
        }
    }

    public class EthernetIPInterface : InterfaceThreadedBaseClass
    {
        public string Gateway = "10.10.10.10";
        public String Path = "1,0";
        public PlcType PLCType = PlcType.ControlLogix;
        public Protocol Protocol = Protocol.ab_eip;
        public int Timeout = 5000;
        public bool DebugMode = true;
        [InfoBox("CSV-Format: Symbol,INPUT/OUTPUT,Type,Comment")]
        public string PLCSignalTable;
        [ReadOnly] public ulong UpdateCycle;
        [ReadOnly] private ulong lastupdatecycle;
        
        private Tag<DintPlcMapper, int> mytag;
        private  List<EthernetIPSignal> EIPSignals;

        public void ImportSignals()
        {
            var tags = new Tag<TagInfoPlcMapper, TagInfo[]>()
            {
                Gateway = Gateway,
                Path = Path,
                PlcType = PLCType,
                Protocol = Protocol,
                Name = "@tags"
            };
            try
            {
                tags.Read();
            }
            catch (Exception e)
            {
                if (DebugMode) Error(e.Message);
                Debug.LogWarning("EthernetIP - Error in connecting to PLC, please check your connection settings!");
                return;
            }
            
            foreach (var taginfo in tags.Value)
            {
                Debug.Log("Tag " + taginfo.Name);
            }
        }
        
        [Button("Test Connect")]
        private void Connect()
        {
            EIPSignals = new List<EthernetIPSignal>();
            var plcsignals = GetComponentsInChildren<Signal>();
            
            foreach (var plcsignal in plcsignals)
            {
                string signalname = plcsignal.name;
                if (plcsignal.Name != "")
                    signalname = plcsignal.Name;
                var signal = CreateEthernetIPSignal(signalname, plcsignal.OriginDataType,plcsignal.IsInput());
                signal.Signal = plcsignal;
                if (plcsignal.IsInput())
                    signal.Direction = InterfaceSignal.DIRECTION.INPUT;
                else
                    signal.Direction = InterfaceSignal.DIRECTION.OUTPUT;

                EIPSignals.Add(signal);
            }

            try
            {
                foreach (var eipsignal in EIPSignals)
                {
                    eipsignal.Init();
                 
               
                    if (DebugMode)
                        Debug.Log($"Signal [{eipsignal.Name}] of type [{eipsignal.OriginDataType}] initialized with Status [{eipsignal.GetStatus()}]");

                }
            }
            catch (Exception e)
            {
                if (DebugMode) Error(e.Message);
                Debug.LogWarning("EthernetIP - Error in connecting to PLC, please check your connection settings!");
                OnDisconnected();
                return;
            }
            Debug.Log($"EthernetIP - connected to PLC {PLCType.ToString()} on IP Adress [{Gateway}]" );
            OnConnected();
            IsConnected = true;
        }
         
        [Button("Select PLC Signal table")]
        private void SelectSymbolTable()
        {
            #if UNITY_EDITOR
            var File = "";
            File = EditorUtility.OpenFilePanel("Select file to import", File, "csv");
            PLCSignalTable = File;
            #endif
        }
        
        [Button("Import PLC Signal Table")]
        public void ReadSignalFile()
        {
            List<string> symbol = new List<string>();
            List<string> io = new List<string>();
            List<string> type = new List<string>();
            List<string> comment = new List<string>();
            try
            {
                using (StreamReader sr = new System.IO.StreamReader(PLCSignalTable))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                
                        var tmp = "\"";
                        var newline = line.Replace(tmp, string.Empty);
                        var values = newline.Split(',');
                        symbol.Add(values[0]);
                        io.Add(values[1]);
                        type.Add(values[2]);
                        comment.Add(values[3]);
                    }
                }
            }
            catch (Exception e)
            {
                Error("Error in reading PLC Signal table " + e.ToString());
            }

            for (int i = 0; i < symbol.Count; i++)
            {
                var direction = InterfaceSignal.DIRECTION.OUTPUT;
                if (io[i].ToUpper() == "INPUT")
                    direction = InterfaceSignal.DIRECTION.INPUT;
                InterfaceSignal signal = new InterfaceSignal();
                
                signal.Direction = direction;
                signal.Name = symbol[i];
                signal.OriginDataType = type[i].ToUpper();
                signal.Comment = comment[i];
                switch (signal.OriginDataType)
                {
                    case "REAL":
                        signal.Type = InterfaceSignal.TYPE.REAL;
                        break;
                    case "BOOL":
                        signal.Type = InterfaceSignal.TYPE.BOOL;
                        break;
                    case "SINT":
                        signal.Type = InterfaceSignal.TYPE.INT;
                        break;
                    case "INT":
                        signal.Type = InterfaceSignal.TYPE.INT;
                        break;
                    case "DINT":
                        signal.Type = InterfaceSignal.TYPE.INT;
                        break;
                }
                var sigobj = AddSignal(signal);
                sigobj.gameObject.name = signal.Name;
            }
        }

        public void Disconnect()
        {
            OnDisconnected();
        }


        private EthernetIPSignal CreateEthernetIPSignal(string name, string type, bool isinput)
        {
            var t = type.ToUpper();
            EthernetIPSignal newsignal = new EthernetIPSignal();
            newsignal.OriginDataType = t;
            newsignal.Name = name;
            switch (t)
            {
                case "REAL" : 
                    newsignal.tagfloat = new Tag<RealPlcMapper, float>()
                    {
                        Name = name,
                        Gateway = Gateway,
                        Path = Path,
                        PlcType = PLCType,
                        Protocol = Protocol,
                        Timeout = TimeSpan.FromMilliseconds(Timeout),
                    };
                    break;
                case "BOOL" :
                    if (isinput)
                    {
                        newsignal.tagbool  = new Tag<BoolPlcMapper, bool>()
                        {
                            Name = name,
                            Gateway = Gateway,
                            Path = Path,
                            PlcType = PLCType,
                            Protocol = Protocol,
                            Timeout = TimeSpan.FromMilliseconds(Timeout),
                        };
                    }
                    else
                    {
                        newsignal.tagsint = new Tag<SintPlcMapper, sbyte>()
                        {
                            Name = name,
                            Gateway = Gateway,
                            Path = Path,
                            PlcType = PLCType,
                            Protocol = Protocol,
                            Timeout = TimeSpan.FromMilliseconds(Timeout),
                        };
                    }
            
                    break;
                case "SINT" : 
                    newsignal.tagsint   = new Tag<SintPlcMapper,sbyte>()
                    {
                        Name = name,
                        Gateway = Gateway,
                        Path = Path,
                        PlcType = PLCType,
                        Protocol = Protocol,
                        Timeout = TimeSpan.FromMilliseconds(Timeout),
                    };
                    break;
                case "INT":
                    newsignal.tagint = new Tag<IntPlcMapper, short>()
                    {
                        Name = name,
                        Gateway = Gateway,
                        Path = Path,
                        PlcType = PLCType,
                        Protocol = Protocol,
                        Timeout = TimeSpan.FromMilliseconds(Timeout),
                    };
                    break;
                case "DINT" : 
                    newsignal.tagdint  = new Tag<DintPlcMapper, int>()
                    {
                        Name = name,
                        Gateway = Gateway,
                        Path = Path,
                        PlcType = PLCType,
                        Protocol = Protocol,
                        Timeout = TimeSpan.FromMilliseconds(Timeout),
                    };
                    break;
                default:
                {
                    if (type == "")
                    {
                        Error($"EthernetIP Interface - Signal [{name}] OriginDataType is not defined");
                    }
                    else
                    {
                        Error($"EthernetIP Interface - Signal [{name}] OriginDataType [{type}] is not allowed");
                    }

                    break;
                }
            }
            return newsignal;
        }

        public async Task Read()
        {
            var tasks = new List<Task>();
            foreach (var eipSignal in EIPSignals)
            {
                if (eipSignal.Direction == InterfaceSignal.DIRECTION.OUTPUT)
                {
                    switch (eipSignal.OriginDataType)
                    {
                        case "REAL":
                            tasks.Add(eipSignal.tagfloat.ReadAsync());
                            break;
                        case "BOOL":
                            tasks.Add(eipSignal.tagsint.ReadAsync());
                            break;
                        case "SINT":
                            tasks.Add(eipSignal.tagsint.ReadAsync());
                            break;
                        case "INT":
                            tasks.Add(eipSignal.tagint.ReadAsync());
                            break;
                        case "DINT":
                            tasks.Add(eipSignal.tagdint.ReadAsync());
                            break;
                    }
                }
            }
            Task.WaitAll(tasks.ToArray());
        }
        
        public async Task Write()
        {
            var tasks = new List<Task>();
            foreach (var eipSignal in EIPSignals)
            {
                if (eipSignal.Direction == InterfaceSignal.DIRECTION.INPUT)
                {
                    switch (eipSignal.OriginDataType)
                    {
                        case "REAL":
                            tasks.Add(eipSignal.tagfloat.WriteAsync());
                            break;
                        case "BOOL":
                            tasks.Add(eipSignal.tagbool.WriteAsync());
                            break;
                        case "SINT":
                            tasks.Add(eipSignal.tagsint.WriteAsync());
                            break;
                        case "INT":
                            tasks.Add(eipSignal.tagint.WriteAsync());
                            break;
                        case "DINT":
                            tasks.Add(eipSignal.tagdint.WriteAsync());
                            break;
                    }
                }
            }
            Task.WaitAll(tasks.ToArray());
        }
        
        //! Updates all signals in the parallel communication thread
        protected override void CommunicationThreadUpdate()
        {
            if (!IsConnected)
                return;
            Read();
            Write();
            UpdateCycle++;
        }

        public override void OpenInterface()
        {
            UpdateCycle = 0;
            Connect();
            base.OpenInterface();
        }

        public override void CloseInterface()
        {
            base.CloseInterface();
        }
        
        private void FixedUpdate()
        {
            if (lastupdatecycle < UpdateCycle)
            {
                foreach (var signal in EIPSignals)
                {
                    if (signal.Direction == InterfaceSignal.DIRECTION.OUTPUT)
                        signal.SetValue();
                    if (signal.Direction == InterfaceSignal.DIRECTION.INPUT)
                        signal.GetValue();
                }
                lastupdatecycle = UpdateCycle;
            }
        }
    }
}