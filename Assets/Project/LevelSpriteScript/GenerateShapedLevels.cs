using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

// Purpose: Adds an Editor menu command that generates transparent pixel-art puzzle level textures.
public class GenerateShapedLevels : MonoBehaviour
{
    // Color Palette
    private static readonly Color32 Clear  = new Color32(0, 0, 0, 0);          // Fully transparent blank space
    private static readonly Color32 Red    = new Color32(235, 64,  52,  255);
    private static readonly Color32 Pink   = new Color32(255, 110, 180, 255);
    private static readonly Color32 Green  = new Color32(46,  204, 113, 255);
    private static readonly Color32 Brown  = new Color32(139, 69,  19,  255);
    private static readonly Color32 Gold   = new Color32(241, 196, 15,  255);
    private static readonly Color32 Orange = new Color32(243, 156, 18,  255);
    private static readonly Color32 Cyan   = new Color32(52,  152, 219, 255);
    private static readonly Color32 Purple = new Color32(155, 89,  182, 255);

    [MenuItem("Tools/Generate Shaped Levels (With Blank Spaces)")]
    public static void GenerateAll()
    {
        string folderPath = Application.dataPath + "/Project/Sprites/Levels/ShapedLevels";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        SaveTexture(CreateHeart9x9(),  "Assets/Project/Sprites/Levels/ShapedLevels/Shape_Heart_9x9.png");
        SaveTexture(CreateApple10x10(), "Assets/Project/Sprites/Levels/ShapedLevels/Shape_Apple_10x10.png");
        SaveTexture(CreateStar11x11(),  "Assets/Project/Sprites/Levels/ShapedLevels/Shape_Star_11x11.png");
        SaveTexture(CreateDonut10x10(), "Assets/Project/Sprites/Levels/ShapedLevels/Shape_Donut_10x10.png");

        AssetDatabase.Refresh();
        Debug.Log("<color=green>Successfully generated 4 transparent shaped level textures inside Assets/Project/Sprites/Levels/ShapedLevels/!</color>");
    }

    // 1. Pixel Heart (9x9) - Top lobes and tapered bottom with blank borders
    private static Texture2D CreateHeart9x9()
    {
        Texture2D tex = new Texture2D(9, 9, TextureFormat.RGBA32, false);
        // Note: y=0 is the bottom row, y=8 is the top row
        int[,] map = new int[,]
        {
            { 0,0,0,0,1,0,0,0,0 }, // y = 0 (bottom tip)
            { 0,0,0,1,2,1,0,0,0 }, // y = 1
            { 0,0,1,2,2,2,1,0,0 }, // y = 2
            { 0,1,2,2,2,2,2,1,0 }, // y = 3
            { 1,2,2,2,2,2,2,2,1 }, // y = 4
            { 1,2,2,2,2,2,2,2,1 }, // y = 5
            { 1,1,2,2,1,2,2,1,1 }, // y = 6
            { 0,1,1,1,0,1,1,1,0 }, // y = 7 (lobes)
            { 0,0,0,0,0,0,0,0,0 }  // y = 8 (empty top)
        };

        ApplyMap(tex, map, new Color32[] { Clear, Red, Pink });
        return tex;
    }

    // 2. Apple with Leaf and Stem (10x10)
    private static Texture2D CreateApple10x10()
    {
        Texture2D tex = new Texture2D(10, 10, TextureFormat.RGBA32, false);
        int[,] map = new int[,]
        {
            { 0,0,1,1,0,0,1,1,0,0 }, // y = 0 (bottom curve)
            { 0,1,1,1,1,1,1,1,1,0 }, // y = 1
            { 1,1,1,1,1,1,1,1,1,1 }, // y = 2
            { 1,1,1,1,1,1,1,1,1,1 }, // y = 3
            { 1,1,1,1,1,1,1,1,1,1 }, // y = 4
            { 1,1,1,1,1,1,1,1,1,1 }, // y = 5
            { 0,1,1,1,1,1,1,1,1,0 }, // y = 6
            { 0,0,1,1,0,0,1,1,0,0 }, // y = 7 (top indent)
            { 0,0,0,0,2,3,3,0,0,0 }, // y = 8 (stem + leaf)
            { 0,0,0,0,2,0,0,0,0,0 }  // y = 9 (stem tip)
        };

        ApplyMap(tex, map, new Color32[] { Clear, Red, Brown, Green });
        return tex;
    }

    // 3. Five-Point Star (11x11)
    private static Texture2D CreateStar11x11()
    {
        Texture2D tex = new Texture2D(11, 11, TextureFormat.RGBA32, false);
        int[,] map = new int[,]
        {
            { 1,1,0,0,0,0,0,0,0,1,1 }, // y = 0 (bottom feet)
            { 0,1,1,0,0,0,0,0,1,1,0 }, // y = 1
            { 0,0,1,1,1,0,1,1,1,0,0 }, // y = 2
            { 0,0,0,1,2,2,2,1,0,0,0 }, // y = 3
            { 0,0,1,2,2,2,2,2,1,0,0 }, // y = 4
            { 1,1,2,2,2,2,2,2,2,1,1 }, // y = 5 (horizontal arm tips)
            { 0,0,1,2,2,2,2,2,1,0,0 }, // y = 6
            { 0,0,0,1,2,2,2,1,0,0,0 }, // y = 7
            { 0,0,0,0,1,2,1,0,0,0,0 }, // y = 8
            { 0,0,0,0,1,1,1,0,0,0,0 }, // y = 9
            { 0,0,0,0,0,1,0,0,0,0,0 }  // y = 10 (top tip)
        };

        ApplyMap(tex, map, new Color32[] { Clear, Gold, Orange });
        return tex;
    }

    // 4. Hollow Donut / Ring (10x10) - Tests both exterior AND interior blank spaces
    private static Texture2D CreateDonut10x10()
    {
        Texture2D tex = new Texture2D(10, 10, TextureFormat.RGBA32, false);
        int[,] map = new int[,]
        {
            { 0,0,0,1,1,1,1,0,0,0 }, // y = 0
            { 0,0,1,2,2,2,2,1,0,0 }, // y = 1
            { 0,1,2,2,2,2,2,2,1,0 }, // y = 2
            { 1,2,2,0,0,0,0,2,2,1 }, // y = 3 (empty hole starts)
            { 1,2,2,0,0,0,0,2,2,1 }, // y = 4
            { 1,2,2,0,0,0,0,2,2,1 }, // y = 5
            { 1,2,2,0,0,0,0,2,2,1 }, // y = 6 (empty hole ends)
            { 0,1,2,2,2,2,2,2,1,0 }, // y = 7
            { 0,0,1,2,2,2,2,1,0,0 }, // y = 8
            { 0,0,0,1,1,1,1,0,0,0 }  // y = 9
        };

        ApplyMap(tex, map, new Color32[] { Clear, Cyan, Purple });
        return tex;
    }

    private static void ApplyMap(Texture2D tex, int[,] map, Color32[] palette)
    {
        int height = map.GetLength(0);
        int width = map.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int colorIndex = map[y, x];
                tex.SetPixel(x, y, palette[colorIndex]);
            }
        }
        tex.Apply();
    }

    private static void SaveTexture(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point; // Exact pixel sampling
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
#endif
