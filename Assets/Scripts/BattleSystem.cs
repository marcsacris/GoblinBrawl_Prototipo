using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SCRIPT PRINCIPAL del combate. Orquesta el flujo de turnos, las acciones
/// del jugador y la IA del enemigo. Debe estar en un GameObject vacío "BattleSystem".
/// </summary>
public class BattleSystem : MonoBehaviour
{
    // ─── Estados posibles del combate ────────────────────────────────────────
    public enum BattleState
    {
        START,          // Inicializando la batalla
        PLAYER_TURN,    // Esperando la acción del jugador
        ENEMY_TURN,     // El enemigo está tomando su turno
        WON,            // El jugador ha ganado
        LOST            // El jugador ha perdido
    }

    public BattleState state;   // Estado actual del combate

    // ─── Referencias a los Goblins (arrastrar desde el Inspector) ────────────
    [Header("Unidades de Combate")]
    public Unit playerUnit;     // El componente Unit del Goblin del Jugador
    public Unit enemyUnit;      // El componente Unit del Goblin Enemigo

    // ─── Referencias a las HUDs (arrastrar desde el Inspector) ───────────────
    [Header("HUDs de la Interfaz")]
    public BattleHUD playerHUD; // El componente BattleHUD del panel del jugador
    public BattleHUD enemyHUD;  // El componente BattleHUD del panel del enemigo

    // ─── Referencias a los elementos de UI (arrastrar desde el Inspector) ────
    [Header("Elementos de UI")]
    public TMP_Text dialogueText;    // Texto central que narra lo que sucede
    public Button attackButton;      // Botón "Atacar"
    public Button skillButton;       // Botón "Habilidad"
    public TMP_Text resultText;      // Texto grande para "¡Ganaste!" / "¡Perdiste!"

    // ─── Constantes de Diseño ────────────────────────────────────────────────
    [Header("Configuración")]
    public float enemyTurnDelay = 1.5f;  // Segundos que espera el enemigo antes de atacar

