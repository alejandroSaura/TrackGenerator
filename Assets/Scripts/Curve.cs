using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Curve : MonoBehaviour
{
    public float trackWidth = 0.5f;
    public int horizontalDivisions = 10;
    public int divisionsPerCurve = 5;

    public float newNodeDistance = 2.5f;

    public List<Node> nodes;
    public List<BezierSpline> splines;

    public List<Mesh> meshes;

    public GameObject nodePrefab;
    public GameObject splinePrefab;

    ExtrudeShape extrudeShape;

    public void Extrude()
    {
        if (extrudeShape == null)
        {
            extrudeShape = new ExtrudeShape();
        }

        for (int i = 0; i < splines.Count; ++i)
        {
            splines[i].Extrude(meshes[i], extrudeShape);
        }
    }    

    public void AddSpline ()
    {
        if (splines.Count == 0)
        {
            // Create the first segment
            GameObject firstSplineGO = Instantiate(splinePrefab, transform.position, transform.rotation) as GameObject;
            firstSplineGO.transform.parent = transform;
            BezierSpline firstSpline = firstSplineGO.GetComponent<BezierSpline>();

            GameObject firstNodeGO = Instantiate(nodePrefab, transform.position, transform.rotation) as GameObject;
            firstNodeGO.transform.parent = transform;
            Node firstNode = firstNodeGO.GetComponent<Node>();            
            firstSpline.startNode = firstNode;

            GameObject secondNodeGO = Instantiate(nodePrefab, transform.position + transform.forward * newNodeDistance, transform.rotation) as GameObject;
            secondNodeGO.transform.parent = transform;
            Node secondNode = secondNodeGO.GetComponent<Node>();
            firstSpline.endNode = secondNode;

            // Add references to this object
            firstNode.curve = this;
            secondNode.curve = this;
            firstSpline.curve = this;

            // Move the spline GO between nodes
            //firstSplineGO.transform.position = (firstNodeGO.transform.position + secondNode.transform.position) / 2;

            nodes.Add(firstNode);
            nodes.Add(secondNode);

            splines.Add(firstSpline);
            meshes.Add(new Mesh());
        }
        else
        {
            Node lastNode = nodes[nodes.Count - 1];
            GameObject newNodeGO = Instantiate(nodePrefab, lastNode.position + lastNode.transform.forward * newNodeDistance, lastNode.transform.rotation) as GameObject;
            newNodeGO.transform.parent = transform;
            Node newNode = newNodeGO.GetComponent<Node>();
            newNode.curve = this;

            GameObject newSplineGO = Instantiate(splinePrefab, lastNode.position, lastNode.transform.rotation) as GameObject;
            newSplineGO.transform.parent = transform;
            BezierSpline newSpline = newSplineGO.GetComponent<BezierSpline>();

            newSpline.startNode = lastNode;
            newSpline.endNode = newNode;
            newSpline.curve = this;

            //move the spline GO between nodes
            //newSpline.transform.position = (lastNode.transform.position + newNodeGO.transform.position) / 2;

            nodes.Add(newNode);
            splines.Add(newSpline);
            meshes.Add(new Mesh());
        }
    }	

    public void CloseCurve()
    {
        Node lastNode = nodes[nodes.Count - 1];
        Node firstNode = nodes[0];       

        GameObject newSplineGO = Instantiate(splinePrefab, lastNode.position, lastNode.transform.rotation) as GameObject;
        newSplineGO.transform.parent = transform;
        BezierSpline newSpline = newSplineGO.GetComponent<BezierSpline>();

        newSpline.startNode = lastNode;
        newSpline.endNode = firstNode;

        //move the spline GO between nodes
        newSpline.transform.position = (lastNode.transform.position + firstNode.transform.position) / 2;
        
        splines.Add(newSpline);
        meshes.Add(new Mesh());
    }

    public void ClearCurve()
    {
        for (int i = 0; i < nodes.Count; ++i)
        {
            DestroyImmediate(nodes[i].gameObject);
        }
        nodes.Clear();

        for (int i = 0; i < splines.Count; ++i)
        {
            DestroyImmediate(splines[i].gameObject);
        }
        splines.Clear();

        for (int i = 0; i < meshes.Count; ++i)
        {
            DestroyImmediate(meshes[i]);
        }
        meshes.Clear();

    }

        
}
