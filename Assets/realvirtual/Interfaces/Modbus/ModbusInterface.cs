// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System;
using EasyModbus;
using NaughtyAttributes;
using UnityEngine;

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/modbus")]
    public class ModbusInterface : InterfaceBaseClass
    {
        // Start is called before the first frame update
        public bool status;
        public bool IsServer=true; // Hide as long as only server
        [HideIf("IsServer")] public string ServerIPAdress;
        [HideIf("IsServer")] public int ServerPort=502;
        [ShowIf("IsServer")] public int ConnectedClients;
        private ModbusClient modbusClient;
        private ModbusServer modbusServer;

        private byte[] outputregisters;
        private byte[] inputregisters;
        private int lastconnected;
        
        public void WriteValue(ModbusData data,object value)
        {
            if (data.Register == ModbusData.RegisterType.InputRegister)
            {
                byte[] bytes;
                switch (data.Type)
                {
                   
                    case ModbusData.ModbusType.BOOL:
                        byte byteval = 0;
                        bool valbool = (bool) value;
                        byte valbyte = inputregisters[data.RegisterAdress * 2 +  data.RegisterByte];
                        if (valbool == true)
                        {
                            byteval = (byte) (valbyte | (byte) (0x01 << data.Bit));
                        }
                        else
                        {
                            byteval = (byte) (valbyte & ~(1 << data.Bit));
                        }
                        inputregisters[data.RegisterAdress * 2 +  data.RegisterByte] = byteval;
                        break;
                    
                    case ModbusData.ModbusType.BYTE:
                        inputregisters[data.RegisterAdress * 2 + data.RegisterByte] = (byte)value;
                        break;
                    case ModbusData.ModbusType.SBYTE:
                        bytes = BitConverter.GetBytes((sbyte) value);
                        inputregisters[data.RegisterAdress * 2 + data.RegisterByte] = bytes[0];
                        break;
                    case ModbusData.ModbusType.INT: 
                        bytes = BitConverter.GetBytes((int) value);
                        inputregisters[data.RegisterAdress * 2] = bytes[0];
                        inputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        inputregisters[data.RegisterAdress * 2+2] = bytes[2];
                        inputregisters[data.RegisterAdress * 2+3] = bytes[3];
                        break;
                    case ModbusData.ModbusType.SHORT:
                        bytes = BitConverter.GetBytes((short) value);
                        inputregisters[data.RegisterAdress * 2] = bytes[0];
                        inputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        break;
                    case ModbusData.ModbusType.USHORT:
                        bytes = BitConverter.GetBytes((ushort) value);
                        inputregisters[data.RegisterAdress * 2] = bytes[0];
                        inputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        break;
                    case ModbusData.ModbusType.UINT:
                        bytes = BitConverter.GetBytes((uint) value);
                        inputregisters[data.RegisterAdress * 2] = bytes[0];
                        inputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        inputregisters[data.RegisterAdress * 2+2] = bytes[2];
                        inputregisters[data.RegisterAdress * 2+3] = bytes[3];
                        break;
                    case ModbusData.ModbusType.FLOAT: 
                        bytes = BitConverter.GetBytes((float) value);
                        inputregisters[data.RegisterAdress * 2] = bytes[0];
                        inputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        inputregisters[data.RegisterAdress * 2+2] = bytes[2];
                        inputregisters[data.RegisterAdress * 2+3] = bytes[3];
                        break;
                }
                Buffer.BlockCopy(inputregisters,data.RegisterAdress * 2,modbusServer.inputRegisters.localArray,data.RegisterAdress * 2,4);
            }
            
            if (data.Register == ModbusData.RegisterType.OutputRegister)
            {
                byte[] bytes;
                switch (data.Type)
                {
                   
                    case ModbusData.ModbusType.BOOL:
                        byte byteval = 0;
                        bool valbool = (bool) value;
                        byte valbyte = outputregisters[data.RegisterAdress * 2 + data.RegisterByte];


                        if (valbool == true)
                        {
                            byteval = (byte) (valbyte | (byte) (0x01 << data.Bit));
                        }
                        else
                        {
                            byteval = (byte) (valbyte & ~(1 << data.Bit));
                        }
                        outputregisters[data.RegisterAdress * 2 +  data.RegisterByte] = byteval ;
                        break;
                    case ModbusData.ModbusType.BYTE:
                        outputregisters[data.RegisterAdress * 2 + data.RegisterByte] = Convert.ToByte(value);
                        break;
                    case ModbusData.ModbusType.SBYTE:
                        bytes = BitConverter.GetBytes((sbyte) value);
                        outputregisters[data.RegisterAdress * 2 + data.RegisterByte] = bytes[0];
                        break;
                    case ModbusData.ModbusType.INT: 
                        bytes = BitConverter.GetBytes((int) value);
                        outputregisters[data.RegisterAdress * 2] = bytes[0];
                        outputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        outputregisters[data.RegisterAdress * 2+2] = bytes[2];
                        outputregisters[data.RegisterAdress * 2+3] = bytes[3];
                        break;
                    case ModbusData.ModbusType.SHORT:
                        bytes = BitConverter.GetBytes((short) value);
                        outputregisters[data.RegisterAdress * 2] = bytes[0];
                        outputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        break;
                    case ModbusData.ModbusType.USHORT:
                        bytes = BitConverter.GetBytes((ushort) value);
                        outputregisters[data.RegisterAdress * 2] = bytes[0];
                        outputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        break;
                    case ModbusData.ModbusType.UINT:
                        bytes = BitConverter.GetBytes((uint) value);
                        outputregisters[data.RegisterAdress * 2] = bytes[0];
                        outputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        outputregisters[data.RegisterAdress * 2+2] = bytes[2];
                        outputregisters[data.RegisterAdress * 2+3] = bytes[3];
                        break;
                    case ModbusData.ModbusType.FLOAT: 
                        bytes = BitConverter.GetBytes((float) value);
                        outputregisters[data.RegisterAdress * 2] = bytes[0];
                        outputregisters[data.RegisterAdress * 2+1] = bytes[1];
                        outputregisters[data.RegisterAdress * 2+2] = bytes[2];
                        outputregisters[data.RegisterAdress * 2+3] = bytes[3];
                        break;
                }
                Buffer.BlockCopy(outputregisters,data.RegisterAdress * 2,modbusServer.holdingRegisters.localArray,data.RegisterAdress * 2,4);
            }
            
            if (data.Register == ModbusData.RegisterType.DigitalOutputsCoils)
            {
                switch (data.Type)
                {
                    case ModbusData.ModbusType.BOOL:
                        modbusServer.coils[data.RegisterAdress] = (bool) value;
                        break;
                    default:
                        Error($"The datatype {data.Type.ToString()} can not be transfered to a bool");
                        value = false;
                        break;
                }
            }
            if (data.Register == ModbusData.RegisterType.DigitalInputsCoils)
            {
                switch (data.Type)
                {
                    case ModbusData.ModbusType.BOOL:
                        modbusServer.discreteInputs[data.RegisterAdress] = (bool) value;
                        break;
                    default:
                        Error($"The datatype {data.Type.ToString()} can not be transfered to a bool");
                        value = false;
                        break;
                }
            }
        }
        
        public object ReadValue(ModbusData data)
        {
            // Read Value Server
            object value = null;

            
            if (data.Register == ModbusData.RegisterType.OutputRegister)
            {
                if (outputregisters == null)
                    return null;
                switch (data.Type)
                {
                    case ModbusData.ModbusType.BOOL:
                        var valbyte = (byte) outputregisters[data.RegisterAdress * 2];
                        bool Valbool = ((valbyte >> data.Bit) & 1) != 0;
                        value = Valbool;
                        break;
                    case ModbusData.ModbusType.BYTE:
                        value = (byte) outputregisters[data.RegisterAdress * 2 + data.RegisterByte];
                        break;
                    case ModbusData.ModbusType.SBYTE:
                        value = (sbyte) outputregisters[data.RegisterAdress * 2 + + data.RegisterByte];
                        break;
                    case ModbusData.ModbusType.INT:
                        value = (int) BitConverter.ToInt32(outputregisters, data.RegisterAdress * 2);
                        break;
                    case ModbusData.ModbusType.SHORT:
                        value = (short) BitConverter.ToInt16(outputregisters, data.RegisterAdress * 2);
                        break;
                    case ModbusData.ModbusType.USHORT:
                        value = (ushort) BitConverter.ToUInt16(outputregisters, data.RegisterAdress * 2);
                        break;
                    case ModbusData.ModbusType.UINT:
                        value = (UInt32) BitConverter.ToUInt32(outputregisters, data.RegisterAdress * 2);
                        break;
                    case ModbusData.ModbusType.FLOAT:
                        value = (float) BitConverter.ToSingle(outputregisters, data.RegisterAdress * 2);
                        break;

                }
            }

            if (data.Register == ModbusData.RegisterType.DigitalOutputsCoils)
            {
                switch (data.Type)
                {
                    case ModbusData.ModbusType.BOOL:
                        value = (bool) modbusServer.coils[data.RegisterAdress];
                        break;
                    default:
                        Error($"The datatype {data.Type.ToString()} can not be transfered to a bool");
                        value = false;
                        break;
                }
            }
            
               if (data.Register == ModbusData.RegisterType.InputRegister)
            {
                if (inputregisters == null)
                    return null;
             
                Buffer.BlockCopy(modbusServer.inputRegisters.localArray,data.RegisterAdress * 2,inputregisters,data.RegisterAdress * 2,4);
                switch (data.Type)
                {
                    case ModbusData.ModbusType.BOOL:
                        var valbyte = (byte) inputregisters[data.RegisterAdress * 2];
                        bool Valbool = ((valbyte >> data.Bit) & 1) != 0;
                        value = Valbool;
                        break;
                    case ModbusData.ModbusType.BYTE:
                        value = (byte) inputregisters[data.RegisterAdress * 2 + data.RegisterByte];
                        break;
                    case ModbusData.ModbusType.SBYTE:
                        value = (sbyte) inputregisters[data.RegisterAdress * 2 + + data.RegisterByte];
                        break;
                    case ModbusData.ModbusType.INT:
                        value = (int) BitConverter.ToInt32(inputregisters, data.RegisterAdress * 2);
                        break;
                    case ModbusData.ModbusType.SHORT:
                        value = (short) BitConverter.ToInt16(inputregisters, data.RegisterAdress * 2);
                        break;
                    case ModbusData.ModbusType.USHORT:
                        value = (ushort) BitConverter.ToUInt16(inputregisters, data.RegisterAdress * 2);
                        break;
                    case ModbusData.ModbusType.UINT:
                        value = (UInt32) BitConverter.ToUInt32(inputregisters, data.RegisterAdress * 2);
                        break;
                    case ModbusData.ModbusType.FLOAT:
                        value = (float) BitConverter.ToSingle(inputregisters, data.RegisterAdress * 2);
                        break;

                }
                Buffer.BlockCopy(inputregisters,data.RegisterAdress * 2,modbusServer.inputRegisters.localArray,data.RegisterAdress * 2,4);
            }

            if (data.Register == ModbusData.RegisterType.DigitalInputsCoils)
            {
                switch (data.Type)
                {
                    case ModbusData.ModbusType.BOOL:
                        value = (bool) modbusServer.discreteInputs[data.RegisterAdress];
                        break;
                    default:
                        Error($"The datatype {data.Type.ToString()} can not be transfered to a bool");
                        value = false;
                        break;
                }
            }

            return value;
        }
        
        /// [Button("Connect")] 
        void ConnectClient()
        {
            try
            {
                modbusClient = new ModbusClient(ServerIPAdress, ServerPort);
                modbusClient.Connect();     
                Log( $"Modbus Client connected to {ServerIPAdress}");
                ReadFromServer();
            }
            catch (Exception e)
            {
                Error(e.Message);
            }
        }

        void StartServer()
        {    
            try
            {
                modbusServer = new ModbusServer();
                modbusServer.Listen();
                modbusServer.CoilsChanged += ModbusServerOnCoilsChanged;
                modbusServer.HoldingRegistersChanged += ModbusServerOnHoldingRegistersChanged;
                modbusServer.NumberOfConnectedClientsChanged += ModbusServerOnNumberOfConnectedClientsChanged;

                var len = modbusServer.inputRegisters.localArray.Length * 2;
                inputregisters = new byte[len];
                
                len = modbusServer.holdingRegisters.localArray.Length * 2;
                outputregisters = new byte[len];
                Log("Modbus Server started");
            }
            catch (Exception e)
            {
                Error(e.Message);
            }
        }

        private void ModbusServerOnNumberOfConnectedClientsChanged()
        {
            
           
        }
        
        

        private void ModbusServerOnHoldingRegistersChanged(int register, int numberofregisters)
        {
            var len = modbusServer.holdingRegisters.localArray.Length*2;
            outputregisters = new byte[len];
            Buffer.BlockCopy(modbusServer.holdingRegisters.localArray,0,outputregisters,0,len);
        }

        private void ModbusServerOnCoilsChanged(int coil, int numberofcoils)
        {
            this.Log($"Coils changed - coil {coil}, number {numberofcoils}");
        }

        void DisconnectClient()
        {
            modbusClient.Disconnect();
            modbusClient = null;
        }
        
        void DisconnectServer()
        {
            modbusServer.StopListening();
            modbusServer = null;
        }

        void Send()
        {
            status = !status;
            modbusClient.WriteMultipleCoils(4, new bool[] {status, status, status, status, status, status, status, status, status, status});
            Invoke("Send", 1);
        }

// Test
        void ReadFromServer()
        {
            int[] holding = modbusClient.ReadInputRegisters(10, 1);
            Debug.Log(" Input Register" + holding[0]);
            
        }

      [Button("Read from Server")]
        void Read()
        {
            ReadFromServer();
        }
        
        // Update is called once per frame
        void Update()
        {
            if (IsServer)
            {
                ConnectedClients = modbusServer.NumberOfConnections;
                if (lastconnected != ConnectedClients)
                {
                    if (ConnectedClients > 0)
                        OnConnected();
                    else
                        OnDisconnected();
                    lastconnected = ConnectedClients;
                }
            }
            else
            {
              //  ReadFromServer();
            }
            
        }

        private void OnEnable()
        {
            if (!IsServer)
            {
                ConnectClient();
            }
            else
            {
                StartServer();
            }
        }

        private void OnDisable()
        {
            if (!IsServer)
            {
                DisconnectClient();
           
            }
            else
            {
                DisconnectServer();
            }
        }
    }
}