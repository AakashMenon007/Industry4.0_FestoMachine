/*

See :
https://www.universal-robots.com/how-tos-and-faqs/how-to/ur-how-tos/real-time-data-exchange-rtde-guide-22229/ 
 
BOOL : bool
UINT8 : byte
UINT32 : uint
UINT64 : ulong
INT32 : int
DOUBLE : double
VECTOR3D : double[]
VECTOR6D : double []
VECTOR6INT32 : int[]
VECTOR6UINT32 : uint[]
  
TODO and not TODO : do not declare public fields with other types & creates the array with the right size

*/
using System;

namespace Ur_Rtde
{

    [Serializable]
    public class UniversalRobot_Outputs
    {
       // public double io_current; // check the fields name in the RTDE guide : MUST be the same with the same type
        public double[] actual_q = new double[6]; // array creation must be done here to give the size
        public UInt64 actual_digital_output_bits;
        public UInt32 robot_status_bits;
        public UInt32 runtime_state;
        // public int robot_mode;

        // free private & protected attributs are allows
        // all properties and methods also (even public)

    }

    [Serializable]
    public class UniversalRobot_Inputs
    {
        public byte configurable_digital_output;
        public byte configurable_digital_output_mask = 255;
        public byte tool_digital_output_mask;
        public byte tool_digital_output;
        public double input_double_register_24;
    }

}