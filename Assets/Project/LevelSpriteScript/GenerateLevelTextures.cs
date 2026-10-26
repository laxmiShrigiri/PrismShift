using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

// Purpose: Adds an Editor menu command that generates the project's colored grid level textures.
public class GenerateLevelTextures : MonoBehaviour
{
    private static readonly Color32 Red    = new Color32(235, 64,  52,  255);
    private static readonly Color32 Orange = new Color32(245, 161, 39,  255);
    private static readonly Color32 Yellow = new Color32(241, 196, 15,  255);
    private static readonly Color32 Green  = new Color32(46,  204, 113, 255);
    private static readonly Color32 Blue   = new Color32(41,  128, 185, 255);
    private static readonly Color32 Purple = new Color32(142, 68,  173, 255);

    [MenuItem("Tools/Generate All Levels (5x5 to 12x12)")]
    public static void GenerateAllLevels()
    {
        string folderPath = Application.dataPath + "/Project/Sprites/Levels";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        SaveLevelTexture(Generate5x5(),   "Assets/Project/Sprites/Levels/Level_01_5x5.png");
        SaveLevelTexture(Generate6x6(),   "Assets/Project/Sprites/Levels/Level_02_6x6.png");
        SaveLevelTexture(Generate7x7(),   "Assets/Project/Sprites/Levels/Level_03_7x7.png");
        SaveLevelTexture(Generate8x8(),   "Assets/Project/Sprites/Levels/Level_04_8x8.png");
        SaveLevelTexture(Generate9x9(),   "Assets/Project/Sprites/Levels/Level_05_9x9.png");
        SaveLevelTexture(Generate10x10(), "Assets/Project/Sprites/Levels/Level_06_10x10.png");
        SaveLevelTexture(Generate11x11(), "Assets/Project/Sprites/Levels/Level_07_11x11.png");
        SaveLevelTexture(Generate12x12(), "Assets/Project/Sprites/Levels/Level_08_12x12.png");

        AssetDatabase.Refresh();
        Debug.Log("<color=green>Successfully generated 8 level textures inside Assets/Project/Sprites/Levels/!</color>");
    }

    // 5x5: Bullseye Ring (2 colors)
    private static Texture2D Generate5x5()
    {
        Texture2D tex = new Texture2D(5, 5, TextureFormat.RGBA32, false);
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                int distFromEdge = Mathf.Min(Mathf.Min(x, 4 - x), Mathf.Min(y, 4 - y));
                tex.SetPixel(x, y, distFromEdge == 1 ? Yellow : Blue);
            }
        }
        tex.Apply();
        return tex;
    }

    // 6x6: 4 Quadrants (4 colors)
    private static Texture2D Generate6x6()
    {
        Texture2D tex = new Texture2D(6, 6, TextureFormat.RGBA32, false);
        for (int y = 0; y < 6; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                if (x < 3 && y >= 3)      tex.SetPixel(x, y, Red);
                else if (x >= 3 && y >= 3) tex.SetPixel(x, y, Orange);
                else if (x < 3 && y < 3)   tex.SetPixel(x, y, Purple);
                else                       tex.SetPixel(x, y, Green);
            }
        }
        tex.Apply();
        return tex;
    }

    // 7x7: Central Cross / Plus (3 colors)
    private static Texture2D Generate7x7()
    {
        Texture2D tex = new Texture2D(7, 7, TextureFormat.RGBA32, false);
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                if (x == 3 || y == 3)
                    tex.SetPixel(x, y, Yellow);
                else if ((x < 3 && y < 3) || (x > 3 && y > 3))
                    tex.SetPixel(x, y, Blue);
                else
                    tex.SetPixel(x, y, Red);
            }
        }
        tex.Apply();
        return tex;
    }

    // 8x8: 2x2 Checker Blocks (4 colors)
    private static Texture2D Generate8x8()
    {
        Texture2D tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        Color32[] palette = new Color32[] { Purple, Orange, Green, Blue };
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int blockX = x / 2;
                int blockY = y / 2;
                int colorIndex = (blockX + blockY * 2) % 4;
                tex.SetPixel(x, y, palette[colorIndex]);
            }
        }
        tex.Apply();
        return tex;
    }

    // 9x9: Diagonal Bands (3 colors)
    private static Texture2D Generate9x9()
    {
        Texture2D tex = new Texture2D(9, 9, TextureFormat.RGBA32, false);
        Color32[] palette = new Color32[] { Red, Yellow, Blue };
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                int band = (x + y) / 3;
                tex.SetPixel(x, y, palette[band % 3]);
            }
        }
        tex.Apply();
        return tex;
    }

    // 10x10: Concentric Rectangles (4 colors)
    private static Texture2D Generate10x10()
    {
        Texture2D tex = new Texture2D(10, 10, TextureFormat.RGBA32, false);
        Color32[] palette = new Color32[] { Purple, Green, Orange, Yellow };
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                int layer = Mathf.Min(Mathf.Min(x, 9 - x), Mathf.Min(y, 9 - y));
                tex.SetPixel(x, y, palette[layer % palette.Length]);
            }
        }
        tex.Apply();
        return tex;
    }

    // 11x11: Diamond Inset (4 colors)
    private static Texture2D Generate11x11()
    {
        Texture2D tex = new Texture2D(11, 11, TextureFormat.RGBA32, false);
        for (int y = 0; y < 11; y++)
        {
            for (int x = 0; x < 11; x++)
            {
                int manhattanDist = Mathf.Abs(x - 5) + Mathf.Abs(y - 5);
                if (manhattanDist <= 2)
                    tex.SetPixel(x, y, Yellow);
                else if (manhattanDist <= 4)
                    tex.SetPixel(x, y, Red);
                else if (manhattanDist <= 7)
                    tex.SetPixel(x, y, Blue);
                else
                    tex.SetPixel(x, y, Green);
            }
        }
        tex.Apply();
        return tex;
    }

    // 12x12: Complex Maze / Mosaic (5 colors)
    private static Texture2D Generate12x12()
    {
        Texture2D tex = new Texture2D(12, 12, TextureFormat.RGBA32, false);
        Color32[] palette = new Color32[] { Red, Blue, Green, Orange, Purple };

        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                // Multi-frequency math pattern creating organic winding regions
                int v = (int)(Mathf.Sin(x * 0.7f) * 3f + Mathf.Cos(y * 0.7f) * 3f + (x * y) / 10);
                int colorIndex = Mathf.Abs(v) % palette.Length;
                tex.SetPixel(x, y, palette[colorIndex]);
            }
        }
        tex.Apply();
        return tex;
    }

    private static void SaveLevelTexture(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point; // Crisp pixels
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
#endif
