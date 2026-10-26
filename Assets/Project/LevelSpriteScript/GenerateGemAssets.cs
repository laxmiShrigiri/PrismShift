using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

// Purpose: Adds an Editor menu command that creates the tile-stud and socket UI textures.
public class GenerateGemAssets : MonoBehaviour
{
    [MenuItem("Tools/Generate Stud & Socket Textures")]
    public static void GenerateTextures()
    {
        int size = 128;

        // 1. Create Tile Stud Texture (Pillow / Beveled Gem)
        Texture2D studTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) / 2f;
        float radius = size * 0.44f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Rounded square distance
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);
                float dist = Mathf.Pow(Mathf.Pow(dx, 4) + Mathf.Pow(dy, 4), 0.25f);

                if (dist > radius)
                {
                    studTex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    // Specular bevel highlight (top-left lighter, bottom-right darker)
                    float nx = (x - center) / radius;
                    float ny = (y - center) / radius;
                    float light = 1f + (-nx * 0.25f + ny * 0.35f);
                    light = Mathf.Clamp(light, 0.65f, 1.35f);

                    studTex.SetPixel(x, y, new Color(light, light, light, 1f));
                }
            }
        }
        studTex.Apply();

        // 2. Create Socket / Slot Texture (Recessed Dark Indentation)
        Texture2D socketTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);
                float dist = Mathf.Pow(Mathf.Pow(dx, 4) + Mathf.Pow(dy, 4), 0.25f);

                if (dist > radius + 4f)
                {
                    socketTex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    // Dark inner cavity with subtle border
                    float cavity = Mathf.Clamp01(dist / radius);
                    float shade = Mathf.Lerp(0.2f, 0.4f, cavity);
                    socketTex.SetPixel(x, y, new Color(shade, shade, shade, 0.85f));
                }
            }
        }
        socketTex.Apply();

        // Save PNGs to Assets/
        SavePNG(studTex, "Assets/TileStud.png");
        SavePNG(socketTex, "Assets/SlotSocket.png");
        AssetDatabase.Refresh();

        Debug.Log("Generated 'TileStud.png' and 'SlotSocket.png' in Assets folder!");
    }

    private static void SavePNG(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
#endif
