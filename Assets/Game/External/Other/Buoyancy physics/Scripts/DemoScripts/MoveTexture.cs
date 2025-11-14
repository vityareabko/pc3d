using UnityEngine;
using System.Collections;

[ExecuteAlways]
public class MoveTexture : MonoBehaviour 
{
    public float speed = 0.1f;
    private float offset;

	void Update () 
    {
        offset +=  speed * Time.deltaTime;

        if (GetComponent<Renderer>().material.HasProperty("_MainTex"))
        {
            GetComponent<Renderer>().material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }

        if (GetComponent<Renderer>().material.HasProperty("_BumpMap"))
        {
            GetComponent<Renderer>().material.SetTextureOffset("_BumpMap", new Vector2(-offset, -offset * 2));
        }
        

	}
}
