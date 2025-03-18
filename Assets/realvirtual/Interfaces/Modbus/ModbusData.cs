using NaughtyAttributes;
using UnityEngine;

namespace realvirtual
{
    public class ModbusData : MonoBehaviour
    {
        public enum RegisterType
        {
            OutputRegister,
            InputRegister,
            DigitalOutputsCoils,
            DigitalInputsCoils
        };

        public enum ModbusType
        {
            BOOL,
            BYTE,
            SBYTE,
            INT,
            UINT,
            SHORT,
            USHORT,
            FLOAT
        };

        public RegisterType Register;
        public int RegisterAdress = 0;

        [ShowIf("IsByte")] [Dropdown("ByteValues")]
        public int RegisterByte = 0;

        public ModbusType Type;

        [ShowIf("IsBit")] [Dropdown("BitValues")]
        public int Bit = 0;

        private Signal signal;
        private ModbusInterface modbusinterface;
        private int[] ByteValues = new int[] {0, 1};
        private int[] BitValues = new int[] {0, 1, 2, 3, 4, 5, 6, 7};

        public void SetSignalFromRegister()
        {
        }

        public void GetSignalFromRegister()
        {
        }

        public void Awake()
        {
            signal = GetComponent<Signal>();
            if (signal.IsInput())
                signal.SignalChanged += SignalOnSignalChanged;
            modbusinterface = GetComponentInParent<ModbusInterface>();
        }

        private bool IsBit()
        {
            return (Type == ModbusType.BOOL &&
                    (Register == RegisterType.InputRegister || Register == RegisterType.OutputRegister));
        }

        private bool IsByte()
        {
            return (Type == ModbusType.BYTE || Type == ModbusType.SBYTE || Type == ModbusType.BOOL);
        }

        private void SignalOnSignalChanged(Signal obj)
        {
            if (signal.IsInput())
                modbusinterface.WriteValue(this, signal.GetValue());
        }

        private void Update()
        {
            if (!signal.IsInput())
                signal.SetValue(modbusinterface.ReadValue(this));
        }
    }
}