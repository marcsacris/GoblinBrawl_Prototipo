using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Gestiona la cola de turnos ordenada por Velocidad.
/// Al inicio de cada ronda, ordena todas las unidades vivas de mayor a menor velocidad.
/// Se integra con BattleSystem para saber quién actúa a continuación.
/// </summary>
public class TurnManager
{
    // ─── Cola de turnos ───────────────────────────────────────────────────────
    private List<Unit> turnQueue = new List<Unit>();
    private int currentIndex = 0;
    private int roundNumber = 0;

    // ─── Propiedades ──────────────────────────────────────────────────────────
    public Unit CurrentUnit => turnQueue.Count > 0 ? turnQueue[currentIndex] : null;
    public int RoundNumber => roundNumber;

    // ─── API Pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa la cola con todas las unidades (jugadores + enemigos).
    /// Llama esto al inicio de la batalla con la lista combinada.
    /// </summary>
    public void Initialize(List<Unit> allUnits)
    {
        BuildQueue(allUnits);
        currentIndex = 0;
        roundNumber = 1;
    }

    /// <summary>
    /// Avanza al siguiente turno. Salta unidades muertas.
    /// Si la cola se agota, empieza una nueva ronda reordenando por velocidad.
    /// </summary>
    /// <param name="allUnits">La lista completa de unidades (para reconstruir la cola en nueva ronda).</param>
    public void Advance(List<Unit> allUnits)
    {
        currentIndex++;

        // Eliminamos unidades muertas de la cola si las hay
        turnQueue.RemoveAll(u => u == null || u.IsDead);

        // Si llegamos al final de la cola, empezamos nueva ronda
        if (currentIndex >= turnQueue.Count)
        {
            roundNumber++;
            BuildQueue(allUnits);
            currentIndex = 0;
        }

        // Nos aseguramos de que la unidad actual esté viva
        // (puede haber muerto justo en el turno anterior)
        while (turnQueue.Count > 0 && turnQueue[currentIndex].IsDead)
        {
            turnQueue.RemoveAt(currentIndex);
            if (currentIndex >= turnQueue.Count) currentIndex = 0;
        }
    }

    /// <summary>
    /// Devuelve la lista de unidades en el orden actual de la cola (para mostrar en UI si se quiere).
    /// </summary>
    public List<Unit> GetQueueSnapshot()
    {
        return new List<Unit>(turnQueue);
    }

    // ─── Privado ──────────────────────────────────────────────────────────────

    private void BuildQueue(List<Unit> allUnits)
    {
        turnQueue = allUnits
            .Where(u => u != null && !u.IsDead)
            .OrderByDescending(u => u.speed)
            .ThenBy(u => Random.value) // Desempate aleatorio cuando velocidades son iguales
            .ToList();
    }
}
