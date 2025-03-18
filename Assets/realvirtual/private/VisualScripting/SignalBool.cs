#if REALVIRTUAL_VISUALSCRIPTING
using Unity.VisualScripting;
using realvirtual;
using UnityEngine;

[UnitTitle("PLC Output Bool")] 
[UnitCategory("realvirtual")] 
public  class SignalBool : Unit
{
    [DoNotSerialize] public ControlInput Read;
    [DoNotSerialize] public ControlInput Write;
    [DoNotSerialize] public ValueInput Signal;
    [DoNotSerialize] public ValueInput ValueWrite;

    [DoNotSerialize] public ControlOutput Changed;
    [DoNotSerialize] public ValueOutput ValueRead;
    
    private bool currentvalue;

    protected override void Definition()
    {
        Read = ControlInput("Read", ActionRead);
        Write = ControlInput("Write", ActionWrite);
        Signal = ValueInput<Signal>("Signal", null);
        ValueWrite = ValueInput<bool>("Value Write", false);
        Changed =  ControlOutput("Changed");
        ValueRead = ValueOutput<bool>("Value Read", flow => currentvalue);
    }
    
    private ControlOutput ActionWrite(Flow flow)
    {
        var signal = flow.GetValue<Signal>(Signal);
        bool setvalue = flow.GetValue<bool>(ValueWrite);
    
        if (setvalue != currentvalue)
        {
            signal.SetValue(setvalue);
            currentvalue = setvalue;
            return Changed;
        }

        return null;
    }

    private ControlOutput ActionRead(Flow flow)
    {
        var signal = flow.GetValue<Signal>(Signal);
        bool val = (bool)signal.GetValue();
        if (val != currentvalue)
        {
            currentvalue = val;
            return Changed;
        }

        return null;
    }
}
#endif