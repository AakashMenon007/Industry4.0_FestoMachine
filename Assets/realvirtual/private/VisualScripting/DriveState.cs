#if REALVIRTUAL_VISUALSCRIPTING

using Unity.VisualScripting;
using realvirtual;
using UnityEngine;

[UnitTitle("Drive State")] 
[UnitCategory("realvirtual")] //Setting the path to find the node in the fuzzy finder in Events > My Events.
public class DriveState : Unit
{
   [DoNotSerialize] // No need to serialize ports.
   public ControlInput Input; //Adding the ControlInput port variable

   [DoNotSerialize] // No need to serialize ports.
   public ControlOutput Output;//Adding the ControlOutput port variable.
   
   [DoNotSerialize] // No need to serialize ports
   public ValueInput Drive; // Adding the ValueInput variable for myValueB
   
   [DoNotSerialize] 
   public ValueOutput Position; 
   
   [DoNotSerialize] 
   public ValueOutput Speed;
   
   [DoNotSerialize] 
   public ValueOutput TargetPosition; 
   
   [DoNotSerialize] 
   public ValueOutput IsRunning; 
   
   [DoNotSerialize] 
   public ValueOutput IsAtTarget;
   
   [DoNotSerialize] 
   public ValueOutput IsAtUpperLimit; 
   
   [DoNotSerialize] 
   public ValueOutput IsAtLowerLimit;


   private float position;
   private float speed;
   private float target;
   private bool isrunning;
   private bool isattarget;
   private bool isatupperlimit;
   private bool isatlowerlimit;
   
   protected override void Definition()
   {

      Input = ControlInput("Get State", Action);
    
      Output = ControlOutput("");
      
      Drive = ValueInput<Drive>("Drive", null);
      
      Position = ValueOutput<float>("Position", flow => position);
      Speed = ValueOutput<float>("Speed", flow => speed);
      TargetPosition = ValueOutput<float>("Target Position", flow => target);
      IsRunning = ValueOutput<bool>("Running", flow => isrunning);
      IsAtTarget = ValueOutput<bool>("At Target", flow => isattarget);
      IsAtUpperLimit = ValueOutput<bool>("Upper Limit", flow => isatupperlimit);
      IsAtLowerLimit = ValueOutput<bool>("Lower Limit", flow => isatlowerlimit);
   }


   private ControlOutput Action(Flow flow)
   {
     
      var drive = flow.GetValue<Drive>(Drive);
      position = drive.CurrentPosition;
      speed = drive.CurrentSpeed;
      target = drive.TargetPosition;
      isrunning = drive.IsRunning;
      isattarget = drive.IsAtTarget;
      isatlowerlimit = drive.IsAtLowerLimit;
      isatupperlimit = drive.IsAtUpperLimit;
      
      return Output;
   }
}
#endif