using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Purpose: In the older prototype, stores the color palette and tracks when all paint-by-number tiles are filled.
public class DiamondArtManager : MonoBehaviour
{
    public static DiamondArtManager Instance { get; private set; }

    [System.Serializable]
    public struct GemColor
    {
        public int colorId;       // e.g., 1, 2, 3, 4
        public Color colorValue;  // The actual color
    }

    [Header("Color Palette Definition")]
    public List<GemColor> palette = new List<GemColor>()
    {
        new GemColor { colorId = 1, colorValue = new Color(0.92f, 0.15f, 0.15f) }, // 1: Red
        new GemColor { colorId = 2, colorValue = new Color(1.0f,  0.88f, 0.10f) }, // 2: Yellow
        new GemColor { colorId = 3, colorValue = new Color(0.12f, 0.45f, 0.90f) }, // 3: Blue
        new GemColor { colorId = 4, colorValue = new Color(0.18f, 0.78f, 0.25f) }  // 4: Green
    };

    [Header("Runtime State")]
    public int selectedColorId = 1;

    [Header("Win State")]
    private int totalTiles = 0;
    private int filledTiles = 0;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterTile()
    {
        totalTiles++;
    }

    public void OnTileFilled()
    {
        filledTiles++;
        if (filledTiles >= totalTiles && totalTiles > 0)
        {
            Debug.Log("Level Complete!");
        }
    }

    public void SelectColor(int id)
    {
        selectedColorId = id;
    }

    public Color GetColorById(int id)
    {
        foreach (var item in palette)
        {
            if (item.colorId == id) return item.colorValue;
        }
        return Color.white;
    }
}
