// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz    

using realvirtual;
using UnityEngine;

public class GuideCircle : realvirtualBehavior, IGuide
{
    public float Radius = 1.0f;
    public float Angle=90;
    public bool ShowGizmos = true;
    private float smoothness = 0.1f;
    private float arclength;
    private Vector3[] pathpoints;

    private Vector3 rotationPoint;
    private int pathpointsnumber;
    [HideInInspector] public float maxx, maxy;
    
    public void OnDrawGizmos()
    {
        CreatePath();
        if (!ShowGizmos) return;
        
        if (Angle == 0) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(pathpoints[0]), 0.02f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(pathpoints[^1]), 0.02f);
        Gizmos.color = Color.yellow;
        for (var i = 1; i < pathpoints.Length; i++)
        {
            Gizmos.DrawLine(transform.TransformPoint(pathpoints[i-1]), transform.TransformPoint(pathpoints[i]));
        }
    }
    

    public Vector3 GetClosestDirection(Vector3 position)
    {
        var localPoint = transform.InverseTransformPoint(position);
        localPoint.y = 0;
        var initDirection = pathpoints[0] - rotationPoint;
        var pointDirection = localPoint - rotationPoint;
        var angle = Vector3.SignedAngle(initDirection, pointDirection, Vector3.up * Mathf.Sign(Angle));
        angle = Mathf.Clamp(angle, 0, Mathf.Abs(Angle));

        var tangent = Quaternion.AngleAxis(angle, Vector3.up * Mathf.Sign(Angle)) * transform.right;
        return tangent;
    }

    public Vector3 GetClosestPoint(Vector3 position)
    {
        Vector3 localPoint = this.transform.InverseTransformPoint(position);
        localPoint.y = 0;
        Vector3 initDirection = pathpoints[0] - rotationPoint;
        Vector3 pointDirection = localPoint - rotationPoint;
        float angle = Vector3.SignedAngle(initDirection, pointDirection, Vector3.up * Mathf.Sign(Angle));            
        angle = Mathf.Clamp(angle, 0, Mathf.Abs(Angle));
        Vector3 result = RotatePointAroundPivot(pathpoints[0], rotationPoint, Vector3.up * (angle * Mathf.Sign(Angle)));
        return this.transform.TransformPoint(result);
    }

    public bool IsActive()
    {
        return this.enabled;
    }

    public  void CreatePath()    
    {
        smoothness = 0.1f;
        if (Mathf.Approximately(Angle,0)) return;

        arclength = Mathf.PI * Radius * (Mathf.Abs(Angle) / 180);
        pathpointsnumber = Mathf.CeilToInt(arclength / smoothness) + 1;
        var angleStep = Mathf.Abs(Angle) / (pathpointsnumber - 1);
        rotationPoint = -Vector3.forward * Mathf.Sign(Angle) * Radius;
        maxx = 0;
        maxy = 0;
        pathpoints = new Vector3[pathpointsnumber];
        for (var i = 0; i < pathpoints.Length; i++)
        {                
            pathpoints[i] = RotatePointAroundPivot(Vector3.zero, rotationPoint, (Vector3.up * Mathf.Sign(Angle) * angleStep * i));
            if (Mathf.Abs(pathpoints[i].x) > maxx) maxx = Mathf.Abs(pathpoints[i].x);
            if (Mathf.Abs(pathpoints[i].z) > maxy) maxy = Mathf.Abs(pathpoints[i].z);
        }
    }
    



    private void Start()
    {
        CreatePath();
    }
    
    private  Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) 
    {
        var dir = point - pivot; 
        dir = Quaternion.Euler(angles) * dir;
        point = dir + pivot;
        return point;
    }
 
}

