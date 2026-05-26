using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa a un combatiente (Goblin del jugador o enemigo).
/// Contiene sus estadísticas, lista de habilidades, lógica de estado (veneno, defensa)
/// y el soporte para pasivas de clase.
/// La clase se asigna dinámicamente al inicio del combate mediante ApplyClassData().
/// </summary>
public class Unit : MonoBehaviour
{
    // ─── Información del Personaje ────────────────────────────────────────────
    [Header("Información del Personaje")]
    public string unitName;             // Nombre base del goblin (ej: "Grog")

    [Header("Clase (asignada en runtime por BattleSystem)")]
    [Tooltip("Se rellena automáticamente al inicio del combate. No hace falta asignarlo.")]
    public GoblinClassData classData;   // Clase actual del goblin (con stats, skills, IA)

    // ─── Estadísticas Base ────────────────────────────────────────────────────
    [Header("Estadísticas")]
    public int maxHP;                   // Puntos de Vida máximos
    public int currentHP;               // Puntos de Vida actuales

    public int maxMP;                   // Puntos de Maná máximos
    public int currentMP;               // Puntos de Maná actuales

    public int strength;                // Fuerza: escala daño físico
    public int magicPower;              // Poder Mágico: escala daño/curación mágica
    public int speed;                   // Velocidad: determina el orden de turnos
    public int defense;                 // Defensa base: reduce el daño físico recibido

    // ─── Habilidades (configurable desde el Inspector) ───────────────────────
    [Header("Habilidades")]
    [Tooltip("Arrastra aquí los ScriptableObject SkillData de las habilidades de este goblin.")]
    public List<SkillData> skills = new List<SkillData>();

    // ─── Estado de Combate (runtime) ─────────────────────────────────────────
    [Header("Estado de Combate (solo lectura en runtime)")]
    [SerializeField] private int poisonTurnsRemaining = 0;  // Turnos de veneno restantes
    [SerializeField] private int poisonDamagePerTurn  = 0;  // Daño de veneno por turno
    [SerializeField] private bool isDefending         = false; // ¿Está en postura de defensa?

    // ─── Pasiva: bonus de defensa temporal (Ironhide del Guerrero) ───────────
    [SerializeField] private int tempDefenseBonus = 0;      // Bonus temporal hasta su próximo turno

    // ─── Propiedades Públicas ─────────────────────────────────────────────────
    public bool IsDead              => currentHP <= 0;
    public bool IsDefending         => isDefending;
    public bool IsPoisoned          => poisonTurnsRemaining > 0;
    public int  PoisonTurnsRemaining => poisonTurnsRemaining;
    public int  PoisonDamagePerTurn  => poisonDamagePerTurn;
    public int  TempDefenseBonus    => tempDefenseBonus;

    // ─── Inicialización ───────────────────────────────────────────────────────
    // NOTA: No inicializamos stats en Start() porque ApplyClassData() se llama
    // desde BattleSystem.SetupBattle() antes de que empiece el combate.

    /// <summary>
    /// Aplica los datos de una clase a este goblin, sobreescribiendo stats y habilidades.
    /// Se llama desde BattleSystem al inicio del combate con una clase aleatoria.
    /// </summary>
    public void ApplyClassData(GoblinClassData data)
    {
        classData = data;

        // Aplicar stats base con variación aleatoria (±statVariation)
        int v = data.statVariation;
        maxHP      = data.baseHP       + Random.Range(-v, v + 1);
        maxMP      = data.baseMP       + Random.Range(-v, v + 1);
        strength   = data.baseStrength + Random.Range(-v, v + 1);
        magicPower = data.baseMagicPower + Random.Range(-v, v + 1);
        speed      = data.baseSpeed    + Random.Range(-v, v + 1);
        defense    = data.baseDefense  + Random.Range(-v, v + 1);

        // Asegurar mínimos razonables
        maxHP = Mathf.Max(maxHP, 1);
        maxMP = Mathf.Max(maxMP, 0);
        strength = Mathf.Max(strength, 1);
        speed = Mathf.Max(speed, 1);
        defense = Mathf.Max(defense, 0);

        // Resetear vida y maná al máximo
        currentHP = maxHP;
        currentMP = maxMP;

        // Copiar las habilidades de la clase
        skills = new List<SkillData>(data.classSkills);

        // Resetear estados de combate
        poisonTurnsRemaining = 0;
        poisonDamagePerTurn  = 0;
        isDefending   = false;
        tempDefenseBonus = 0;
    }

