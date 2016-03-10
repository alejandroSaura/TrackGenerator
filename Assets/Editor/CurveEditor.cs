using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Curve))]
public class CurveEditor : Editor {

	public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Curve myCurve = (Curve)target;
        if(GUILayout.Button("AddSpline"))
        {
            myCurve.AddSpline();
        }
        if (GUILayout.Button("CloseCurve"))
        {
            myCurve.CloseCurve();
        }
        if (GUILayout.Button("CreateGeometry"))
        {
            myCurve.Extrude();
        }
        if (GUILayout.Button("ClearCurve"))
        {
            myCurve.ClearCurve();
        }
    }

}
