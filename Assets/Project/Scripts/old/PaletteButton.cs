using UnityEngine;
using UnityEngine.UI;

// Purpose: In the older color-by-number prototype, colors a palette button and selects its color.
[RequireComponent(typeof(Button), typeof(Image))]
public class PaletteButton : MonoBehaviour
{
    [SerializeField] private int colorId = 1;
    [SerializeField] private Text numberText;

    private Button button;
    private Image buttonImage;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
    }

    void Start()
    {
        // Tint the button to match its color ID
        if (DiamondArtManager.Instance != null)
        {
            buttonImage.color = DiamondArtManager.Instance.GetColorById(colorId);
        }

        if (numberText != null)
        {
            numberText.text = colorId.ToString();
        }

        button.onClick.AddListener(() =>
        {
            DiamondArtManager.Instance.SelectColor(colorId);
        });
    }
}
