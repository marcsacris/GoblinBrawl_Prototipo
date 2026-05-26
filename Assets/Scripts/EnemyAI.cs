using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// IA del equipo enemigo. Decide qué acción toma cada goblin enemigo
/// basándose en su clase y el estado del combate.
/// Se llama desde BattleSystem durante el turno del enemigo.
/// </summary>
public static class EnemyAI
{
    // ─── Resultado de la decisión de IA ──────────────────────────────────────

    public enum AIActionType { Attack, Skill, Defend, Pass }

    public class AIDecision
    {
        public AIActionType actionType;
        public SkillData chosenSkill;   // null si actionType != Skill
        public Unit target;             // null si la habilidad afecta a todos
    }

    // ─── Punto de entrada principal ───────────────────────────────────────────

    /// <summary>
    /// Genera la decisión de IA para un goblin enemigo dado el estado del combate.
    /// </summary>
    /// <param name="actor">El goblin enemigo que va a actuar.</param>
    /// <param name="enemyTeam">Lista del equipo enemigo (el propio equipo del actor).</param>
    /// <param name="playerTeam">Lista del equipo del jugador.</param>
    public static AIDecision Decide(Unit actor, List<Unit> enemyTeam, List<Unit> playerTeam)
    {
        List<Unit> aliveEnemies = enemyTeam.Where(u => !u.IsDead).ToList();
        List<Unit> alivePlayers = playerTeam.Where(u => !u.IsDead).ToList();

        if (alivePlayers.Count == 0) return new AIDecision { actionType = AIActionType.Pass };

        // Obtenemos el comportamiento de IA desde el GoblinClassData del actor
        AIBehavior behavior = actor.classData != null 
            ? actor.classData.aiBehavior 
            : AIBehavior.Balanced;

        return behavior switch
        {
            AIBehavior.Aggressive => DecideWarrior(actor, alivePlayers),
            AIBehavior.Supportive => DecideShaman(actor, aliveEnemies, alivePlayers),
            AIBehavior.Tactical   => DecideRogue(actor, alivePlayers),
            AIBehavior.Berserker  => DecideBerserker(actor, alivePlayers),
            AIBehavior.Guardian   => DecideGuardian(actor, aliveEnemies, alivePlayers),
            _                     => DecideDefault(actor, alivePlayers),
        };
    }

    // ─── Comportamientos por Clase ────────────────────────────────────────────

    /// <summary>
    /// Guerrero: Ataca siempre. Si HP baja del 30%, se defiende para sobrevivir.
    /// Prioriza al enemigo con menos HP (rematador).
    /// </summary>
    private static AIDecision DecideWarrior(Unit actor, List<Unit> targets)
    {
        float hpRatio = (float)actor.currentHP / actor.maxHP;

        // Si tiene poca vida y aún no está defendiendo, se defiende
        if (hpRatio < 0.30f && !actor.IsDefending)
        {
            return new AIDecision { actionType = AIActionType.Defend };
        }

        // Ataca al objetivo con menos HP (para rematar)
        Unit target = targets.OrderBy(u => u.currentHP).First();
        return new AIDecision { actionType = AIActionType.Attack, target = target };
    }

    /// <summary>
    /// Chamán: Si hay un aliado con HP < 50%, lo cura usando una skill de Heal.
    /// Si no, lanza la skill mágica de daño contra el objetivo con más HP (debilitar al fuerte).
    /// Si no tiene MP, ataca o pasa.
    /// </summary>
    private static AIDecision DecideShaman(Unit actor, List<Unit> allies, List<Unit> targets)
    {
        // Buscamos una skill de curación
        SkillData healSkill = actor.skills.FirstOrDefault(
            s => s.skillType == SkillType.Heal && actor.currentMP >= s.mpCost
        );

        // Comprobamos si algún aliado necesita curación (HP < 50%)
        Unit injuredAlly = allies
            .Where(u => !u.IsDead && (float)u.currentHP / u.maxHP < 0.5f)
            .OrderBy(u => u.currentHP)
            .FirstOrDefault();

        if (healSkill != null && injuredAlly != null)
        {
            return new AIDecision
            {
                actionType = AIActionType.Skill,
                chosenSkill = healSkill,
                target = injuredAlly  // Siempre cura al aliado más herido
            };
        }

        // Si no necesita curación, busca una skill de daño mágico
        SkillData damageSkill = actor.skills
            .Where(s => (s.skillType == SkillType.Magical) && actor.currentMP >= s.mpCost)
            .OrderByDescending(s => s.powerMultiplier)
            .FirstOrDefault();

        if (damageSkill != null)
        {
            // Ataca al objetivo con más HP
            Unit target = targets.OrderByDescending(u => u.currentHP).First();
            return new AIDecision
            {
                actionType = AIActionType.Skill,
                chosenSkill = damageSkill,
                target = damageSkill.targetType == TargetType.AllEnemies ? null : target
            };
        }

        // Sin MP: ataca físicamente
        return new AIDecision
        {
            actionType = AIActionType.Attack,
            target = targets.First()
        };
    }