    // ════════════════════════════════════════════════════════════════════════
    // CICLO DE VIDA DE UNITY
    // ════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        state = BattleState.START;
        StartCoroutine(SetupBattle()); // Iniciamos la batalla con una Coroutine
    }

    // ════════════════════════════════════════════════════════════════════════
    // SETUP INICIAL
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Coroutine que inicializa la batalla: configura HUDs y pasa al turno del jugador.
    /// </summary>
    IEnumerator SetupBattle()
    {
        // Inicialmente ocultamos el texto de resultado
        resultText.gameObject.SetActive(false);

        // Deshabilitamos los botones mientras se inicializa
        SetPlayerButtonsInteractable(false);

        // Configuramos las HUDs con los datos de cada unidad
        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);

        // Mostramos el mensaje de inicio
        dialogueText.text = $"¡{enemyUnit.unitName} Salvaje ha aparecido!";

        // Esperamos un poco para que el jugador pueda leer el texto
        yield return new WaitForSeconds(2f);

        // Pasamos al turno del jugador
        PlayerTurn();
    }

    // ════════════════════════════════════════════════════════════════════════
    // TURNO DEL JUGADOR
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Activa el turno del jugador: muestra el mensaje y habilita los botones.
    /// </summary>
    void PlayerTurn()
    {
        state = BattleState.PLAYER_TURN;
        dialogueText.text = "¿Qué harás?";
        SetPlayerButtonsInteractable(true); // El jugador puede pulsar botones
    }

    /// <summary>
    /// Llamado por el botón "Atacar" (desde el Inspector del botón -> OnClick).
    /// </summary>
    public void OnAttackButton()
    {
        // Solo actuamos si es el turno del jugador
        if (state != BattleState.PLAYER_TURN) return;

        SetPlayerButtonsInteractable(false); // Deshabilitamos botones para evitar doble clic
        StartCoroutine(PlayerAttack());
    }

    /// <summary>
    /// Llamado por el botón "Habilidad" (desde el Inspector del botón -> OnClick).
    /// </summary>
    public void OnSkillButton()
    {
        if (state != BattleState.PLAYER_TURN) return;

        SetPlayerButtonsInteractable(false);
        StartCoroutine(PlayerSkill());
    }

    /// <summary>
    /// Coroutine del ataque físico del jugador.
    /// </summary>
    IEnumerator PlayerAttack()
    {
        // Calculamos el daño basado en la fuerza del jugador
        int damage = playerUnit.strength;
        bool isDead = enemyUnit.TakeDamage(damage);

        // Actualizamos la HUD del enemigo
        enemyHUD.UpdateHP(enemyUnit);

        dialogueText.text = $"¡{playerUnit.unitName} ataca!\n¡{enemyUnit.unitName} recibe {damage} de daño!";

        yield return new WaitForSeconds(1.5f);

        // Comprobamos si el enemigo ha muerto
        if (isDead)
        {
            state = BattleState.WON;
            StartCoroutine(EndBattle());
        }
        else
        {
            // Pasamos al turno del enemigo
            StartCoroutine(EnemyTurn());
        }
    }

    /// <summary>
    /// Coroutine de la habilidad mágica del jugador.
    /// Si tiene MP: inflige daño mágico al enemigo y cura ligeramente al jugador.
    /// Si no tiene MP: muestra un mensaje de error.
    /// </summary>
    IEnumerator PlayerSkill()
    {
        // Intentamos gastar el MP necesario
        bool hadEnoughMP = playerUnit.SpendMP(playerUnit.magicCost);

        if (!hadEnoughMP)
        {
            // Sin MP: volvemos al turno del jugador sin cambiar el estado
            dialogueText.text = "¡No tienes suficiente Maná!";
            yield return new WaitForSeconds(1.5f);
            PlayerTurn(); // Volvemos a habilitar los botones
            yield break;  // Salimos de la Coroutine
        }

        // Actualizamos el MP del jugador en la HUD
        playerHUD.UpdateMP(playerUnit);

        // Calculamos y aplicamos daño mágico al enemigo
        int magicDamage = playerUnit.magicPower;
        bool isDead = enemyUnit.TakeDamage(magicDamage);
        enemyHUD.UpdateHP(enemyUnit);

        // Curación parcial para el jugador (el 50% del poder mágico)
        int healAmount = playerUnit.magicPower / 2;
        playerUnit.Heal(healAmount);
        playerHUD.UpdateHP(playerUnit);

        dialogueText.text = $"¡{playerUnit.unitName} usa Magia!\n" +
                            $"¡{enemyUnit.unitName} recibe {magicDamage} de daño mágico!\n" +
                            $"¡{playerUnit.unitName} se cura {healAmount} HP!";

        yield return new WaitForSeconds(2f);

        if (isDead)
        {
            state = BattleState.WON;
            StartCoroutine(EndBattle());
        }
        else
        {
            StartCoroutine(EnemyTurn());
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // TURNO DEL ENEMIGO (IA Básica)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Coroutine del turno del enemigo. Espera un momento y luego ataca automáticamente.
    /// </summary>
    IEnumerator EnemyTurn()
    {
        state = BattleState.ENEMY_TURN;

        dialogueText.text = $"Turno de {enemyUnit.unitName}...";

        // Pausa dramática antes del ataque del enemigo
        yield return new WaitForSeconds(enemyTurnDelay);

        // IA muy básica: el enemigo siempre ataca
        int damage = enemyUnit.strength;
        bool isDead = playerUnit.TakeDamage(damage);

        // Actualizamos la HUD del jugador
        playerHUD.UpdateHP(playerUnit);

        dialogueText.text = $"¡{enemyUnit.unitName} ataca!\n¡{playerUnit.unitName} recibe {damage} de daño!";

        yield return new WaitForSeconds(1.5f);

        // Comprobamos si el jugador ha muerto
        if (isDead)
        {
            state = BattleState.LOST;
            StartCoroutine(EndBattle());
        }
        else
        {
            // Devolvemos el turno al jugador
            PlayerTurn();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // FIN DE BATALLA
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gestiona el final del combate: muestra el texto de victoria o derrota.
    /// </summary>
    IEnumerator EndBattle()
    {
        // Aseguramos que los botones estén desactivados al final
        SetPlayerButtonsInteractable(false);

        if (state == BattleState.WON)
        {
            dialogueText.text = $"¡{enemyUnit.unitName} ha sido derrotado!";
            yield return new WaitForSeconds(1f);
            resultText.text = "🏆 ¡VICTORIA!";
            resultText.color = new Color(0.2f, 1f, 0.4f); // Verde
        }
        else if (state == BattleState.LOST)
        {
            dialogueText.text = $"¡{playerUnit.unitName} ha caído en combate!";
            yield return new WaitForSeconds(1f);
            resultText.text = "💀 ¡DERROTA!";
            resultText.color = new Color(1f, 0.3f, 0.3f); // Rojo
        }

        // Mostramos el panel de resultado
        resultText.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════════
    // UTILIDADES PRIVADAS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Activa o desactiva la interactividad de los botones del jugador.
    /// </summary>
    private void SetPlayerButtonsInteractable(bool value)
    {
        attackButton.interactable = value;
        skillButton.interactable = value;
    }
}
