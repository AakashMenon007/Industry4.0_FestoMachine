// realvirtual.io (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Ur_Rtde;
using Debug = UnityEngine.Debug;

//! Universal robots interface for communication with URSIM via RTDE
namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/universal-robots")]
    public class UniversalRobotsInterface : InterfaceThreadedBaseClass
    {
        public string UrIpAdress = "192.168.0.4"; //!< URSIM IP Adress
        public int OutputFrequency = 20; //!< Frequency per second in which the outputs of URSIM are read
        public List<Drive> Axis = new List<Drive>(); //!< The axis of the robot
        public List<PLCOutputBool> DigitalOutputs = new List<PLCOutputBool>();  //!< The Digital outputs of the robot, PLCOutputBool
        public List<PLCOutputBool> ConfigurableOutputs = new List<PLCOutputBool>();   //!< The configurable outputs of the robot, PLCOutputBool
        public List<PLCInputBool> DigitalInputs = new List<PLCInputBool>(); //!< The inputs of the robot, PLCInputBool
        public List<PLCInputBool> ConfigurableInputs = new List<PLCInputBool>(); //!< The configurable inputs of the robot, PLCInputtBool
        
        [Header ("Robot Status")]        
        [ReadOnly] public UInt64 RobotDigitalInputs;
        [ReadOnly] public UInt64 RobotDigitalOutputs;
        [ReadOnly] public UInt32 RobotStatusBits;
        [ReadOnly] public UInt32 RobotProgramState;
        
        private RtdeClient urclient;
        private UniversalRobot_Outputs Outputs;
        private Socket socket;
        private UniversalRobot_Inputs Inputs;
        private IPAddress ip;
        private IPEndPoint endpoint;
        
        #region StandardMethods

        public override void OpenInterface()
        {
            StartCoroutine(Connect());
            base.OpenInterface();
        }

        public override void CloseInterface()
        {
            if (urclient != null)
                urclient.Disconnect();
            if (socket != null)
               socket.Close();
            base.CloseInterface();
            OnDisconnected();
        }

        protected override void CommunicationThreadUpdate()
        {
            SendInputsSecondary();
        }

        #endregion

        private string CreateCommand()
        {
            string command = $"sec secondaryProgram():\n";
            int i = 0;
            foreach (var input in DigitalInputs)
            {
                if (input != null)
                {
                    var value = input.Value;
                    command += $"set_digital_in({i}, {value})\n";
                    i++;
                }
              
            }
             i = 0;
            foreach (var input in ConfigurableInputs)
            {
                if (input != null)
                {
                    var value = input.Value;
                    command += $"set_configurable_digital_in({i}, {value})\n";
                    i++;
                }
              
            }
            command += "end\n";
            return command;
        }
     
        private void SendInputsSecondary()
        {
            if (socket != null)
            {
                string command = CreateCommand();
                UTF8Encoding utf8 = new UTF8Encoding();
                byte[] cmd = utf8.GetBytes(command);
                var send = socket.Send(cmd);
            }
        }
        
        IEnumerator Connect()
        {
            urclient = new RtdeClient();
            Outputs = new UniversalRobot_Outputs();
            Inputs = new UniversalRobot_Inputs();
            urclient.OnSockClosed += UrclientOnOnSockClosed;
            urclient.OnDataReceive += UrclientOnOnDataReceive;
            bool error = false;
            if (urclient.Connect(UrIpAdress, 2))
            {
                Debug.Log("UR connected to UR controller on IP adress " + UrIpAdress);
            }
            else
            {
                Debug.LogError("Unable to connect to UR controller on IP Adress " + UrIpAdress);
                error = true;
            }

            yield return new WaitForSecondsRealtime(0.2f);
            if (urclient.Setup_Ur_Inputs(Inputs))
            {
                Debug.Log("UR inputs registered");
            }
            else
            {
                Debug.LogError("Error in registering UR inputs ");
                error = true;
            }
            
            yield return new WaitForSecondsRealtime(0.2f);
            if (urclient.Setup_Ur_Outputs(Outputs, OutputFrequency))
            {
                Debug.Log("UR outputs registered");
            }
            else
            {
                Debug.LogError("Error in registering UR outputs ");
                error = true;
            }
            
            yield return new WaitForSecondsRealtime(0.2f);
            if (!urclient.Ur_ControlStart())
            {
                Debug.LogError("Error in starting UR Client ");
                error = true;
            }

            if (!error)
            {
                OnConnected();
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
                ip = IPAddress.Parse(UrIpAdress);
                endpoint = new IPEndPoint(ip, 30001);
                socket.Connect(endpoint);
            }
                
        }

        void SetDrives()
        {
             int index = 0;
            foreach (var drive in Axis)
            {
                var position = Outputs.actual_q[index];
                float pos = (float)position * Mathf.Rad2Deg;
                drive.CurrentPosition = pos;
                index++;
            }
        }

        void SetDigitalOutputs()
        {
            var bytes = BitConverter.GetBytes(RobotDigitalOutputs);
            var bits = new BitArray(bytes[0]);
            int bit = 0;
            foreach (var digitaloutput in DigitalOutputs)
            {
                if (digitaloutput != null)
                {
                    if ((bytes[0] & (1 << bit)) != 0)
                    {
                        digitaloutput.Value = true;
                    }
                    else
                    {
                        digitaloutput.Value = false;
                    }
                }
                bit++;
            }
             bit = 0;
            foreach (var digitaloutput in ConfigurableOutputs)
            {
                if (digitaloutput != null)
                {
                    if ((bytes[1] & (1 << bit)) != 0)
                    {
                        digitaloutput.Value = true;
                    }
                    else
                    {
                        digitaloutput.Value = false;
                    }
                }
                bit++;
            }
        }

        public static byte SetBit(byte b, int BitNumber)
        {
            //Kleine Fehlerbehandlung
            if (BitNumber < 8 && BitNumber > -1)
            {
                return (byte)(b | (byte)(0x01 << BitNumber));
            }
            else
            {
                throw new InvalidOperationException(
                    "Der Wert für BitNumber " +  BitNumber.ToString() + " war nicht im zulässigen Bereich! (BitNumber = (min)0 - (max)7)");
            }
        }
        
        void SetDigitalInputs()
        {
            int bit = 0;
            byte b = 0;
            foreach (var digitalinput in DigitalInputs)
            {
                if (digitalinput != null)
                {
                    if (digitalinput.Value)
                        b = SetBit(b, bit);
                }
                bit++;
            }
            Inputs.configurable_digital_output_mask = 255;
            Inputs.configurable_digital_output = b;
        }

        private void UrclientOnOnDataReceive(object sender, EventArgs e)
        { RobotStatusBits = Outputs.robot_status_bits;
           RobotProgramState = Outputs.runtime_state;
           RobotDigitalOutputs = Outputs.actual_digital_output_bits;
           SetDrives();
           SetDigitalOutputs();
        }
    
        private void UrclientOnOnSockClosed(object sender, EventArgs e)
        {
            Debug.Log("UR connection to controller on IP Adress " + UrIpAdress + " closed");
            OnDisconnected();
        }
    
        void Reset()
        {
            MinUpdateCycle = 20;
        }

    
    }
}

