using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona los 3 paneles de HUD de un equipo (jugador o enemigo).
/// Resalta el goblin activo y actualiza todos los HUDs del equipo.
/// Asigna este script a un GameObject "PlayerTeamHUD" o "EnemyTeamHUD"
/// y rellena la lista huds con los 3 BattleHUD del equipo.
/// </summary>
public class TeamHUD : MonoBehaviour
{
    [Header("HUDs del Equipo (uno por goblin, en orden)")]
    public List<BattleHUD> huds = new List<BattleHUD>();

    /// <summary>
    /// Inicializa todos los HUDs del equipo con los datos de las unidades.
    /// </summary>
    public void SetupTeam(List<Unit> team)
    {
        for (int i = 0; i < huds.Count && i < team.Count; i++)
        {
            huds[i].SetHUD(team[i]);
        }
    }

    /// <summary>
    /// Actualiza el HP de la unidad en la posición dada.
    /// </summary>
    public void UpdateHP(int index, Unit unit)
    {
        if (index >= 0 && index < huds.Count)
            huds[index].UpdateHP(unit);
    }

    /// <summary>
    /// Actualiza el MP de la unidad en la posición dada.
    /// </summary>
    public void UpdateMP(int index, Unit unit)
    {
        if (index >= 0 && index < huds.Count)
            huds[index].UpdateMP(unit);
    }

    /// <summary>
    /// Actualiza HP, MP e indicadores de estado para una unidad.
    /// </summary>
    public void RefreshUnit(int index, Unit unit)
    {
        if (index < 0 || index >= huds.Count) return;
        huds[index].UpdateHP(unit);
        huds[index].UpdateMP(unit);
        huds[index].UpdateStatusIndicators(unit);
    }

    /// <summary>
    /// Marca el goblin en la posición indicada como el que tiene el turno activo.
    /// Quita el resalte de los demás.
    /// </summary>
    public void SetActiveUnit(int activeIndex)
    {
        for (int i = 0; i < huds.Count; i++)
            huds[i].SetActive(i == activeIndex);
    }

    /// <summary>
    /// Quita el resalte de turno activo de todos los goblins del equipo.
    /// </summary>
    public void ClearActiveUnit()
    {
        foreach (var hud in huds)
            hud.SetActive(false);
    }

    /// <summary>
    /// Marca un goblin como objetivo seleccionado (resalte dorado).
    /// </summary>
    public void SetSelectedUnit(int selectedIndex, bool isSelected)
    {
        if (selectedIndex >= 0 && selectedIndex < huds.Count)
            huds[selectedIndex].SetSelected(isSelected);
    }

    /// <summary>
    /// Quita el resalte de objetivo de todos los goblins del equipo.
    /// </summary>
    public void ClearSelection()
    {
        foreach (var hud in huds)
            hud.SetSelected(false);
    }

    /// <summary>
    /// Obtiene el índice de una unidad dentro de este equipo, o -1 si no está.
    /// </summary>
    public int GetIndexOf(Unit unit, List<Unit> team)
    {
        return team.IndexOf(unit);
    }
}
