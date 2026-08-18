using UnityEngine;

public class DiceController : MonoBehaviour
{
    public int CurrentValue { get; set; } = 0;
    public bool IsSelected { get; private set; } = false;
    public bool IsLocked { get; private set; } = false;

    [Header("Настройки подсветки")]
    public Color highlightColor = Color.green;

    private Renderer diceRenderer;
    private Color originalColor;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        diceRenderer = GetComponent<Renderer>();
        if (diceRenderer == null) diceRenderer = GetComponentInChildren<Renderer>();

        if (diceRenderer != null)
        {
            if (diceRenderer.material.HasProperty("_BaseColor"))
                originalColor = diceRenderer.material.GetColor("_BaseColor");
            else
                originalColor = diceRenderer.material.color;
        }
    }

    public void ToggleSelection()
    {
        if (IsLocked || CurrentValue == 0) return;

        IsSelected = !IsSelected;
        UpdateColor();
    }

    public void LockDice()
    {
        IsLocked = true;
        IsSelected = false;

        if (rb != null) rb.isKinematic = true;

        // ВИЗУАЛ: Прячем кубик со сцены (откладываем)
        gameObject.SetActive(false);
    }

    public void ResetDice()
    {
        IsLocked = false;
        IsSelected = false;
        CurrentValue = 0;

        // ИСПРАВЛЕНИЕ: Прячем кубики со стола при сбросе хода! 
        // Они снова станут видимыми только в момент физического броска из руки.
        gameObject.SetActive(false);

        if (rb != null) rb.isKinematic = false;
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (diceRenderer == null) return;

        Color targetColor = IsSelected ? highlightColor : originalColor;

        if (diceRenderer.material.HasProperty("_BaseColor"))
            diceRenderer.material.SetColor("_BaseColor", targetColor);
        else
            diceRenderer.material.color = targetColor;
    }
}