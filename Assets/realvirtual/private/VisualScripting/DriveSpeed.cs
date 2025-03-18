#if REALVIRTUAL_VISUALSCRIPTING
using Unity.VisualScripting;
using realvirtual;
using UnityEngine;

[UnitTitle("Drive Speed")] 
[UnitCategory("realvirtual")] //Setting the path to find the node in the fuzzy finder in Events > My Events.
public class DriveSpeed : Unit
{
   [DoNotSerialize] // No need to serialize ports.
   public ControlInput Start; //Adding the ControlInput port variable

   [DoNotSerialize] // No need to serialize ports.
   public ControlOutput Trigger;//Adding the ControlOutput port variable.
   
         
   [DoNotSerialize] // No need to serialize ports
   public ValueInput Drive; // Adding the ValueInput variable for myValueB
   
   
   [DoNotSerialize] // No need to serialize ports
   public ValueInput Speed; // Adding the ValueInput variable for myValueB
   
   
   protected override void Definition()
   {
      //Making the ControlInput port visible, setting its key and running the anonymous action method to pass the flow to the outputTrigger port.
      Start = ControlInput("Set Speed", Action);
      //Making the ControlOutput port visible and setting its key.
      Trigger = ControlOutput("");
      
      Drive = ValueInput<Drive>("Drive", null);
      Speed = ValueInput<float>("Speed", 100);
   }
   
   private ControlOutput Action(Flow flow)
   {
      var speed = flow.GetValue<float>(Speed);
      var drive = flow.GetValue<Drive>(Drive);
      if (speed > 0)
      {
         drive.JogForward = true;
         drive.JogBackward = false;
         drive.TargetSpeed = speed;
      }
      if (speed<0)
      {
         drive.JogForward = false;
         drive.JogBackward = true;
         drive.TargetSpeed = -speed;
      }
      if (speed==0)
      {
            drive.JogForward = false;
            drive.JogBackward = false;
            drive.TargetSpeed = 0;
      }
      return Trigger;
   }
}
#endif