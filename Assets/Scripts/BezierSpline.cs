using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct BezierData
{

}

[ExecuteInEditMode]
public class BezierSpline : MonoBehaviour
{    
    public Material material;
    public Curve curve;

    public int numTracersDebug = 100;

    public Node startNode;
    public Node endNode;

    OrientedPoint[] orientedPoints;    

    public Vector3 GetPoint(float t)
    {
        if (endNode == null) return Vector3.zero;

        Vector3 p0 = startNode.position;
        Vector3 p1 = startNode.frontControl;
        Vector3 p2 = endNode.backControl;
        Vector3 p3 = endNode.position;

        /*
            L1 = Lerp(start-startControl) , L2 = Lerp(startControl-endControl) , L3 = Lerp(endControl-end)
            L1' = Lerp(L1-L2) , L2' = Lerp(L2-L3)
            result = Lerp(L1'-L2')

            We use here the bernstein polynomial form of this algorithm for performance issues
        */

        Vector3 result =
            p0 * ((1 - t) * (1 - t) * (1 - t)) +
            p1 * (3 * (1 - t) * (1 - t) * t) +
            p2 * (3 * (1 - t) * t * t) +
            p3 * (t * t * t);

        return result;

    }

    public Vector3 GetTangent(float t)
    {
        if (endNode == null) return Vector3.zero;

        Vector3 p0 = startNode.position;
        Vector3 p1 = startNode.frontControl;
        Vector3 p2 = endNode.backControl;
        Vector3 p3 = endNode.position;

        // Vector between L1' and L2', see GetPoint

        Vector3 result =
            p0 * (-t * t + 2 * t - 1) +
            p1 * (3 * t * t - 4 * t + 1) +
            p2 * (-3 * t * t + 2 * t) +
            p3 * (t * t);

        return result.normalized;

    }

    public Vector3 GetNormal(float t, Vector3 up)
    {
        if (endNode == null) return Vector3.zero;

        Vector3 tangent = GetTangent(t);
        Vector3 binormal = Vector3.Cross(up, tangent);
        return Vector3.Cross(tangent, binormal).normalized;

    }

    public Vector3 GetBinormal(float t, Vector3 up)
    {
        if (endNode == null) return Vector3.zero;

        Vector3 tangent = GetTangent(t);
        return Vector3.Cross(up, tangent);
    }

    public Quaternion GetOrientation(float t, Vector3 up)
    {
        if (endNode == null) return Quaternion.identity;

        Vector3 tangent = GetTangent(t);
        Vector3 normal = GetNormal(t, up);

        return Quaternion.LookRotation(tangent, normal);
    }

    void OnDrawGizmos()
    {
        if (endNode == null) return;

        float segmentLength = 1.0f / (float)numTracersDebug;
        Vector3 start = startNode.position;
        for (int i = 1; i < numTracersDebug+1; ++i)
        {
            Vector3 end = GetPoint(i * segmentLength);
            Gizmos.DrawLine(start, end);

            //if(i%20 == 0)
            //{
            //    DebugExtension.DebugArrow(start, GetTangent(i * segmentLength), Color.cyan);

            //    Vector3 up = Vector3.Lerp(startNode.transform.up, endNode.transform.up, i * segmentLength);
            //    DebugExtension.DebugArrow(start, GetNormal(i * segmentLength, up), Color.green);
            //    DebugExtension.DebugArrow(start, GetBinormal(i * segmentLength, up), Color.red);
            //}          

            

            start = end;
        }
        //if (orientedPoints != null)
        //{
        //    for (int j = 0; j < orientedPoints.Length; ++j)
        //    {
        //        Gizmos.DrawWireSphere(transform.TransformPoint(orientedPoints[j].position), 0.1f);
        //    }
        //} 
    }

    // find the length of the spline
    float GetLength()
    {
        // chorded approximation
        float length = 0;

        int numChords = 100;
        float chordLength = 1.0f / (float)numChords;
        Vector3 start = startNode.position;
        for (int i = 1; i < numChords + 1; ++i)
        {
            Vector3 end = GetPoint(i * chordLength);
            length += Vector3.Distance(start, end);            

            start = end;
        }
        return length;
    }

