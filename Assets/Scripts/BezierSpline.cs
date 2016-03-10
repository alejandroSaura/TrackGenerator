using UnityEngine;
using System.Collections.Generic;

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

            if (orientedPoints != null)
            {
                for (int j = 0; j < orientedPoints.Length; ++j)
                {
                    Gizmos.DrawWireSphere(transform.TransformPoint(orientedPoints[j].position), 0.1f);
                }
            } 

            start = end;
        }
    }

    public void Extrude(Mesh mesh, ExtrudeShape shape, int divisions)
    {
        int vertsInShape = shape.verts.Length;
        int segments = divisions;
        int edgeLoops = divisions + 1;
        int vertCount = edgeLoops * vertsInShape;
        int triCount = shape.lines.Length * segments * 2;
        int triIndexCount = triCount * 3;

        List<int> triangleIndices = new List<int>(); //new int[triIndexCount];
        List <Vector3> vertices = new List<Vector3>();  //new Vector3[vertCount];
        List<Vector3> normals = new List<Vector3>(); //new Vector3[vertCount];
        List<Vector2> uvs = new List<Vector2>(); //new Vector2[vertCount];

        /*
            Mesh generation code
        */
        #region meshGeneration

        Debug.Log("Extrusion Started");
        float divisionLength = 1.0f / (float)divisions;
        orientedPoints = new OrientedPoint[divisions + 1];

        // Create vertices
        for (int i = 0; i <= divisions; ++i)
        {// for each edgeLoop
            float t = i * divisionLength;

            Vector3 up = Vector3.Lerp(startNode.transform.up, endNode.transform.up, t);

            // Initialize oriented point
            orientedPoints[i].position = transform.InverseTransformPoint(GetPoint(t));
            orientedPoints[i].rotation =  Quaternion.Inverse(transform.rotation) * (GetOrientation(t, up));

            for (int j = 0; j < vertsInShape; ++j)
            {// for each vertex in the shape

                vertices.Add(orientedPoints[i].LocalToWorld(shape.verts[j]));
                normals.Add(orientedPoints[i].LocalToWorldDirection(shape.normals[j]));

                // u is based on the 2D shape, and v is based on the distance along the curve
                uvs.Add(new Vector2(shape.us[j], t));
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
        #endregion

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangleIndices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();        

        gameObject.GetComponent<MeshFilter>().mesh = mesh;
    }
}
