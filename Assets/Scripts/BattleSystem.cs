using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SCRIPT PRINCIPAL de Goblin Brawl.
/// Controla el flujo de combate por turnos 3v3 ordenados por Velocidad,
/// maneja la selección de acciones y objetivos del jugador, y delega en EnemyAI.
/// 
/// SISTEMA DE DIÁLOGO: Los mensajes avanzan cuando el jugador hace clic en la pantalla.
/// Un indicador "▼" parpadeante aparece para indicar que se puede avanzar.
/// </summary>
public class BattleSystem : MonoBehaviour
{
    public enum BattleState
    {
        START,
        TURN_CHECK,         // Comprobando estados y venenos al inicio del turno
        WAITING_FOR_INPUT,  // Esperando que el jugador elija Atacar, Habilidad, Defender o Pasar
        SELECTING_SKILL,    // Mostrando el menú de habilidades
        SELECTING_TARGET,   // Esperando que el jugador elija un objetivo
        EXECUTING_ACTION,   // Animando/resolviendo la acción
        CHECK_WIN_LOSS,     // Verificando si el combate terminó
        ENEMY_TURN,         // Turno de la IA enemiga
        WON,
        LOST
    }

    [Header("Estado del Combate")]
    public BattleState state;

    [Header("Equipos (3v3)")]
    [Tooltip("Arrastra aquí los 3 Goblins del jugador en orden.")]
    public List<Unit> playerTeam = new List<Unit>();
    [Tooltip("Arrastra aquí los 3 Goblins enemigos en orden.")]
    public List<Unit> enemyTeam = new List<Unit>();

    [Header("HUDs de los Equipos")]
    public TeamHUD playerTeamHUD;
    public TeamHUD enemyTeamHUD;

    [Header("Elementos de UI Principal")]
    public TMP_Text dialogueText;       // Texto central que narra el combate
    public TMP_Text resultText;         // Texto grande de Victoria/Derrota
    public GameObject actionPanel;      // Panel que contiene los 4 botones principales
    public Button attackButton;
    public Button skillButton;
    public Button defendButton;
    public Button passButton;

    [Header("Menú de Habilidades")]
    public GameObject skillPanel;       // Panel contenedor de las habilidades
    public List<Button> skillButtons;   // Botones de habilidad pre-creados (ej. 3 botones)
    public List<TMP_Text> skillButtonTexts;

    [Header("Indicador de Click para Continuar")]
    [Tooltip("Texto que muestra '▼' parpadeante cuando el jugador debe hacer clic para avanzar.")]
    public TMP_Text clickIndicatorText; // Arrastra aquí un TMP_Text que muestre "▼" debajo del diálogo

    [Header("Configuración")]
    public float sliderAnimDelay = 0.5f;    // Tiempo para que las barras se animen antes de permitir clic
    public float enemyPreDelay = 0.5f;      // Breve pausa antes de que la IA decida

    [Header("Base de Datos de Clases")]
    [Tooltip("Arrastra aquí todos los GoblinClassData disponibles. Cada partida asignará clases aleatorias de esta lista.")]
    public List<GoblinClassData> classDatabase = new List<GoblinClassData>();

    // ─── Variables de Estado Interno ─────────────────────────────────────────
    private TurnManager turnManager;
    private List<Unit> allUnits = new List<Unit>();
    private Unit activeUnit;            // Goblin cuyo turno está activo
    
    // Contexto de la acción del jugador
    private SkillData selectedSkill;    // Habilidad elegida (si es el caso)
    private bool isSelectingTargetForSkill = false;

    // ─── Sistema de Click para Continuar ─────────────────────────────────────
    private bool waitingForClick = false;    // ¿Estamos esperando un clic del jugador?
    private bool clickReceived = false;      // ¿Se ha recibido el clic?
    private Coroutine blinkCoroutine;        // Referencia a la coroutine del parpadeo

    // ════════════════════════════════════════════════════════════════════════
    // CICLO DE VIDA DE UNITY
    // ════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        state = BattleState.START;
        turnManager = new TurnManager();

        // Ocultamos el indicador de clic al inicio
        if (clickIndicatorText != null)
            clickIndicatorText.gameObject.SetActive(false);

