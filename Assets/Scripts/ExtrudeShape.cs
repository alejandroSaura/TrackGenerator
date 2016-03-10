using UnityEngine;
using System.Collections;

public class ExtrudeShape
{

    public Vector2[] verts =
    {
        new Vector2(0,0),
        new Vector2(1,0),
        new Vector2(2,0),
        new Vector2(3,0),
        new Vector2(4,0)
        //new Vector2(4.5f,0.5f),
        //new Vector2(5,1),
        //new Vector2(5,1),
        //new Vector2(5,1.5f)
    };
    public Vector2[] normals =
    {
        new Vector2(1,0),
        new Vector2(1,0),
        new Vector2(1,0),
        new Vector2(1,0),
        new Vector2(1,0)
        //new Vector2(-1,1),
        //new Vector2(-1,0.1f),
        //new Vector2(-1,0),
        //new Vector2(-1,0)
    };
    public float[] us =
    {
        0,
        1/4f,
        2/4f,
        3/4f,
        1
        //5/7f,
        //6/7f,
        //6/7f,
        //1
    };
    public int[] lines =
    {
        0,1,
        1,2,
        2,3,
        3,4
        //4,5,
        //5,6,
        //6,7,
        //8,9
    };
}
