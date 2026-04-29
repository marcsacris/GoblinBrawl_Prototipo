using UnityEngine;
using UnityEngine.UI;
using TMPro; // Importamos TextMeshPro para textos de mayor calidad

/// <summary>
/// Gestiona la Interfaz de Usuario (HUD) de una unidad (barras de vida, maná y nombre).
/// Hay dos instancias de este script: una para el jugador y otra para el enemigo.
/// Se conecta a los sliders y textos del Canvas desde el Inspector.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    // ─── Referencias a la UI (arrastrar desde el Inspector) ─────────────────
    [Header("Elementos de la HUD")]
    public TMP_Text nameText;       // Text que muestra el nombre de la unidad
    public Slider hpSlider;         // Barra deslizante para los Puntos de Vida
    public Slider mpSlider;         // Barra deslizante para los Puntos de Maná
    public TMP_Text hpText;         // Text que muestra "HP: X / MaxX"
    public TMP_Text mpText;         // Text que muestra "MP: X / MaxX"

    // ─── Métodos Públicos ────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa la HUD con los datos de la unidad al comienzo del combate.
    /// </summary>
    public void SetHUD(Unit unit)
    {
        nameText.text = unit.unitName;

        // Configuramos los Sliders con los valores máximos y actuales
        hpSlider.maxValue = unit.maxHP;
        hpSlider.value = unit.currentHP;
        mpSlider.maxValue = unit.maxMP;
        mpSlider.value = unit.currentMP;

        // Actualizamos los textos numéricos
        UpdateHP(unit);
        UpdateMP(unit);
    }

    /// <summary>
    /// Actualiza la barra y el texto de HP. Se llama cada vez que cambia la vida.
    /// </summary>
    public void UpdateHP(Unit unit)
    {
        // Animación suave del slider (puedes usar un Coroutine aquí para mayor polish)
        hpSlider.value = unit.currentHP;
        hpText.text = $"HP: {unit.currentHP} / {unit.maxHP}";
    }

    /// <summary>
    /// Actualiza la barra y el texto de MP. Se llama cada vez que cambia el maná.
    /// </summary>
    public void UpdateMP(Unit unit)
    {
        mpSlider.value = unit.currentMP;
        mpText.text = $"MP: {unit.currentMP} / {unit.maxMP}";
    }
}
