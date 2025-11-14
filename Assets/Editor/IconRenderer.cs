// Assets/Editor/IconRenderer.cs
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class IconRenderer
{
    // --- Defaults you can tweak ---
    const int ICON_SIZE = 512;
    static readonly Color CLEAR_COLOR = new Color(0,0,0,0);       // transparent background
    static readonly Vector3 CAM_EULER = new Vector3(40f, -80f, 5);// view angle
    const float PADDING = 1.1f;                                   // 10% padding around bounds
    const bool USE_ORTHO = true;                                  // orthographic icons feel cleaner

    [MenuItem("Tools/Icons/Render Icons (Transparent)")]
    public static void RenderIconsForSelection()
    {
        var assets = Selection.objects
            .Where(o => o != null && (PrefabUtility.GetPrefabAssetType(o) != PrefabAssetType.NotAPrefab || o is Mesh || o is GameObject))
            .ToArray();

        if (assets.Length == 0)
        {
            EditorUtility.DisplayDialog("Icon Renderer", "Select one or more Prefabs / Models in Project view.", "OK");
            return;
        }

        try
        {
            for (int i = 0; i < assets.Length; i++)
            {
                var a = assets[i];
                string assetPath = AssetDatabase.GetAssetPath(a);
                string dir = Path.GetDirectoryName(assetPath);
                string name = Path.GetFileNameWithoutExtension(assetPath);
                string outPath = Path.Combine(dir, $"{name}_icon.png").Replace("\\", "/");

                RenderSingleAssetIcon(a, outPath);
                EditorUtility.DisplayProgressBar("Rendering icons", name, (float)(i + 1) / assets.Length);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }

    static void RenderSingleAssetIcon(Object asset, string outPngPath)
    {
        // ----- Create temp stage -----
        var root = new GameObject("__IconStageRoot");
        var camGO = new GameObject("__IconCamera");
        camGO.transform.SetParent(root.transform, false);
        var lightGO = new GameObject("__IconLight");
        lightGO.transform.SetParent(root.transform, false);

        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparent
        cam.backgroundColor = CLEAR_COLOR;
        cam.allowMSAA = true;
        cam.allowHDR = true;
        cam.useOcclusionCulling = false;
        cam.cullingMask = ~0;

        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = Color.white;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Instantiate a preview object
        GameObject inst = null;
        if (asset is GameObject go)
            inst = (GameObject)PrefabUtility.InstantiatePrefab(go);
        else if (asset is Mesh mesh)
        {
            inst = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mf = inst.GetComponent<MeshFilter>();
            mf.sharedMesh = mesh;
        }
        else
        {
            // Try to get main object
            var main = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(asset));
            if (main != null) inst = (GameObject)PrefabUtility.InstantiatePrefab(main);
        }

        if (inst == null)
        {
            Object.DestroyImmediate(root);
            Debug.LogWarning($"[IconRenderer] Unsupported asset: {asset.name}");
            return;
        }

        inst.name = "__IconSubject";
        inst.transform.SetParent(root.transform, false);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        // ----- Compute bounds -----
        var renderers = inst.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Object.DestroyImmediate(root);
            Debug.LogWarning($"[IconRenderer] No renderers in {asset.name}");
            return;
        }

        Bounds b = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
        foreach (var r in renderers) b.Encapsulate(r.bounds);

        Vector3 center = b.center;
        Vector3 ext = b.extents;
        float maxExtent = Mathf.Max(ext.x, Mathf.Max(ext.y, ext.z)) * PADDING;

        // ----- Setup camera framing -----
        cam.transform.rotation = Quaternion.Euler(CAM_EULER);
        var fwd = cam.transform.forward;

        if (USE_ORTHO)
        {
            cam.orthographic = true;
            // Make orthographic size fit the object extents in camera's XY
            // A simple conservative fit using max extent works fine for icons.
            cam.orthographicSize = maxExtent;
            // place camera in front of object at a safe distance
            float dist = maxExtent * 3f;
            cam.transform.position = center - fwd * dist;
        }
        else
        {
            cam.orthographic = false;
            cam.fieldOfView = 30f;

            float radius = maxExtent;
            float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            dist *= 1.05f; // small padding
            cam.transform.position = center - fwd * dist;
        }

        light.transform.position = cam.transform.position;
        light.transform.rotation = cam.transform.rotation * Quaternion.Euler(30, 0, 0);

        // ----- Render to RT -----
        var rt = new RenderTexture(ICON_SIZE, ICON_SIZE, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        rt.Create();

        var prevRT = RenderTexture.active;
        var prevTarget = cam.targetTexture;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;

        var tex = new Texture2D(ICON_SIZE, ICON_SIZE, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, ICON_SIZE, ICON_SIZE), 0, 0);
        tex.Apply();

        // ----- Save PNG with alpha -----
        byte[] png = tex.EncodeToPNG();
        Directory.CreateDirectory(Path.GetDirectoryName(outPngPath));
        File.WriteAllBytes(outPngPath, png);

        // import settings
        AssetDatabase.ImportAsset(outPngPath);
        var ti = (TextureImporter)AssetImporter.GetAtPath(outPngPath);
        if (ti != null)
        {
            ti.alphaIsTransparency = true;
            ti.sRGBTexture = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        // ----- Cleanup -----
        cam.targetTexture = prevTarget;
        RenderTexture.active = prevRT;

        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(root);
    }
}
