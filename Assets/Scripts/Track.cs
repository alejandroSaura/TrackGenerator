using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class TrackData
{
    public int curveId;
    public int bifId;
}

[ExecuteInEditMode]
public class Track : MonoBehaviour
{
    public int curveIdGenerator = 0;
    public int bifIdGenerator = 0;

    string lastState = "";
    void Update()
    {
        // Reload when changing between editor and play modes
        string state = "";
        if (EditorApplication.isPlaying)
            state = "PlayMode";
        else
        {
            state = "EditorMode";            
        }
        if (state != lastState)
        {
            Load();
        }
        lastState = state;        
    }

    public void Save()
    {
        Debug.Log("Track ids saved");

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.dataPath + "/CurvesSavedData/" + gameObject.name + ".curve");

        TrackData data = new TrackData();
        data.bifId = bifIdGenerator;
        data.curveId = curveIdGenerator;

        bf.Serialize(file, data);
        file.Close();
    }

    public void Load()
    {
        if (File.Exists(Application.dataPath + "/CurvesSavedData/" + gameObject.name + ".curve"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.dataPath + "/CurvesSavedData/" + gameObject.name + ".curve", FileMode.Open);
            TrackData data = (TrackData)bf.Deserialize(file);
            file.Close();

            curveIdGenerator = data.curveId;
            bifIdGenerator = data.bifId;
        }
    }

}


