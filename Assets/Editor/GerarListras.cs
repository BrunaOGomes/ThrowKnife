using UnityEditor;
using UnityEngine;
using System.IO;

public static class GerarListras
{
    [MenuItem("Tools/Gerar textura de listras")]
    static void Gerar()
    {
        int w = 64, h = 8;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, x < w / 2 ? new Color(0.85f, 0.1f, 0.1f) : Color.white);
        tex.Apply();
        File.WriteAllBytes("Assets/Materials/Listras.png", tex.EncodeToPNG());
        AssetDatabase.Refresh();
        Debug.Log("Listras.png criada em Assets/Materials");
    }
}