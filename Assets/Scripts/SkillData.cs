using UnityEngine;

/// <summary>
/// ScriptableObject que define una habilidad especial de un Goblin.
/// Crea instancias desde el menú: Assets → Create → GoblinBrawl → Skill
/// Asigna las Skills al campo "skills" de cada Unit desde el Inspector.
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "GoblinBrawl/Skill")]
public class SkillData : ScriptableObject
{
    // ─── Información General ──────────────────────────────────────────────────
    [Header("Información")]
    public string skillName;            // Nombre que aparece en el menú de habilidades
    [TextArea(2, 4)]
    public string description;          // Descripción breve (tooltip futuro)

    // ─── Tipo y Objetivo ──────────────────────────────────────────────────────
    [Header("Tipo de Habilidad")]
    public SkillType skillType;         // Qué hace la habilidad
    public TargetType targetType;       // A quién afecta

    // ─── Costes ───────────────────────────────────────────────────────────────
    [Header("Coste")]
    public int mpCost;                  // Maná necesario para lanzarla

    // ─── Valores de Efecto ────────────────────────────────────────────────────
    [Header("Efecto")]
    [Tooltip("Multiplicador sobre la stat correspondiente (Fuerza para físico, Magia para mágico/curación).")]
    public float powerMultiplier = 1f;  // El daño/curación = stat × powerMultiplier

    [Tooltip("Bonificación fija de daño/curación, independiente de las stats.")]
    public int flatBonus = 0;           // Bonus plano adicional

    // ─── Veneno (solo SkillType.Poison) ──────────────────────────────────────
    [Header("Veneno (solo si SkillType = Poison o PhysicalPoison)")]
    public int poisonDamagePerTurn = 0; // Daño por turno que aplica el veneno
    public int poisonDuration = 0;      // Número de turnos que dura el veneno
}

/// <summary>
/// Qué tipo de acción realiza la habilidad.
/// </summary>
public enum SkillType
{
    Physical,       // Daño físico escalado con Fuerza
    Magical,        // Daño mágico escalado con Magia
    Heal,           // Curación escalada con Magia
    Poison,         // Aplica veneno (daño continuo por turnos)
    PhysicalPoison, // Daño físico + aplica veneno
    BuffDefend,     // Mejora temporal la defensa propia (variante de Defender con magia)
}

/// <summary>
/// A quién va dirigida la habilidad.
/// </summary>
public enum TargetType
{
    SingleEnemy,    // El jugador elige un enemigo concreto
    AllEnemies,     // Afecta a todos los enemigos a la vez
    SingleAlly,     // El jugador elige un aliado concreto
    Self,           // Solo se aplica a uno mismo
    AllAllies,      // Afecta a todos los aliados
}
