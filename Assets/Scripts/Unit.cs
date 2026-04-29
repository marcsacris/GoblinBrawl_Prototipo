using UnityEngine;

/// <summary>
/// Representa a un combatiente (Goblin del jugador o enemigo).
/// Contiene sus estadísticas y la lógica para recibir daño y curar.
/// Este script se arrastra directamente sobre el GameObject del Goblin en la escena.
/// </summary>
public class Unit : MonoBehaviour
{
    // ─── Estadísticas Base (configurables desde el Inspector de Unity) ───────
    [Header("Información del Personaje")]
    public string unitName;       // Nombre que aparecerá en la HUD

    [Header("Estadísticas")]
    public int maxHP;             // Puntos de Vida máximos
    public int currentHP;         // Puntos de Vida actuales

    public int maxMP;             // Puntos de Maná máximos
    public int currentMP;         // Puntos de Maná actuales

    public int strength;          // Fuerza: determina el daño físico base
    public int magicPower;        // Poder mágico: determina el daño/curación de habilidades
    public int magicCost;         // Coste en MP de la habilidad mágica

    // ─── Inicialización ──────────────────────────────────────────────────────
    private void Start()
    {
        // Al inicio, la vida y el maná actuales son iguales al máximo
        currentHP = maxHP;
        currentMP = maxMP;
    }

    // ─── Métodos de Combate ──────────────────────────────────────────────────

    /// <summary>
    /// Recibe daño físico. Resta el daño al HP actual y lo clampea a 0 como mínimo.
    /// Devuelve 'true' si la unidad muere (HP <= 0).
    /// </summary>
    public bool TakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0); // Evita que el HP sea negativo
        return currentHP <= 0; // Devuelve true si la unidad ha muerto
    }

    /// <summary>
    /// Restaura HP al personaje. No permite superar el máximo de vida.
    /// </summary>
    public void Heal(int healAmount)
    {
        currentHP += healAmount;
        currentHP = Mathf.Min(currentHP, maxHP); // No superar el máximo
    }

    /// <summary>
    /// Comprueba si la unidad tiene suficiente MP para usar la habilidad
    /// y lo gasta si es posible. Devuelve true si pudo hacerlo.
    /// </summary>
    public bool SpendMP(int amount)
    {
        if (currentMP >= amount)
        {
            currentMP -= amount;
            return true; // Tenía suficiente MP
        }
        return false; // No tenía suficiente MP
    }
}
