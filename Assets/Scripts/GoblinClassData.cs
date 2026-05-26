using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject que define una clase de Goblin completa.
/// Contiene stats base, habilidades, nombre de clase, comportamiento de IA y pasiva de clase.
/// Para crear nuevas clases: Assets → Create → GoblinBrawl → GoblinClass
/// 
/// EXTENSIBILIDAD: Para añadir una nueva clase (ej. "Goblin Berserker"):
///   1. Crea un nuevo GoblinClassData desde el menú Assets → Create → GoblinBrawl → GoblinClass
///   2. Rellena las stats, habilidades y elige un AIBehavior existente
///   3. Arrástralo a la lista "classDatabase" del BattleSystem
///   4. ¡Listo! No necesitas tocar código.
///
///   Si necesitas un comportamiento de IA completamente nuevo,
///   añade un valor al enum AIBehavior y un case en EnemyAI.cs.
/// </summary>
[CreateAssetMenu(fileName = "NewGoblinClass", menuName = "GoblinBrawl/GoblinClass")]
public class GoblinClassData : ScriptableObject
{
    // ─── Identidad de la Clase ────────────────────────────────────────────────
    [Header("Identidad")]
    public string className;            // Ej: "Guerrero", "Chamán", "Pícaro", "Berserker"
    public string classEmoji;           // Ej: "⚔", "✨", "🗡", "🔥"
    [TextArea(1, 3)]
    public string classDescription;     // Ej: "Tanque que absorbe daño y golpea fuerte"

    // ─── Comportamiento de IA ─────────────────────────────────────────────────
    [Header("Comportamiento de IA")]
    [Tooltip("Determina cómo actúa la IA cuando un enemigo tiene esta clase.")]
    public AIBehavior aiBehavior;

    // ─── Habilidad Pasiva de Clase ────────────────────────────────────────────
    [Header("Pasiva de Clase")]
    [Tooltip("Habilidad pasiva que define el rasgo único de esta clase.")]
    public PassiveAbility passiveAbility = PassiveAbility.None;

    [Tooltip("Valor numérico asociado a la pasiva (bonus de defensa, turnos extra de veneno, etc.).")]
    public int passiveValue = 1;

    // ─── Estadísticas Base ────────────────────────────────────────────────────
    [Header("Estadísticas Base")]
    public int baseHP  = 100;
    public int baseMP  = 30;
    public int baseStrength   = 12;
    public int baseMagicPower = 8;
    public int baseSpeed      = 10;
    public int baseDefense    = 4;

    // ─── Variación aleatoria (para que dos Guerreros no sean idénticos) ──────
    [Header("Variación Aleatoria (±)")]
    [Tooltip("Rango de variación aleatoria sobre cada stat base.")]
    public int statVariation = 3;       // Cada stat varía ± este valor

    // ─── Habilidades de la Clase ──────────────────────────────────────────────
    [Header("Habilidades")]
    [Tooltip("Lista de SkillData que un goblin de esta clase tendrá.")]
    public List<SkillData> classSkills = new List<SkillData>();
}

/// <summary>
/// Define el patrón de comportamiento de la IA.
/// Puedes añadir nuevos sin romper nada existente.
/// </summary>
public enum AIBehavior
{
    Aggressive,     // Ataca al más débil. Si HP bajo, se defiende. (Guerrero)
    Supportive,     // Cura aliados heridos. Si no, usa magia ofensiva. (Chamán)
    Tactical,       // Envenena primero, luego ataca. (Pícaro)
    Balanced,       // Mezcla ataque y defensa equilibradamente. (Genérico/Fallback)
    Berserker,      // Siempre ataca al más fuerte, nunca se defiende. (Futuro)
    Guardian,       // Se defiende mucho y protege aliados. (Futuro)
}

/// <summary>
/// Habilidades pasivas únicas de cada clase.
/// Se procesan en Unit y BattleSystem en los momentos clave del combate.
/// </summary>
public enum PassiveAbility
{
    None,               // Sin pasiva (clase genérica)

    // ── Pícaro ─────────────────────────────────────────────────────────────
    VenomousStrike,     // Al pasar el turno (Pass), extiende el veneno de todos los objetivos
                        // envenenados por +passiveValue turnos.

    // ── Guerrero ───────────────────────────────────────────────────────────
    Ironhide,           // Al recibir un golpe físico, gana +passiveValue de defensa temporal
                        // hasta su próximo turno (se resetea al inicio del turno).

    // ── Chamán ─────────────────────────────────────────────────────────────
    TideCaller,         // Al curar a un aliado, también recupera passiveValue de MP propio
                        // (retroalimentación mágica).

    // ── Berserker ──────────────────────────────────────────────────────────
    Bloodlust,          // Al noquear a un enemigo, recupera passiveValue de HP.

    // ── Guardián ───────────────────────────────────────────────────────────
    Bulwark,            // Cuando se defiende, además reduce el daño de todos los aliados
                        // adyacentes en passiveValue puntos hasta el siguiente turno.
}