    // ─── Métodos de Combate ───────────────────────────────────────────────────

    /// <summary>
    /// Recibe daño. Si está defendiendo, el daño físico se reduce a la mitad (+ defensa).
    /// Suma el bonus temporal de defensa (pasiva Ironhide) al cálculo.
    /// Devuelve el daño real aplicado.
    /// </summary>
    public int TakeDamage(int rawDamage, bool isPhysical = true)
    {
        int finalDamage = rawDamage;

        if (isPhysical)
        {
            // La defensa base + bonus temporal reduce el daño
            finalDamage -= (defense + tempDefenseBonus);
            if (isDefending) finalDamage = Mathf.RoundToInt(finalDamage * 0.5f);
        }

        finalDamage = Mathf.Max(finalDamage, 1); // Mínimo 1 de daño siempre
        currentHP -= finalDamage;
        currentHP = Mathf.Max(currentHP, 0);
        return finalDamage;
    }

    /// <summary>
    /// Restaura HP al personaje. No permite superar el máximo.
    /// Devuelve la cantidad real curada.
    /// </summary>
    public int Heal(int healAmount)
    {
        int realHeal = Mathf.Min(healAmount, maxHP - currentHP);
        currentHP += realHeal;
        return realHeal;
    }

    /// <summary>
    /// Gasta MP. Devuelve true si tenía suficiente.
    /// </summary>
    public bool SpendMP(int amount)
    {
        if (currentMP >= amount)
        {
            currentMP -= amount;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Restaura MP (usado por la pasiva TideCaller del Chamán).
    /// No permite superar el máximo.
    /// </summary>
    public void RestoreMP(int amount)
    {
        currentMP = Mathf.Min(currentMP + amount, maxMP);
    }

    /// <summary>
    /// Aplica estado de veneno al personaje.
    /// Si ya está envenenado, refresca la duración si la nueva es mayor.
    /// </summary>
    public void ApplyPoison(int damagePerTurn, int turns)
    {
        poisonDamagePerTurn  = Mathf.Max(poisonDamagePerTurn, damagePerTurn);
        poisonTurnsRemaining = Mathf.Max(poisonTurnsRemaining, turns);
    }

    /// <summary>
    /// Extiende la duración del veneno activo en extraTurns turnos adicionales.
    /// Pasiva VenomousStrike del Pícaro.
    /// Solo actúa si el objetivo ya está envenenado.
    /// </summary>
    public bool ExtendPoison(int extraTurns)
    {
        if (!IsPoisoned) return false;
        poisonTurnsRemaining += extraTurns;
        return true;
    }

    /// <summary>
    /// Procesa el veneno al inicio del turno de este personaje.
    /// Devuelve el daño de veneno aplicado (0 si no está envenenado).
    /// </summary>
    public int ProcessPoisonTick()
    {
        if (poisonTurnsRemaining <= 0) return 0;

        int dmg = poisonDamagePerTurn;
        currentHP -= dmg;
        currentHP = Mathf.Max(currentHP, 0);
        poisonTurnsRemaining--;

        if (poisonTurnsRemaining <= 0)
        {
            poisonDamagePerTurn = 0;
        }

        return dmg;
    }

    /// <summary>
    /// Activa la postura de defensa para este turno.
    /// </summary>
    public void SetDefending(bool defending)
    {
        isDefending = defending;
    }

    /// <summary>
    /// Añade un bonus temporal de defensa (pasiva Ironhide del Guerrero).
    /// Se acumula; usa ClearTempDefense() para resetearlo.
    /// </summary>
    public void AddTempDefense(int bonus)
    {
        tempDefenseBonus += bonus;
    }

    /// <summary>
    /// Resetea el bonus temporal de defensa al inicio del turno de este personaje.
    /// </summary>
    public void ClearTempDefense()
    {
        tempDefenseBonus = 0;
    }

    /// <summary>
    /// Calcula el daño de una habilidad según su tipo y las stats del usuario.
    /// </summary>
    public int CalculateSkillDamage(SkillData skill)
    {
        float baseStat = (skill.skillType == SkillType.Physical || skill.skillType == SkillType.PhysicalPoison)
            ? strength
            : magicPower;

        return Mathf.RoundToInt(baseStat * skill.powerMultiplier) + skill.flatBonus;
    }
}
