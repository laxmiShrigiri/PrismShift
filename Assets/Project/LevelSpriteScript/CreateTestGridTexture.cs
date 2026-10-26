using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

// Purpose: Adds an Editor menu command that creates a simple 12-by-12 test grid texture.
public class CreateTestGridTexture : MonoBehaviour
{
    [MenuItem("Tools/Generate 12x12 Four-Color Grid PNG")]
    public static void Generate()
    {
        int width = 12;
        int height = 12;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // 4 distinct solid colors for each 6x6 quadrant
        Color32 red    = new Color32(235, 64, 52, 255);   // Top-Left
        Color32 orange = new Color32(245, 161, 39, 255);  // Top-Right
        Color32 purple = new Color32(142, 68, 173, 255);  // Bottom-Left
        Color32 blue   = new Color32(41, 128, 185, 255);  // Bottom-Right

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 chosenColor;

                if (x < 6 && y >= 6)
                {
                    chosenColor = red;       // Top-Left (6x6)
                }
                else if (x >= 6 && y >= 6)
                {
                    chosenColor = orange;    // Top-Right (6x6)
                }
                else if (x < 6 && y < 6)
                {
                    chosenColor = purple;    // Bottom-Left (6x6)
                }
                else
                {
                    chosenColor = blue;      // Bottom-Right (6x6)
                }

                texture.SetPixel(x, y, chosenColor);
            }
        }

        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();
        string path = Application.dataPath + "/TestGrid_12x12.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        // Configure texture settings automatically for crisp pixel grid reads
        TextureImporter importer = AssetImporter.GetAtPath("Assets/TestGrid_12x12.png") as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Debug.Log("Created clean 12x12 grid PNG at: " + path);
    }
}
#endif
