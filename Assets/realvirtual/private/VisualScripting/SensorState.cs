#if REALVIRTUAL_VISUALSCRIPTING
using Unity.VisualScripting;
using realvirtual;
using UnityEngine;

[UnitTitle("Sensor State")] 
[UnitCategory("realvirtual")] //Setting the path to find the node in the fuzzy finder in Events > My Events.
public class SensorState : Unit
{
   [DoNotSerialize] 
   public ControlInput Start; //Adding the ControlInput port variable

   [DoNotSerialize] 
   public ControlOutput Occupied;//Adding the ControlOutput port variable.
   public ControlOutput NotOccupied;//Adding the ControlOutput port variable.
   
   
   [DoNotSerialize] 
   public ValueInput Sensor; // Adding the ValueInput variable for myValueB
   
   [DoNotSerialize] 
   public ValueOutput IsOccupied; // Adding the ValueInput variable for myValueB
   
   [DoNotSerialize] 
   public ValueOutput MU; // Adding the ValueInput variable for myValueB
   
   [DoNotSerialize] 
   public ValueOutput GameObject; // Adding the ValueInput variable for myValueB
   
   [DoNotSerialize]
   public ValueInput Speed; // Adding the ValueInput variable for myValueB

   private bool occupied;
   private Sensor sensor;
   protected override void Definition()
   {

      Start = ControlInput("Start", Action);
     
  
      Occupied = ControlOutput("Occupied");
      NotOccupied= ControlOutput("NotOccupied");
      
      Sensor = ValueInput<Sensor>("Sensor", null);

      IsOccupied = ValueOutput<bool>("IsOccupied", getOccupied);
      MU = ValueOutput <MU>("MU", getMU);
      GameObject = ValueOutput <GameObject>("Gameobject", getGameobject);
   }

   private bool getOccupied(Flow flow)
   {
      var sensor = flow.GetValue<Sensor>(Sensor);
      return sensor.Occupied;
   }
   
   private MU getMU(Flow flow)
   {
      var sensor = flow.GetValue<Sensor>(Sensor);
      if (sensor.CollidingMus != null && sensor.CollidingMus.Count>0)
         return sensor.CollidingMus[0];
      else
         return null;
   }
   
   private GameObject getGameobject(Flow flow)
   {
      var sensor = flow.GetValue<Sensor>(Sensor);
      if (sensor.CollidingObjects != null && sensor.CollidingObjects.Count>0)
         return sensor.CollidingObjects[0];
      else
         return null;
   }

   private ControlOutput Action(Flow flow)
   {
      var sensor = flow.GetValue<Sensor>(Sensor);
      occupied = sensor.Occupied;
     
      if (occupied)
      {
         return Occupied;
      }
      else
      {
         return NotOccupied;
      }
           
   }
}
#endif