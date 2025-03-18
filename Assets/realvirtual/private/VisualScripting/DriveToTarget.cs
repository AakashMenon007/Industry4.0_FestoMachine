#if REALVIRTUAL_VISUALSCRIPTING

using Unity.VisualScripting;
using realvirtual;
using UnityEngine;

[UnitTitle("Drive to Target")] 
[UnitCategory("realvirtual")] //Setting the path to find the node in the fuzzy finder in Events > My Events.
public class DriveToTarget : Unit
{
   [DoNotSerialize] // No need to serialize ports.
   public ControlInput Start; //Adding the ControlInput port variable
   
   [DoNotSerialize] // No need to serialize ports.
   public ControlInput UpdateStatus; //Adding the ControlInput port variable

   [DoNotSerialize] // No need to serialize ports.
   public ControlOutput IsDriving;//Adding the ControlOutput port variable.
   
   [DoNotSerialize] // No need to serialize ports.
   public ControlOutput ArrivedAtTarget;//Adding the ControlOutput port variable.
   
   [DoNotSerialize] // No need to serialize ports.
   public ControlOutput IsAtTarget;//Adding the ControlOutput port variable.
      
   [DoNotSerialize] // No need to serialize ports
   public ValueInput Drive; // Adding the ValueInput variable for myValueB
   

   
   [DoNotSerialize] // No need to serialize ports
   public ValueInput Target; // Adding the ValueInput variable for myValueB
   
   [DoNotSerialize] // No need to serialize ports
   public ValueInput Speed; // Adding the ValueInput variable for myValueB
   
   [DoNotSerialize] // No need to serialize ports
   public ValueOutput Position; // Adding the ValueInput variable for myValueB


   private Drive drive;
   private float target;
   private float speed;
   private bool attarget;
   private bool drivingtotarget;
   private float drivepos;
   
   protected override void Definition()
   {
      Start = ControlInput("Start", StartDrive);
      UpdateStatus = ControlInput("Update", CheckStatus);
 
      Drive = ValueInput<Drive>("Drive", null);
      Speed = ValueInput<float>("Speed", 100);
      Target = ValueInput<float>("Target", 1000);
      
      
      ArrivedAtTarget = ControlOutput("Arrived");   
      IsDriving = ControlOutput("Driving");
      IsAtTarget = ControlOutput("At Target");
      
      Position = ValueOutput<float>("Position", flow => drivepos);
   }
   
   
   private ControlOutput StartDrive(Flow flow)
   {
      drive = flow.GetValue<Drive>(Drive); 
      var s =  flow.GetValue<float>(Speed);
      target =  flow.GetValue<float>(Target);
      drivepos = drive.CurrentPosition;
      
      drive.TargetSpeed = s;
      if (drive.TargetPosition != target)
      {
         drive.DriveTo(target);
         drivingtotarget = true;
      }

      return null;
   }

   private void FixedUpdate()
   {
      Debug.Log("FixedUpdate");
   }
   
   private ControlOutput CheckStatus(Flow flow)
   {
      drive = flow.GetValue<Drive>(Drive); 
      var s =  flow.GetValue<float>(Speed);
      target =  flow.GetValue<float>(Target);
      drivepos = drive.CurrentPosition;

      if (target == drive.CurrentPosition && drivingtotarget)
      {
         drivingtotarget = false;
         attarget = true;
         return ArrivedAtTarget;
      }


      if (target == drive.CurrentPosition && !drivingtotarget)
      {  
         attarget = true;
         return IsAtTarget;
      }

      if (target != drive.CurrentPosition && drivingtotarget)
      {
         return IsDriving;

      }

      return null;
   }
}

#endif