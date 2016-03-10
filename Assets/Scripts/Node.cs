using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Node : MonoBehaviour
{
    public Curve curve;

    public float gizmoSize = 0.2f;

    public float frontWeight = 1;
    public float backWeight = 1;

    Transform frontTransform;
    Transform backTransform;

    public Vector3 frontControl
    {
        get {
            //return transform.position + transform.forward * frontWeight; 
            return frontTransform.position;
        }
        set { }
    }

    public Vector3 backControl
    {
        get {
            //return transform.position - transform.forward * backWeight;
            return backTransform.position;
        }
        set { }
    }

    public Vector3 position
    {
        get { return transform.position; }
        set { }
    }

    void Awake()
    {
        frontTransform = transform.FindChild("front");
        backTransform = transform.FindChild("back");
    }

    void OnDrawGizmos()
    {
        frontTransform = transform.FindChild("front");
        backTransform = transform.FindChild("back");

        Gizmos.color = new Vector4(0, 0, 1, 1);
        Gizmos.DrawWireSphere(transform.position, gizmoSize);

        Gizmos.color = new Vector4(0.7f, 0.7f, 1, 1);
        //Gizmos.DrawLine(transform.position, transform.position + transform.forward * frontWeight);
        //Gizmos.DrawLine(transform.position, transform.position - transform.forward * backWeight);

        Gizmos.DrawLine(transform.position, frontTransform.position);
        Gizmos.DrawLine(transform.position, backTransform.position);

        //Gizmos.DrawWireSphere(transform.position + transform.forward * frontWeight, gizmoSize / 2);
        //Gizmos.DrawWireSphere(transform.position - transform.forward * backWeight, gizmoSize / 2);

        DebugExtension.DrawArrow(transform.position, transform.forward / 2, Color.cyan);

        // up and right indicators
        Gizmos.color = new Vector4(1, 0, 0, 1);
        Gizmos.DrawLine(transform.position, transform.position + transform.right*3);
        Gizmos.color = new Vector4(0, 1, 0, 1);
        Gizmos.DrawLine(transform.position, transform.position + transform.up*3);

    }    
}
