using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEditor;
using System;


[Serializable]
class BifurcationData
{
    public NodeData[] nodesData;
    //public BezierSpline[] splinesData;
}

[ExecuteInEditMode]
public class Bifurcation : TrackElement
{
    // connectors
    public TrackElement nextCurveRight;
    public TrackElement nextCurveLeft;
    // ----------

    string lastState = "";

    void Awake()
    {
        if (!(EditorApplication.isPlaying))
        {            
            Load();
            Save();
        }
    }

    void Update()
    {
        // Reload curve when changing between editor and play modes
        string state = "";
        if (EditorApplication.isPlaying)
            state = "PlayMode";
        else
        {
            state = "EditorMode";
            //Debug.Log("Saved");
        }
        if (state != lastState)
        {
            Load();
        }
        lastState = state;

        // On editor: save changes and recreate geometry
        if (state == "EditorMode")
        {
            if (nodes != null) Save();
            if (splines != null) Extrude();
        }

        // Maintain conection with next curves        
        if (nextCurveRight != null && nextCurveRight.nodes.Count > 0)
        {
            nodes[3].Copy(nextCurveRight.nodes[0]);
        }
        if (nextCurveLeft != null && nextCurveLeft.nodes.Count > 0)
        {
            nodes[2].Copy(nextCurveLeft.nodes[0]);
        }
    }

    public void Save()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.dataPath + "/ProceduralTracks/CurvesSavedData/" + gameObject.name + ".curve");

        BifurcationData data = new BifurcationData();

        List<NodeData> _nodesData = new List<NodeData>();
        if (nodes != null)
        {
            foreach (Node n in nodes)
            {
                _nodesData.Add(n.Serialize());
            }
            data.nodesData = _nodesData.ToArray();
        }

        //List<BezierSpline> _splinesData = new List<BezierSpline>();
        //foreach (BezierSpline b in splines)
        //{
        //    _splinesData.Add(b.Serialize());
        //}
        //data.splinesData = _splinesData.ToArray();

        bf.Serialize(file, data);
        file.Close();
    }

    public void Load()
    {
        if (File.Exists(Application.dataPath + "/ProceduralTracks/CurvesSavedData/" + gameObject.name + ".curve"))
        {
            ClearCurve();

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.dataPath + "/ProceduralTracks/CurvesSavedData/" + gameObject.name + ".curve", FileMode.Open);
            BifurcationData data = (BifurcationData)bf.Deserialize(file);
            file.Close();

            if (extrudeShape == null) { extrudeShape = new ExtrudeShape(); }

            for (int i = 0; i < data.nodesData.Length; ++i)
            {
                // Create Nodes                
                Node node = CreateNode(transform.position, transform.rotation);
                node.Load(data.nodesData[i]);                
            }

            // Create Splines
            CreateSpline(nodes[0], nodes[1]).Extrude(meshes[0], extrudeShape);
            CreateSpline(nodes[1], nodes[2]).Extrude(meshes[1], extrudeShape);
            CreateSpline(nodes[1], nodes[3]).Extrude(meshes[2], extrudeShape);
        }
        else
        {
            //Debug.Assert(true, "Data file not found");
            Create();
        }
    }

    public void Create()
    {
        ClearCurve();

        // Create Nodes                
        Node node0 = CreateNode(transform.position, transform.rotation);
        Node node1 = CreateNode(transform.position + nodes[nodes.Count - 1].transform.forward * newNodeDistance, 
            nodes[nodes.Count - 1].transform.rotation);
        Node node2 = CreateNode(node1.position + node1.transform.forward * newNodeDistance + 0.5f * node1.transform.right * newNodeDistance, 
            nodes[nodes.Count - 1].transform.rotation);
        Node node3 = CreateNode(node1.position + node1.transform.forward * newNodeDistance - 0.5f * node1.transform.right * newNodeDistance,
            nodes[nodes.Count - 1].transform.rotation);

        // Create Splines
        CreateSpline(node0, node1).Extrude(meshes[0], extrudeShape); ;
        CreateSpline(node1, node2).Extrude(meshes[1], extrudeShape); ;
        CreateSpline(node1, node3).Extrude(meshes[2], extrudeShape); ;
    }

    public override Node CreateNode(Vector3 position, Quaternion rotation)
    {
        GameObject nodeGO = Instantiate(nodePrefab, transform.position, transform.rotation) as GameObject;
        nodeGO.transform.parent = transform;
        Node node = nodeGO.GetComponent<Node>();

        node.frontTransform = node.transform.FindChild("front");
        node.backTransform = node.transform.FindChild("back");

        node.position = position;
        node.curve = this;
        nodes.Add(node);

        return node;
    }

    public override BezierSpline CreateSpline(Node start, Node end)
    {
        GameObject splineGO = Instantiate(splinePrefab, transform.position, transform.rotation) as GameObject;
        splineGO.transform.parent = transform;
        BezierSpline spline = splineGO.GetComponent<BezierSpline>();

        spline.curve = this;
        spline.startNode = start;
        spline.endNode = end;

        spline.transform.position = (start.transform.position + end.transform.position) / 2;

        splines.Add(spline);
        meshes.Add(new Mesh());

        return (spline);
    }

    public void Extrude()
    {
        if (extrudeShape == null)
        {
            extrudeShape = new ExtrudeShape();
        }
        splines[0].Extrude(meshes[0], extrudeShape);
        splines[1].ExtrudeSide(meshes[1], extrudeShape, "right");
        splines[2].ExtrudeSide(meshes[2], extrudeShape, "left");

        //for (int i = 0; i < splines.Count; ++i)
        //{
        //    splines[i].Extrude(meshes[i], extrudeShape);
        //}
    }

    public void AddSpline()
    {
        if (splines.Count == 0)
        {
            // Create the first segment           
            Node firstNode = CreateNode(transform.position, transform.rotation);
            Node secondNode = CreateNode(transform.position + transform.forward * newNodeDistance, transform.rotation);
            CreateSpline(firstNode, secondNode);
        }
        else
        {
            Node lastNode = nodes[nodes.Count - 1];
            Node newNode = CreateNode(lastNode.position + lastNode.transform.forward * newNodeDistance, lastNode.transform.rotation);
            CreateSpline(lastNode, newNode);
        }
    }    

    public void ClearCurve()
    {
        // Clear node references
        for (int i = 0; i < nodes.Count; ++i)
        {
            if (nodes[i] != null)
                DestroyImmediate(nodes[i].gameObject);
        }
        nodes.Clear();

        // Clear spline references
        for (int i = 0; i < splines.Count; ++i)
        {
            if (splines[i] != null)
                DestroyImmediate(splines[i].gameObject);
        }
        splines.Clear();

        // Clear mesh references
        for (int i = 0; i < meshes.Count; ++i)
        {
            if (meshes[i] != null)
                DestroyImmediate(meshes[i]);
        }
        meshes.Clear();

        // destroy other unreferenced elements        
        while (transform.childCount != 0)
        {
            if (transform.GetChild(0) != null)
                DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }


}

