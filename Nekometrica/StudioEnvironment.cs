// StudioEnvironment.cs
using UnityEngine;
using System;

namespace Nekometrica
{

[Serializable]
public class StudioPanel
{
    public string   label;
    public bool     enabled = true;
    public Color    color   = Color.white;
    [HideInInspector] public GameObject go;
}

public class StudioEnvironment : MonoBehaviour
{
    [Header("6面パネル")]
    public StudioPanel floor    = new StudioPanel { label = "床",    color = new Color(0.85f,0.85f,0.85f) };
    public StudioPanel ceiling  = new StudioPanel { label = "天井",  color = new Color(0.95f,0.95f,0.95f), enabled = false };
    public StudioPanel wallBack = new StudioPanel { label = "奥の壁", color = new Color(0.90f,0.90f,0.90f) };
    public StudioPanel wallFront= new StudioPanel { label = "手前の壁",color = new Color(0.90f,0.90f,0.90f), enabled = false };
    public StudioPanel wallLeft = new StudioPanel { label = "左の壁", color = new Color(0.88f,0.88f,0.88f), enabled = false };
    public StudioPanel wallRight= new StudioPanel { label = "右の壁", color = new Color(0.88f,0.88f,0.88f), enabled = false };

    [Header("サイズ")]
    public float roomWidth  = 10f;
    public float roomDepth  = 10f;
    public float roomHeight = 5f;

    public StudioPanel[] AllPanels => new[] { floor, ceiling, wallBack, wallFront, wallLeft, wallRight };

    void OnValidate() { ApplyAll(); }

    public void ApplyAll()
    {
        foreach (var p in AllPanels)
        {
            if (p.go == null) continue;
            p.go.SetActive(p.enabled);
            var r = p.go.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
                r.sharedMaterial.color = p.color;
        }
    }
}

}
