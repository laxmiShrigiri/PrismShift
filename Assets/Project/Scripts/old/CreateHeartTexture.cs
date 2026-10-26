using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

// Purpose: Adds an Editor menu command that generates an older 12-by-12 pixel-art heart texture.
public class CreateHeartTexture : MonoBehaviour
{
    [MenuItem("Tools/Generate 12x12 Heart PNG")]
    public static void Generate()
    {
        int width = 12;
        int height = 12;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color clear = new Color(0, 0, 0, 0);
        Color border = new Color32(0x23, 0x1F, 0x2A, 255); // Dark outline
        Color red    = new Color32(0xDF, 0x3E, 0x5D, 255); // Red/pink top
        Color orange = new Color32(0xF2, 0x9B, 0x0A, 255); // Orange middle
        Color purple = new Color32(0x8D, 0x4D, 0x7E, 255); // Purple bottom

        // 12x12 grid layout from top (row 11) to bottom (row 0)
        // . = Clear, B = Border, R = Red, O = Orange, P = Purple
        string[] rows = new string[]
        {
            "..BB....BB..", // Row 11 (Top bumps)
            ".BRRB..BRRB.", // Row 10
            "BRRRRBBBRRRB", // Row 9
            "BRRRRRRRRRRB", // Row 8
            "BOOOOOOOOOOB", // Row 7
            "BOOOOOOOOOOB", // Row 6
            "BOOOOOOOOOOB", // Row 5
            ".BPPPPPPPBB.", // Row 4
            "..BPPPPPBB..", // Row 3
            "...BPPPBB...", // Row 2
            "....BPBB....", // Row 1
            ".....BB....."  // Row 0 (Tip)
        };

        for (int y = 0; y < 12; y++)
        {
            string row = rows[11 - y]; // Convert row to bottom-up Y coordinate
            for (int x = 0; x < 12; x++)
            {
                char c = row[x];
                Color pixelColor = clear;
                if (c == 'B') pixelColor = border;
                else if (c == 'R') pixelColor = red;
                else if (c == 'O') pixelColor = orange;
                else if (c == 'P') pixelColor = purple;

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();
        string path = Application.dataPath + "/Heart_12x12.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        Debug.Log("Created true 12x12 PNG at: " + path);
    }
}
#endif
