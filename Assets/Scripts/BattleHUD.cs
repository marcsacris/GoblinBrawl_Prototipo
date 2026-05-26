using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona el HUD de una sola unidad: barras de vida/maná, nombre, clase,
/// indicadores de veneno y defensa. Hay 6 instancias en el combate 3v3.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    // ─── Referencias de la UI (asignar desde el Inspector) ───────────────────
    [Header("Elementos de la HUD")]
    public TMP_Text nameText;           // Nombre del goblin
    public TMP_Text classText;          // Clase (Guerrero / Chamán / Pícaro)
    public Slider hpSlider;             // Barra de Vida
    public Slider mpSlider;             // Barra de Maná
    public TMP_Text hpText;             // Texto "HP: X / Max"
    public TMP_Text mpText;             // Texto "MP: X / Max"

    [Header("Textos debajo del sprite (opcional)")]
    public TMP_Text spriteNameText;     // Nombre mostrado debajo del sprite del goblin
    public TMP_Text spriteClassText;    // Clase mostrada debajo del sprite del goblin

    [Header("Indicadores de Estado")]
    public GameObject poisonIndicator;  // Panel/Icono verde de veneno (puede ser null)
    public GameObject defendIndicator;  // Panel/Icono azul de defensa (puede ser null)
    public GameObject activeIndicator;  // Borde/Glow que indica que es el turno de esta unidad
    public Image panelImage;            // Imagen del panel para cambiar color al seleccionar

    [Header("Colores")]
    public Color normalColor   = new Color(0.15f, 0.15f, 0.25f, 0.9f);
    public Color activeColor   = new Color(0.30f, 0.55f, 0.30f, 0.95f);
    public Color deadColor     = new Color(0.20f, 0.08f, 0.08f, 0.7f);
    public Color selectedColor = new Color(0.55f, 0.45f, 0.10f, 0.95f); // Dorado (objetivo seleccionado)

    // ─── Estado interno ───────────────────────────────────────────────────────
    private Coroutine hpCoroutine;
    private Coroutine mpCoroutine;

    // ─── API Pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa la HUD con los datos de la unidad al comienzo del combate.
    /// </summary>
    public void SetHUD(Unit unit)
    {
        nameText.text = unit.unitName;

        if (classText != null)
            classText.text = GetClassLabel(unit);

        // Actualizar también los textos debajo del sprite si están asignados
        if (spriteNameText != null)
            spriteNameText.text = unit.unitName;

        if (spriteClassText != null)
            spriteClassText.text = GetClassLabel(unit);

        hpSlider.maxValue = unit.maxHP;
        hpSlider.value    = unit.currentHP;
        mpSlider.maxValue = unit.maxMP;
        mpSlider.value    = unit.currentMP;

        UpdateHP(unit);
        UpdateMP(unit);
        UpdateStatusIndicators(unit);
        SetActive(false);
        SetSelected(false);
    }

    /// <summary>
    /// Actualiza la barra y el texto de HP con animación suave.
    /// </summary>
    public void UpdateHP(Unit unit)
    {
        hpText.text = $"HP: {unit.currentHP} / {unit.maxHP}";

        if (hpCoroutine != null) StopCoroutine(hpCoroutine);
        hpCoroutine = StartCoroutine(AnimateSlider(hpSlider, unit.currentHP));

        // Si el goblin murió, oscurecer el panel
        if (unit.IsDead && panelImage != null)
            panelImage.color = deadColor;
    }

    /// <summary>
    /// Actualiza la barra y el texto de MP con animación suave.
    /// </summary>
    public void UpdateMP(Unit unit)
    {
        mpText.text = $"MP: {unit.currentMP} / {unit.maxMP}";

        if (mpCoroutine != null) StopCoroutine(mpCoroutine);
        mpCoroutine = StartCoroutine(AnimateSlider(mpSlider, unit.currentMP));
    }

    /// <summary>
    /// Actualiza los indicadores visuales de veneno y defensa.
    /// </summary>
    public void UpdateStatusIndicators(Unit unit)
    {
        if (poisonIndicator != null)
            poisonIndicator.SetActive(unit.IsPoisoned);

        if (defendIndicator != null)
            defendIndicator.SetActive(unit.IsDefending);
    }

    /// <summary>
    /// Marca esta unidad como la que tiene el turno activo (resalta su panel).
    /// </summary>
    public void SetActive(bool isActive)
    {
        if (activeIndicator != null)
            activeIndicator.SetActive(isActive);

        if (panelImage != null)
            panelImage.color = isActive ? activeColor : normalColor;
    }

    /// <summary>
    /// Marca esta unidad como objetivo seleccionado por el jugador (resalta en dorado).
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (panelImage != null && !isSelected)
            panelImage.color = normalColor;
        else if (panelImage != null && isSelected)
            panelImage.color = selectedColor;
    }

    // ─── Privado ──────────────────────────────────────────────────────────────

    private IEnumerator AnimateSlider(Slider slider, float targetValue)
    {
        float startValue = slider.value;
        float elapsed = 0f;
        const float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            slider.value = Mathf.Lerp(startValue, targetValue, elapsed / duration);
            yield return null;
        }

        slider.value = targetValue;
    }

    private string GetClassLabel(Unit unit)
    {
        if (unit.classData != null)
            return $"{unit.classData.classEmoji} {unit.classData.className}";
        return "?";
    }
}