    public void Extrude(Mesh mesh, ExtrudeShape shape)
    {
        float splineLength = GetLength();
        shape.Initialize(1, curve.horizontalDivisions);        

        int divisions = (int)(curve.divisionsPerCurve * splineLength / curve.trackWidth / 20);
        int vertsInShape = curve.horizontalDivisions + 1;

        List<int> triangleIndices = new List<int>(); //new int[triIndexCount];
        List <Vector3> vertices = new List<Vector3>();  //new Vector3[vertCount];
        List<Vector3> normals = new List<Vector3>(); //new Vector3[vertCount];
        List<Vector2> uvs = new List<Vector2>(); //new Vector2[vertCount];

        /*
            Mesh generation code
        */
        #region meshGeneration

        float divisionLength = 1.0f / (float)divisions;
        orientedPoints = new OrientedPoint[divisions + 1];

        #region rightSide
        int rightOffset = 0;

        // Create vertices
        for (int i = 0; i <= divisions; ++i)
        {// for each edgeLoop
            float t = i * divisionLength;

            Vector3 up = Vector3.Lerp(startNode.transform.up, endNode.transform.up, t);
            float curvature = Mathf.Lerp(startNode.rightCurvature, endNode.rightCurvature, t);
            float width = (curve.trackWidth + Mathf.Lerp(startNode.trackWidthModifier, endNode.trackWidthModifier, t))/2;
            Matrix4x4 scale = Matrix4x4.Scale(new Vector3(width, width, width));            

            // Initialize oriented point
            orientedPoints[i].position = transform.InverseTransformPoint(GetPoint(t));
            orientedPoints[i].rotation =  Quaternion.Inverse(transform.rotation) * (GetOrientation(t, up));
            orientedPoints[i].scale = scale;

            for (int j = 0; j < vertsInShape; ++j)
            {// for each vertex in the shape

                Vector2 vertex = Vector2.Lerp(shape.verts[j], shape.curvedVerts[j], curvature);
                Vector2 normal = Vector2.Lerp(shape.normals[j], shape.curvedNormals[j], curvature);

                vertices.Add(orientedPoints[i].LocalToWorld(vertex));
                normals.Add(orientedPoints[i].LocalToWorldDirection(normal));

                // u is based on the 2D shape, and v is based on the distance along the curve
                uvs.Add(new Vector2(shape.us[j], t * splineLength / width));                
            }            
        }

        // Create triangles
        for (int j = 0; j < divisions; ++j)
        {// for each division in the curve
            int offset = shape.verts.Length * j;

            for(int k = 0; k < shape.lines.Length; k = k+2)
            {//for each 2d line in the shape
                // triangle 1                
                triangleIndices.Add(shape.lines[k + 1] + offset);
                triangleIndices.Add(shape.lines[k] + offset);
                triangleIndices.Add(shape.lines[k] + offset + shape.verts.Length);
                // triangle 2
                triangleIndices.Add(shape.lines[k + 1] + offset);
                triangleIndices.Add(shape.lines[k] + offset + shape.verts.Length);
                triangleIndices.Add(shape.lines[k + 1] + offset + shape.verts.Length);
                                
            }

        }
        rightOffset = vertices.Count;
        #endregion

        #region leftSide
        for (int i = 0; i <= divisions; ++i)
        {// for each edgeLoop
            float t = i * divisionLength;

            Vector3 up = Vector3.Lerp(startNode.transform.up, endNode.transform.up, t);
            float curvature = Mathf.Lerp(startNode.leftCurvature, endNode.leftCurvature, t);
            float width = curve.trackWidth + Mathf.Lerp(startNode.trackWidthModifier, endNode.trackWidthModifier, t);

            // Initialize oriented point
            orientedPoints[i].position = transform.InverseTransformPoint(GetPoint(t));
            orientedPoints[i].rotation = Quaternion.Inverse(transform.rotation) * (GetOrientation(t, up));

            for (int j = 0; j < vertsInShape; ++j)
            {// for each vertex in the shape

                Vector2 vertex = Vector2.Lerp(shape.verts[j], shape.curvedVerts[j], curvature);
                vertex.x *= -1;
                Vector2 normal = Vector2.Lerp(shape.normals[j], shape.curvedNormals[j], curvature);
                

                vertices.Add(orientedPoints[i].LocalToWorld(vertex));
                normals.Add(orientedPoints[i].LocalToWorldDirection(normal));

                // u is based on the 2D shape, and v is based on the distance along the curve
                uvs.Add(new Vector2(shape.us[j], t * splineLength / width));
            }
        }
        #endregion

        // Create triangles
        for (int j = 0; j < divisions; ++j)
        {// for each division in the curve
            int offset = shape.verts.Length * j;

            for (int k = 0; k < shape.lines.Length; k = k + 2)
            {//for each 2d line in the shape
                // triangle 1                
                triangleIndices.Add(shape.lines[k + 1] + offset);
                triangleIndices.Add(shape.lines[k] + offset);
                triangleIndices.Add(shape.lines[k] + offset + shape.verts.Length);
                // triangle 2
                triangleIndices.Add(shape.lines[k + 1] + offset);
                triangleIndices.Add(shape.lines[k] + offset + shape.verts.Length);
                triangleIndices.Add(shape.lines[k + 1] + offset + shape.verts.Length);

            }

            for (int k = 0; k < shape.lines.Length; k = k + 2)
            {//for each 2d line in the shape
                // triangle 1
                triangleIndices.Add(shape.lines[k] + offset + rightOffset);
                triangleIndices.Add(shape.lines[k + 1] + offset + rightOffset);
                triangleIndices.Add(shape.lines[k] + offset + shape.verts.Length + rightOffset);
                // triangle 2                
                triangleIndices.Add(shape.lines[k] + offset + shape.verts.Length + rightOffset);
                triangleIndices.Add(shape.lines[k + 1] + offset + rightOffset);
                triangleIndices.Add(shape.lines[k + 1] + offset + shape.verts.Length + rightOffset);

            }

        }
        #endregion

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangleIndices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();

        //mesh.RecalculateNormals();      

        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void Split()
    {
        Quaternion orientation = GetOrientation(0.5f, Vector3.Lerp(startNode.transform.up, endNode.transform.up, 0.5f));
        Vector3 position = GetPoint(0.5f);
        
        // new intermediate node creation
        GameObject newNodeGO = Instantiate(curve.nodePrefab, position, orientation) as GameObject;
        newNodeGO.transform.parent = curve.transform;
        Node newNode = newNodeGO.GetComponent<Node>();
        newNode.curve = this.curve;

        // we need to create 2 splines now
        GameObject newSplineGO = Instantiate(curve.splinePrefab, startNode.position, startNode.transform.rotation) as GameObject;
        newSplineGO.transform.parent = curve.transform;
        BezierSpline newSpline = newSplineGO.GetComponent<BezierSpline>();

        newSpline.startNode = this.startNode;
        newSpline.endNode = newNode;
        newSpline.curve = this.curve;

        GameObject newSplineGO2 = Instantiate(curve.splinePrefab, newNode.position, newNode.transform.rotation) as GameObject;
        newSplineGO2.transform.parent = curve.transform;
        BezierSpline newSpline2 = newSplineGO2.GetComponent<BezierSpline>();

        newSpline2.startNode = newNode;
        newSpline2.endNode = endNode;
        newSpline2.curve = this.curve;

        curve.nodes.Add(newNode);
        curve.splines.Add(newSpline);
        curve.splines.Add(newSpline2);
        curve.meshes.Add(new Mesh());
        curve.meshes.Add(new Mesh());

        // Make sure that its removed from the curve list
        curve.splines.Remove(this);
        DestroyImmediate(this);
    }

}