    /// <summary>
    /// Pícaro: Si el objetivo más lento no está envenenado, usa Daga Envenenada.
    /// Si ya está envenenado, ataca al objetivo más débil para aprovechar el veneno.
    /// Muy rápido, actúa antes que los demás.
    /// </summary>
    private static AIDecision DecideRogue(Unit actor, List<Unit> targets)
    {
        SkillData poisonSkill = actor.skills.FirstOrDefault(
            s => (s.skillType == SkillType.Poison || s.skillType == SkillType.PhysicalPoison)
              && actor.currentMP >= s.mpCost
        );

        // Objetivo sin veneno para envenenar
        Unit unpoisonedTarget = targets.FirstOrDefault(u => !u.IsPoisoned);

        if (poisonSkill != null && unpoisonedTarget != null)
        {
            return new AIDecision
            {
                actionType = AIActionType.Skill,
                chosenSkill = poisonSkill,
                target = unpoisonedTarget
            };
        }

        // Si todos están envenenados o no tiene la skill, ataca al de menos HP
        Unit weakestTarget = targets.OrderBy(u => u.currentHP).First();
        return new AIDecision { actionType = AIActionType.Attack, target = weakestTarget };
    }

    /// <summary>
    /// Fallback genérico: ataca al objetivo con menos HP.
    /// </summary>
    private static AIDecision DecideDefault(Unit actor, List<Unit> targets)
    {
        return new AIDecision
        {
            actionType = AIActionType.Attack,
            target = targets.OrderBy(u => u.currentHP).First()
        };
    }

    /// <summary>
    /// Berserker: Siempre ataca al objetivo más fuerte (más HP). Nunca se defiende.
    /// Si tiene una skill de daño físico con MP, la usa. Si no, ataca normal.
    /// </summary>
    private static AIDecision DecideBerserker(Unit actor, List<Unit> targets)
    {
        Unit strongestTarget = targets.OrderByDescending(u => u.currentHP).First();

        // Intenta usar una habilidad de daño físico potente
        SkillData powerSkill = actor.skills
            .Where(s => (s.skillType == SkillType.Physical) && actor.currentMP >= s.mpCost)
            .OrderByDescending(s => s.powerMultiplier)
            .FirstOrDefault();

        if (powerSkill != null)
        {
            return new AIDecision
            {
                actionType = AIActionType.Skill,
                chosenSkill = powerSkill,
                target = strongestTarget
            };
        }

        return new AIDecision { actionType = AIActionType.Attack, target = strongestTarget };
    }

    /// <summary>
    /// Guardian: Si algún aliado tiene HP < 40%, se defiende (protege al equipo narrativamente).
    /// Si tiene habilidad de curación con MP, cura al aliado más herido.
    /// Si no, ataca al más débil.
    /// </summary>
    private static AIDecision DecideGuardian(Unit actor, List<Unit> allies, List<Unit> targets)
    {
        Unit injuredAlly = allies
            .Where(u => !u.IsDead && (float)u.currentHP / u.maxHP < 0.4f)
            .OrderBy(u => u.currentHP)
            .FirstOrDefault();

        // Intenta curar si tiene habilidad
        SkillData healSkill = actor.skills.FirstOrDefault(
            s => s.skillType == SkillType.Heal && actor.currentMP >= s.mpCost
        );

        if (healSkill != null && injuredAlly != null)
        {
            return new AIDecision
            {
                actionType = AIActionType.Skill,
                chosenSkill = healSkill,
                target = injuredAlly
            };
        }

        // Si hay aliados heridos pero no puede curar, se defiende
        if (injuredAlly != null && !actor.IsDefending)
        {
            return new AIDecision { actionType = AIActionType.Defend };
        }

        // Si nadie necesita protección, ataca
        Unit weakest = targets.OrderBy(u => u.currentHP).First();
        return new AIDecision { actionType = AIActionType.Attack, target = weakest };
    }
}
