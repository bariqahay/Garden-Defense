using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Tambahkan script ini ke SETIAP BUTTON untuk visual feedback yang lebih jelas
/// </summary>
public class ButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("Highlight Settings")]
    public float selectedScale = 1.1f; // Ukuran saat selected
    public Color selectedOutlineColor = Color.yellow;
    public bool useOutline = true;
    public bool useScale = true;

    private Vector3 originalScale;
    private Outline outline;

    void Start()
    {
        originalScale = transform.localScale;

        // Get atau add Outline component ke Text child
        if (useOutline)
        {
            Text textComponent = GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                outline = textComponent.GetComponent<Outline>();
                if (outline == null)
                    outline = textComponent.gameObject.AddComponent<Outline>();

                outline.effectColor = selectedOutlineColor;
                outline.effectDistance = new Vector2(2, -2);
                outline.enabled = false; // Mulai disabled
            }
        }
    }

    // Dipanggil saat button di-select (dengan controller atau mouse)
    public void OnSelect(BaseEventData eventData)
    {
        // Scale up
        if (useScale)
        {
            transform.localScale = originalScale * selectedScale;
        }

        // Enable outline
        if (useOutline && outline != null)
        {
            outline.enabled = true;
        }

        Debug.Log($"[ButtonHighlight] {gameObject.name} selected");
    }

    // Dipanggil saat button di-deselect
    public void OnDeselect(BaseEventData eventData)
    {
        // Scale back to normal
        if (useScale)
        {
            transform.localScale = originalScale;
        }

        // Disable outline
        if (useOutline && outline != null)
        {
            outline.enabled = false;
        }

        Debug.Log($"[ButtonHighlight] {gameObject.name} deselected");
    }
}