        StartCoroutine(SetupBattle());
    }

    private void Update()
    {
        // Detectar clic/toque en cualquier parte de la pantalla
        if (waitingForClick)
        {
            // Clic izquierdo del ratón O toque en pantalla táctil
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                clickReceived = true;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SISTEMA DE CLICK PARA CONTINUAR
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Coroutine que espera hasta que el jugador haga clic en la pantalla.
    /// Muestra un indicador "▼" parpadeante mientras espera.
    /// </summary>
    private IEnumerator WaitForClick()
    {
        // Pequeña pausa mínima para que el jugador pueda leer el texto
        // y para que las animaciones de sliders terminen
        yield return new WaitForSeconds(sliderAnimDelay);

        // Activar el indicador parpadeante
        clickReceived = false;
        waitingForClick = true;
        ShowClickIndicator(true);

        // Esperar hasta que se reciba el clic
        while (!clickReceived)
        {
            yield return null;
        }

        // Limpiar estado
        waitingForClick = false;
        clickReceived = false;
        ShowClickIndicator(false);
    }

    /// <summary>
    /// Muestra u oculta el indicador "▼" parpadeante.
    /// </summary>
    private void ShowClickIndicator(bool show)
    {
        if (clickIndicatorText == null) return;

        if (show)
        {
            clickIndicatorText.gameObject.SetActive(true);
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkIndicator());
        }
        else
        {
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
            clickIndicatorText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Anima el indicador "▼" con un efecto de parpadeo suave (fade in/out).
    /// </summary>
    private IEnumerator BlinkIndicator()
    {
        clickIndicatorText.text = "▼ Clic para continuar ▼";
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime * 2.5f; // Velocidad del parpadeo
            float alpha = Mathf.PingPong(t, 1f);
            Color c = clickIndicatorText.color;
            c.a = Mathf.Lerp(0.2f, 1f, alpha); // Nunca desaparece del todo
            clickIndicatorText.color = c;
            yield return null;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SETUP INICIAL
    // ════════════════════════════════════════════════════════════════════════

    IEnumerator SetupBattle()
    {
        resultText.gameObject.SetActive(false);
        skillPanel.SetActive(false);
        SetActionButtonsInteractable(false);

        // 1. Randomizar las clases de todos los goblins
        RandomizeClasses();

        // Reunimos a todas las unidades vivas
        allUnits.Clear();
        allUnits.AddRange(playerTeam);
        allUnits.AddRange(enemyTeam);

        // Inicializamos los HUDs (después de randomizar para que muestren las stats correctas)
        playerTeamHUD.SetupTeam(playerTeam);
        enemyTeamHUD.SetupTeam(enemyTeam);

        // Configuramos los botones del HUD para que al hacerles clic sirvan para seleccionar objetivo
        BindHUDButtons();

        // Inicializamos la cola de turnos (después de randomizar para que use la Velocidad correcta)
        turnManager.Initialize(allUnits);

        dialogueText.text = "¡Da comienzo la guerra por el trono del Pantano!";
        yield return StartCoroutine(WaitForClick());

        StartCoroutine(StartNextTurn());
    }

    /// <summary>
    /// Asigna una clase aleatoria de classDatabase a cada goblin de ambos equipos.
    /// Cada goblin recibe stats randomizadas según su clase (con variación ±).
    /// </summary>
    private void RandomizeClasses()
    {
        if (classDatabase == null || classDatabase.Count == 0)
        {
            Debug.LogWarning("BattleSystem: classDatabase está vacía. Los goblins no tendrán clase.");
            return;
        }

        // Asignar clase aleatoria a cada goblin del jugador
        foreach (Unit unit in playerTeam)
        {
            GoblinClassData randomClass = classDatabase[Random.Range(0, classDatabase.Count)];
            unit.ApplyClassData(randomClass);
        }

        // Asignar clase aleatoria a cada goblin enemigo
        foreach (Unit unit in enemyTeam)
        {
            GoblinClassData randomClass = classDatabase[Random.Range(0, classDatabase.Count)];
            unit.ApplyClassData(randomClass);
        }
    }

    /// <summary>
    /// Asigna de forma dinámica eventos de clic a los paneles del HUD para la selección de objetivos.
    /// Esto evita tener que arrastrarlos uno a uno en Unity.
    /// </summary>
    private void BindHUDButtons()
    {
        // Enlazar Goblins del jugador
        for (int i = 0; i < playerTeam.Count; i++)
        {
            int index = i; // Variable local para capturar en la lambda
            Button btn = playerTeamHUD.huds[index].GetComponent<Button>();
            if (btn == null) btn = playerTeamHUD.huds[index].gameObject.AddComponent<Button>();
            
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnTargetClicked(playerTeam[index]));
        }

        // Enlazar Goblins enemigos
        for (int i = 0; i < enemyTeam.Count; i++)
        {
            int index = i;
            Button btn = enemyTeamHUD.huds[index].GetComponent<Button>();
            if (btn == null) btn = enemyTeamHUD.huds[index].gameObject.AddComponent<Button>();

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnTargetClicked(enemyTeam[index]));
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // FLUJO DE TURNOS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Determina quién actúa en este turno, procesa veneno y arranca su lógica.
    /// </summary>
    IEnumerator StartNextTurn()
    {
        state = BattleState.TURN_CHECK;

        // Limpiar indicadores y estados previos
        playerTeamHUD.ClearActiveUnit();
        enemyTeamHUD.ClearActiveUnit();
        playerTeamHUD.ClearSelection();
        enemyTeamHUD.ClearSelection();
        skillPanel.SetActive(false);
        SetActionButtonsInteractable(false);

        // Verificar si la batalla ya terminó
        if (CheckBattleOver()) yield break;

        // Obtener el goblin activo
        activeUnit = turnManager.CurrentUnit;

        if (activeUnit == null || activeUnit.IsDead)
        {
            AdvanceTurn();
            yield break;
        }

        // Resaltar al goblin activo en la interfaz
        HighlightActiveUnitHUD(activeUnit);

        // 1. Quitar el estado de defensa al iniciar su propio turno
        if (activeUnit.IsDefending)
        {
            activeUnit.SetDefending(false);
            RefreshUnitHUD(activeUnit);
        }

        // 2. Procesar daño por veneno al inicio del turno
        if (activeUnit.IsPoisoned)
        {
            int poisonDmg = activeUnit.ProcessPoisonTick();
            RefreshUnitHUD(activeUnit);
            dialogueText.text = $"¡El veneno del pantano quema a {activeUnit.unitName} causándole {poisonDmg} HP de daño!";
            yield return StartCoroutine(WaitForClick());

            if (activeUnit.IsDead)
            {
                dialogueText.text = $"¡{activeUnit.unitName} ha sucumbido al veneno!";
                yield return StartCoroutine(WaitForClick());
                AdvanceTurn();
                yield break;
            }
        }

        // 3. Determinar si es turno de Jugador o Enemigo
        if (playerTeam.Contains(activeUnit))
        {
            dialogueText.text = $"Es el turno de {activeUnit.unitName}. ¿Qué hará?";
            state = BattleState.WAITING_FOR_INPUT;
            SetActionButtonsInteractable(true);
        }
        else
        {
            state = BattleState.ENEMY_TURN;
            dialogueText.text = $"Turno de {activeUnit.unitName} del clan rival...";
            yield return StartCoroutine(WaitForClick());
            ExecuteEnemyAI();
        }
    }

    private void AdvanceTurn()
    {
        turnManager.Advance(allUnits);
        StartCoroutine(StartNextTurn());
    }

    // ════════════════════════════════════════════════════════════════════════
    // BOTONES DE ACCIÓN DEL JUGADOR
    // ════════════════════════════════════════════════════════════════════════

    public void OnAttackButton()
    {
        if (state != BattleState.WAITING_FOR_INPUT) return;

        skillPanel.SetActive(false);
        selectedSkill = null;
        isSelectingTargetForSkill = false;

        // Entrar en modo selección de objetivo (enemigo)
        state = BattleState.SELECTING_TARGET;
        dialogueText.text = "Selecciona un objetivo enemigo en su panel.";
        
        // Resaltar enemigos como posibles objetivos interactivos
        HighlightTargets(enemyTeam);
    }

    public void OnSkillMenuButton()
    {
        if (state != BattleState.WAITING_FOR_INPUT) return;

        // Mostrar / Ocultar el panel de habilidades
        bool isShowing = !skillPanel.activeSelf;
        skillPanel.SetActive(isShowing);

        if (isShowing)
        {
            PopulateSkillMenu();
        }
        else
        {
            dialogueText.text = "¿Qué hará?";
        }
    }

    public void OnDefendButton()
    {
        if (state != BattleState.WAITING_FOR_INPUT) return;

        StartCoroutine(PlayerDefend());
    }

    public void OnPassButton()
    {
        if (state != BattleState.WAITING_FOR_INPUT) return;

        StartCoroutine(PlayerPass());
    }

    // ════════════════════════════════════════════════════════════════════════
    // MENÚ Y RESOLUCIÓN DE HABILIDADES
    // ════════════════════════════════════════════════════════════════════════

    private void PopulateSkillMenu()
    {
        // Ocultar todos los botones primero
        for (int i = 0; i < skillButtons.Count; i++)
        {
            skillButtons[i].gameObject.SetActive(false);
        }

        // Configurar botones para las habilidades disponibles del unit activo
        for (int i = 0; i < activeUnit.skills.Count && i < skillButtons.Count; i++)
        {
            SkillData skill = activeUnit.skills[i];
            Button btn = skillButtons[i];
            TMP_Text txt = skillButtonTexts[i];

            txt.text = $"{skill.skillName} ({skill.mpCost} MP)";
            btn.gameObject.SetActive(true);

            // Deshabilitar botón si no tiene suficiente MP
            btn.interactable = activeUnit.currentMP >= skill.mpCost;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSkillSelected(skill));
        }
    }

    private void OnSkillSelected(SkillData skill)
    {
        if (state != BattleState.WAITING_FOR_INPUT && state != BattleState.SELECTING_SKILL) return;

        selectedSkill = skill;
        skillPanel.SetActive(false); // Ocultar menú tras elegir

        // Dependiendo del tipo de objetivo de la habilidad, procedemos
        switch (skill.targetType)
        {
            case TargetType.Self:
                StartCoroutine(ExecuteSkillAction(activeUnit));
                break;

            case TargetType.AllEnemies:
            case TargetType.AllAllies:
                StartCoroutine(ExecuteSkillAction(null)); // null significa multi-objetivo
                break;

            case TargetType.SingleEnemy:
                state = BattleState.SELECTING_TARGET;
                isSelectingTargetForSkill = true;
                dialogueText.text = $"Selecciona un rival para usar {skill.skillName}.";
                HighlightTargets(enemyTeam);
                break;

            case TargetType.SingleAlly:
                // Si es una habilidad de curación, seleccionar automáticamente al aliado más herido
                if (skill.skillType == SkillType.Heal)
                {
                    // Buscamos al aliado que NO esté muerto y que NO tenga la vida al máximo
                    Unit mostInjured = playerTeam
                        .Where(u => !u.IsDead && u.currentHP < u.maxHP)
                        // Ordenamos por el menor PORCENTAJE de vida restante (ej: 10/100 es peor que 15/20)
                        .OrderBy(u => (float)u.currentHP / u.maxHP)
                        .FirstOrDefault();
                    
                    if (mostInjured != null)
                    {
                        // Si hay alguien herido, lanzamos la curación
                        StartCoroutine(ExecuteSkillAction(mostInjured));
                    }
                    else
                    {
                        // Si todos tienen la vida al máximo, avisamos y no gastamos la habilidad
                        dialogueText.text = "¡Todos los aliados tienen la salud al máximo!";
                        skillPanel.SetActive(true); // Reabrimos el panel para que elija otra cosa
                        state = BattleState.WAITING_FOR_INPUT;
                    }
                }
                else
                {
                    // Otras habilidades de aliados (como buffs) requieren selección manual
                    state = BattleState.SELECTING_TARGET;
                    isSelectingTargetForSkill = true;
                    dialogueText.text = $"Selecciona un aliado para usar {skill.skillName}.";
                    HighlightTargets(playerTeam);
                }
                break;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SELECCIÓN DE OBJETIVOS E INTERACTIVIDAD
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Evento que se ejecuta cuando el jugador hace clic en el panel de un Goblin.
    /// </summary>
    private void OnTargetClicked(Unit clickedUnit)
    {
        if (state != BattleState.SELECTING_TARGET) return;
        if (clickedUnit.IsDead) return;

        // Validar si el objetivo elegido es correcto
        if (selectedSkill != null)
        {
            if (selectedSkill.targetType == TargetType.SingleEnemy && !enemyTeam.Contains(clickedUnit)) return;
            if (selectedSkill.targetType == TargetType.SingleAlly && !playerTeam.Contains(clickedUnit)) return;
            
            // Objetivo válido para la habilidad
            playerTeamHUD.ClearSelection();
            enemyTeamHUD.ClearSelection();
            StartCoroutine(ExecuteSkillAction(clickedUnit));
        }
        else
        {
            // Ataque básico: solo a enemigos
            if (!enemyTeam.Contains(clickedUnit)) return;

            playerTeamHUD.ClearSelection();
            enemyTeamHUD.ClearSelection();
            StartCoroutine(PlayerAttack(clickedUnit));
        }
    }

    private void HighlightTargets(List<Unit> targetTeam)
    {
        TeamHUD hud = (targetTeam == playerTeam) ? playerTeamHUD : enemyTeamHUD;
        hud.ClearSelection();

        for (int i = 0; i < targetTeam.Count; i++)
        {
            if (!targetTeam[i].IsDead)
            {
                hud.SetSelectedUnit(i, true);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // RESOLUCIÓN DE ACCIONES (COROUTINES) — Ahora con WaitForClick()
    // ════════════════════════════════════════════════════════════════════════

    IEnumerator PlayerAttack(Unit target)
    {
        state = BattleState.EXECUTING_ACTION;
        SetActionButtonsInteractable(false);

        int damage = activeUnit.strength;
        int finalDamage = target.TakeDamage(damage, true);
        
        RefreshUnitHUD(target);

        dialogueText.text = $"¡{activeUnit.unitName} asesta un garrotazo a {target.unitName}!\n" +
                            $"¡{target.unitName} recibe {finalDamage} de daño físico!";

        // Ejecutar pasiva Ironhide del Guerrero si recibe daño físico
        if (target.classData != null && target.classData.passiveAbility == PassiveAbility.Ironhide)
        {
            target.AddTempDefense(target.classData.passiveValue);
            dialogueText.text += "\n¡La piel del Guerrero se endurece!";
        }

        yield return StartCoroutine(WaitForClick());

        if (target.IsDead)
        {
            dialogueText.text = $"¡{target.unitName} ha sido noqueado!";
            yield return StartCoroutine(WaitForClick());
        }

        AdvanceTurn();
    }

    IEnumerator ExecuteSkillAction(Unit target)
    {
        state = BattleState.EXECUTING_ACTION;
        SetActionButtonsInteractable(false);

        // Consumir maná
        activeUnit.SpendMP(selectedSkill.mpCost);
        RefreshUnitHUD(activeUnit);

        dialogueText.text = $"¡{activeUnit.unitName} ejecuta {selectedSkill.skillName}!";
        yield return StartCoroutine(WaitForClick());

        // Resolver la habilidad por tipo
        switch (selectedSkill.skillType)
        {
            case SkillType.Physical:
            case SkillType.Magical:
                yield return StartCoroutine(ResolveDamageSkill(target));
                break;

            case SkillType.Heal:
                yield return StartCoroutine(ResolveHealSkill(target));
                break;

            case SkillType.Poison:
                yield return StartCoroutine(ResolvePoisonSkill(target, false));
                break;

            case SkillType.PhysicalPoison:
                yield return StartCoroutine(ResolvePoisonSkill(target, true));
                break;
        }

        AdvanceTurn();
    }

    IEnumerator ResolveDamageSkill(Unit target)
    {
        bool isPhysical = selectedSkill.skillType == SkillType.Physical;
        
        if (selectedSkill.targetType == TargetType.SingleEnemy)
        {
            int rawDmg = activeUnit.CalculateSkillDamage(selectedSkill);
            int finalDmg = target.TakeDamage(rawDmg, isPhysical);
            RefreshUnitHUD(target);

            // Ejecutar pasiva Ironhide del Guerrero si recibe daño físico
            if (isPhysical && target.classData != null && target.classData.passiveAbility == PassiveAbility.Ironhide)
            {
                target.AddTempDefense(target.classData.passiveValue);
                dialogueText.text = $"¡{target.unitName} recibe {finalDmg} de daño!\n¡La piel del Guerrero se endurece!";
            }
            else
            {
                dialogueText.text = $"¡{target.unitName} recibe {finalDmg} de daño!";
            }
            yield return StartCoroutine(WaitForClick());

            if (target.IsDead)
            {
                dialogueText.text = $"¡{target.unitName} ha caído derrotado!";
                yield return StartCoroutine(WaitForClick());
            }
        }
        else if (selectedSkill.targetType == TargetType.AllEnemies)
        {
            dialogueText.text = "¡Afecta a todos los rivales!";
            yield return StartCoroutine(WaitForClick());

            var targets = enemyTeam.Where(u => !u.IsDead).ToList();
            foreach (var t in targets)
            {
                int rawDmg = activeUnit.CalculateSkillDamage(selectedSkill);
                int finalDmg = t.TakeDamage(rawDmg, isPhysical);
                RefreshUnitHUD(t);

                // Ejecutar pasiva Ironhide del Guerrero si recibe daño físico
                if (isPhysical && t.classData != null && t.classData.passiveAbility == PassiveAbility.Ironhide)
                {
                    t.AddTempDefense(t.classData.passiveValue);
                    dialogueText.text = $"¡{t.unitName} recibe {finalDmg} de daño!\n¡La piel del Guerrero se endurece!";
                }
                else
                {
                    dialogueText.text = $"¡{t.unitName} recibe {finalDmg} de daño!";
                }
                yield return StartCoroutine(WaitForClick());

                if (t.IsDead)
                {
                    dialogueText.text = $"¡{t.unitName} ha caído derrotado!";
                    yield return StartCoroutine(WaitForClick());
                }
            }
        }
    }

    IEnumerator ResolveHealSkill(Unit target)
    {
        if (selectedSkill.targetType == TargetType.SingleAlly || selectedSkill.targetType == TargetType.Self)
        {
            Unit finalTarget = (selectedSkill.targetType == TargetType.Self) ? activeUnit : target;
            int healPower = activeUnit.CalculateSkillDamage(selectedSkill); // Usa magicPower
            int cured = finalTarget.Heal(healPower);
            RefreshUnitHUD(finalTarget);

            dialogueText.text = $"¡{finalTarget.unitName} recupera {cured} HP de salud pantanosa!";
            yield return StartCoroutine(WaitForClick());

            // Ejecutar pasiva TideCaller del Chamán después de curar
            if (activeUnit.classData != null && activeUnit.classData.passiveAbility == PassiveAbility.TideCaller && cured > 0)
            {
                activeUnit.RestoreMP(activeUnit.classData.passiveValue);
                RefreshUnitHUD(activeUnit);
                dialogueText.text = $"¡La energía mágica retorna a {activeUnit.unitName}! Recupera {activeUnit.classData.passiveValue} MP.";
                yield return StartCoroutine(WaitForClick());
            }
        }
        else if (selectedSkill.targetType == TargetType.AllAllies)
        {
            var targets = playerTeam.Where(u => !u.IsDead).ToList();
            foreach (var t in targets)
            {
                int healPower = activeUnit.CalculateSkillDamage(selectedSkill);
                int cured = t.Heal(healPower);
                RefreshUnitHUD(t);

                dialogueText.text = $"¡{t.unitName} recupera {cured} HP!";
                yield return StartCoroutine(WaitForClick());
            }

            // Ejecutar pasiva TideCaller del Chamán después de curar todos
            if (activeUnit.classData != null && activeUnit.classData.passiveAbility == PassiveAbility.TideCaller)
            {
                activeUnit.RestoreMP(activeUnit.classData.passiveValue);
                RefreshUnitHUD(activeUnit);
                dialogueText.text = $"¡La energía mágica retorna a {activeUnit.unitName}! Recupera {activeUnit.classData.passiveValue} MP.";
                yield return StartCoroutine(WaitForClick());
            }
        }
    }

    IEnumerator ResolvePoisonSkill(Unit target, bool dealDamageFirst)
    {
        if (dealDamageFirst)
        {
            int rawDmg = activeUnit.CalculateSkillDamage(selectedSkill);
            int finalDmg = target.TakeDamage(rawDmg, true);
            RefreshUnitHUD(target);
            dialogueText.text = $"¡Dardo directo! {target.unitName} sufre {finalDmg} de daño.";
            yield return StartCoroutine(WaitForClick());
        }

        // Aplicar el veneno
        target.ApplyPoison(selectedSkill.poisonDamagePerTurn, selectedSkill.poisonDuration);
        RefreshUnitHUD(target);

        dialogueText.text = $"¡{target.unitName} ha sido envenenado con lodo tóxico por {selectedSkill.poisonDuration} turnos!";
        yield return StartCoroutine(WaitForClick());

        if (target.IsDead)
        {
            dialogueText.text = $"¡{target.unitName} ha caído!";
            yield return StartCoroutine(WaitForClick());
        }
    }

    IEnumerator PlayerDefend()
    {
        state = BattleState.EXECUTING_ACTION;
        SetActionButtonsInteractable(false);

        activeUnit.SetDefending(true);
        RefreshUnitHUD(activeUnit);

        dialogueText.text = $"¡{activeUnit.unitName} se protege con su garrote reduciendo el daño a la mitad!";
        yield return StartCoroutine(WaitForClick());

        AdvanceTurn();
    }

    IEnumerator PlayerPass()
    {
        state = BattleState.EXECUTING_ACTION;
        SetActionButtonsInteractable(false);

        dialogueText.text = $"¡{activeUnit.unitName} cede el turno para recuperar fuerzas!";
        yield return StartCoroutine(WaitForClick());

        // Ejecutar pasiva si aplica (ej: Pícaro extiende veneno)
        if (activeUnit.classData != null && activeUnit.classData.passiveAbility == PassiveAbility.VenomousStrike)
        {
            yield return StartCoroutine(ExecuteVenomousStrikePassive(activeUnit));
        }

        AdvanceTurn();
    }

    // ════════════════════════════════════════════════════════════════════════
    // TURNO DEL ENEMIGO (IA) — También usa WaitForClick()
    // ════════════════════════════════════════════════════════════════════════

    private void ExecuteEnemyAI()
    {
        // Llamar a la clase inteligente de IA
        EnemyAI.AIDecision decision = EnemyAI.Decide(activeUnit, enemyTeam, playerTeam);

        switch (decision.actionType)
        {
            case EnemyAI.AIActionType.Attack:
                StartCoroutine(EnemyAttack(decision.target));
                break;

            case EnemyAI.AIActionType.Skill:
                selectedSkill = decision.chosenSkill;
                StartCoroutine(ExecuteEnemySkill(decision.target));
                break;

            case EnemyAI.AIActionType.Defend:
                StartCoroutine(EnemyDefend());
                break;

            case EnemyAI.AIActionType.Pass:
                StartCoroutine(EnemyPass());
                break;
        }
    }

    IEnumerator EnemyAttack(Unit target)
    {
        int damage = activeUnit.strength;
        int finalDamage = target.TakeDamage(damage, true);
        RefreshUnitHUD(target);

        dialogueText.text = $"¡El rival {activeUnit.unitName} ataca salvajemente a {target.unitName}!\n" +
                            $"¡{target.unitName} recibe {finalDamage} de daño físico!";

        // Ejecutar pasiva Ironhide del Guerrero si recibe daño físico
        if (target.classData != null && target.classData.passiveAbility == PassiveAbility.Ironhide)
        {
            target.AddTempDefense(target.classData.passiveValue);
            dialogueText.text += "\n¡La piel del Guerrero se endurece!";
        }

        yield return StartCoroutine(WaitForClick());

        if (target.IsDead)
        {
            dialogueText.text = $"¡Oh no! ¡{target.unitName} ha sido noqueado!";
            yield return StartCoroutine(WaitForClick());
        }

        AdvanceTurn();
    }

    IEnumerator ExecuteEnemySkill(Unit target)
    {
        activeUnit.SpendMP(selectedSkill.mpCost);
        RefreshUnitHUD(activeUnit);

        dialogueText.text = $"¡El rival {activeUnit.unitName} conjura {selectedSkill.skillName}!";
        yield return StartCoroutine(WaitForClick());

        switch (selectedSkill.skillType)
        {
            case SkillType.Physical:
            case SkillType.Magical:
                yield return StartCoroutine(ResolveDamageSkill(target));
                break;

            case SkillType.Heal:
                yield return StartCoroutine(ResolveHealSkill(target));
                break;

            case SkillType.Poison:
                yield return StartCoroutine(ResolvePoisonSkill(target, false));
                break;

            case SkillType.PhysicalPoison:
                yield return StartCoroutine(ResolvePoisonSkill(target, true));
                break;
        }

        AdvanceTurn();
    }

    IEnumerator EnemyDefend()
    {
        activeUnit.SetDefending(true);
        RefreshUnitHUD(activeUnit);

        dialogueText.text = $"¡El rival {activeUnit.unitName} alza su escudo y se prepara para recibir golpes!";
        yield return StartCoroutine(WaitForClick());

        AdvanceTurn();
    }

    IEnumerator EnemyPass()
    {
        dialogueText.text = $"¡El rival {activeUnit.unitName} gruñe y pasa el turno!";
        yield return StartCoroutine(WaitForClick());

        // Ejecutar pasiva si aplica (ej: Pícaro extiende veneno)
        if (activeUnit.classData != null && activeUnit.classData.passiveAbility == PassiveAbility.VenomousStrike)
        {
            yield return StartCoroutine(ExecuteVenomousStrikePassive(activeUnit));
        }

        AdvanceTurn();
    }

    // ════════════════════════════════════════════════════════════════════════
    // COMPROBACIONES DE CONDICIÓN DE VICTORIA
    // ════════════════════════════════════════════════════════════════════════

    private bool CheckBattleOver()
    {
        // Todos los del jugador están muertos
        bool allPlayersDead = playerTeam.All(u => u.IsDead);
        // Todos los enemigos están muertos
        bool allEnemiesDead = enemyTeam.All(u => u.IsDead);

        if (allPlayersDead)
        {
            state = BattleState.LOST;
            StartCoroutine(EndBattle());
            return true;
        }
        else if (allEnemiesDead)
        {
            state = BattleState.WON;
            StartCoroutine(EndBattle());
            return true;
        }

        return false;
    }

    IEnumerator EndBattle()
    {
        SetActionButtonsInteractable(false);
        skillPanel.SetActive(false);

        if (state == BattleState.WON)
        {
            dialogueText.text = "¡Todos los clanes rivales muerden el barro! ¡La corona del pantano es tuya!";
            yield return StartCoroutine(WaitForClick());
            resultText.text = "🏆 ¡VICTORIA!";
            resultText.color = new Color(0.2f, 1f, 0.4f);
        }
        else if (state == BattleState.LOST)
        {
            dialogueText.text = "Tu clan ha mordido el lodo... Has fracasado en la guerra de los 100 gitanillos.";
            yield return StartCoroutine(WaitForClick());
            resultText.text = "💀 ¡DERROTA!";
            resultText.color = new Color(1f, 0.3f, 0.3f);
        }

        resultText.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════════
    // UTILIDADES DE INTERFAZ (REFRESH)
    // ════════════════════════════════════════════════════════════════════════

    private void SetActionButtonsInteractable(bool interactable)
    {
        attackButton.interactable = interactable;
        skillButton.interactable = interactable;
        defendButton.interactable = interactable;
        passButton.interactable = interactable;
    }

    private void HighlightActiveUnitHUD(Unit unit)
    {
        if (playerTeam.Contains(unit))
        {
            int idx = playerTeam.IndexOf(unit);
            playerTeamHUD.SetActiveUnit(idx);
        }
        else if (enemyTeam.Contains(unit))
        {
            int idx = enemyTeam.IndexOf(unit);
            enemyTeamHUD.SetActiveUnit(idx);
        }
    }

    private void RefreshUnitHUD(Unit unit)
    {
        if (playerTeam.Contains(unit))
        {
            int idx = playerTeam.IndexOf(unit);
            playerTeamHUD.RefreshUnit(idx, unit);
        }
        else if (enemyTeam.Contains(unit))
        {
            int idx = enemyTeam.IndexOf(unit);
            enemyTeamHUD.RefreshUnit(idx, unit);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // EJECUCIÓN DE PASIVAS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ejecuta la pasiva VenomousStrike del Pícaro: extiende la duración del veneno
    /// de todos los enemigos afectados en +passiveValue turnos.
    /// </summary>
    IEnumerator ExecuteVenomousStrikePassive(Unit picaroUnit)
    {
        bool extendedAny = false;
        List<Unit> targets = picaroUnit == playerTeam[0] || playerTeam.Contains(picaroUnit) ? enemyTeam : playerTeam;

        foreach (Unit target in targets)
        {
            if (target.IsPoisoned && target.ExtendPoison(picaroUnit.classData.passiveValue))
            {
                extendedAny = true;
            }
        }

        if (extendedAny)
        {
            dialogueText.text = $"¡{picaroUnit.unitName} extiende el veneno en sus rivales con un veneno más potente!";
            yield return StartCoroutine(WaitForClick());
            
            // Actualizar HUDs de objetivos afectados
            foreach (Unit target in targets)
            {
                if (target.IsPoisoned)
                    RefreshUnitHUD(target);
            }
        }
    }
}
