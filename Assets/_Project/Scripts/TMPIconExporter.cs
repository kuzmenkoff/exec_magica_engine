#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class TMPIconExporter : MonoBehaviour
{
    public Camera renderCamera;
    public int width = 1600;
    public int height = 500;
    public string fileName = "banner";   // сохранит в корень проекта как <fileName>.png

    [ContextMenu("Export PNG")]
    public void ExportPNG()
    {
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var prevTarget = renderCamera.targetTexture;
        var prevActive = RenderTexture.active;

        renderCamera.targetTexture = rt;
        renderCamera.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        renderCamera.targetTexture = prevTarget;
        RenderTexture.active = prevActive;

        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(Application.dataPath, "..", fileName + ".png"),
            tex.EncodeToPNG());

        DestroyImmediate(tex);
        DestroyImmediate(rt);
        AssetDatabase.Refresh();
        Debug.Log("Saved: " + fileName + ".png (root)");
    }
}
#endif
