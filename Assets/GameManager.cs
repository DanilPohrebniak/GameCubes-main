using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public CubeSpawner spawner;
    public DiceManager diceManager;
    public DiceSelectionManager selectionManager;

    [Header("Бот (Игрок 2)")]
    public bool player2IsBot = true;
    [Tooltip("Минимальная и максимальная задержка \"раздумий\" бота, сек")]
    public float botThinkDelayMin = 0.6f;
    public float botThinkDelayMax = 1.4f;
    [Tooltip("При каком накопленном счёте за ход бот предпочитает забрать очки в банк")]
    public int botBankThreshold = 300;

    // Вынесли из локальной переменной GameLoop, чтобы бот тоже мог это читать
    private bool isFirstRollOfTurn = true;

    private bool IsCurrentPlayerBot => currentPlayer == 2 && player2IsBot;

    [Header("Правила игры")]
    [Tooltip("Сколько очков нужно набрать, чтобы выиграть партию")]
    public int winScore = 4000;

    [Tooltip("Сколько секунд нужно удерживать ESC для сдачи")]
    public float surrenderHoldTime = 2f;

    private int currentPlayer = 1;
    private int[] scores = new int[2];
    private int currentTurnScore = 0; // очки, набранные в текущем раунде (ещё не в банке)

    private bool throwInput = false;
    private bool bankInput = false;
    private bool canAcceptInput = false; // Защита от лишних нажатий
    private bool gameEnded = false;

    private float escapeHoldTime = 0f; // Таймер удержания ESC

    // События для UI (счёт, победа) — можно подписаться из UI-скрипта
    public event Action<int, int> OnScoreChanged;      // (playerIndex 1/2, newScore) — общий банк игрока
    public event Action<int, int> OnRoundScoreChanged; // (playerIndex 1/2, roundScore) — очки текущего раунда
    public event Action<int> OnTurnBusted;             // (playerIndex) — ход сгорел
    public event Action<int> OnGameWon;                // (playerIndex) — победитель

    [Header("Ссылки")]
    [Tooltip("Объект с картинкой-подсказкой (Image), который нужно показать/скрыть")]
    public GameObject helpPanel;

    [Header("UI победы")]
    [Tooltip("Панель/картинка с уведомлением о победе. Показывается при выигрыше, ENTER закрывает и перезапускает партию.")]
    public GameObject winPanel;
    [Tooltip("Необязательно: текст на панели победы, куда впишется имя победителя и счёт")]
    public TMP_Text winText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        player2IsBot = GameSettings.PlayVsBot;

        // Диагностика: покажет в консоли, какое значение winScore реально
        // подхватилось на старте — если тут не 500, значит правится не тот
        // объект/префаб (см. пояснение в чате). Строку можно убрать потом.
        Debug.Log($"<color=orange>[DEBUG] winScore на старте партии = {winScore}, объект: {gameObject.name}</color>");

        // На всякий случай гарантируем, что при старте панели скрыты
        if (helpPanel != null) helpPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        StartCoroutine(GameLoop());
    }

    void Update()
    {
        // Пока не gameEnded — обычная игра. Как только gameEnded, единственное,
        // что нас интересует — ENTER на экране победы, чтобы перезапустить партию.
        if (gameEnded)
        {
            if (winPanel != null && winPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    RestartGame();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    QuitGame();
                }
            }
            return;
        }

        bool isPanelActive = helpPanel != null && helpPanel.activeSelf;

        // Логика удержания ESC для сдачи (работает только если панель подсказок скрыта)
        if (!isPanelActive)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                escapeHoldTime += Time.deltaTime;
                if (escapeHoldTime >= surrenderHoldTime)
                {
                    Surrender();
                }
            }
            else
            {
                escapeHoldTime = 0f; // Сбрасываем таймер, если клавиша отпущена
            }
        }
        else
        {
            escapeHoldTime = 0f; // Сбрасываем таймер, если панель открыта
        }

        // Считываем нажатия только когда игра ждет действий игрока
        if (canAcceptInput)
        {
            if (Input.GetKeyDown(KeyCode.Space)) bankInput = true;
            if (Input.GetKeyDown(KeyCode.F)) throwInput = true;

            if (helpPanel == null) return;

            if (!helpPanel.activeSelf && Input.GetKeyDown(KeyCode.E))
            {
                helpPanel.SetActive(true);
            }
            else if (helpPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                helpPanel.SetActive(false);
            }
        }
    }

    // ---- Публичный API для UI-кнопок (альтернатива клавиатуре) ----

    public void RequestThrow()
    {
        if (canAcceptInput) throwInput = true;
    }

    public void RequestBank()
    {
        if (canAcceptInput) bankInput = true;
    }

    public int GetScore(int playerIndex) => scores[playerIndex - 1];
    public int CurrentTurnScore => currentTurnScore;
    public int CurrentPlayer => currentPlayer;
    public bool CanAcceptInput => canAcceptInput;
    public bool IsGameEnded => gameEnded;

    IEnumerator GameLoop()
    {
        while (!gameEnded)
        {
            Debug.Log($"<color=cyan>--- ХОД ИГРОКА {currentPlayer} --- Нажмите F для старта.</color>");
            diceManager.FullReset();
            selectionManager.ClearSelection();

            currentTurnScore = 0;
            OnRoundScoreChanged?.Invoke(currentPlayer, currentTurnScore);

            bool isTurnActive = true;
            isFirstRollOfTurn = true;

            while (isTurnActive && !gameEnded)
            {
                throwInput = false;
                bankInput = false;

                canAcceptInput = true;

                if (IsCurrentPlayerBot)
                    yield return StartCoroutine(BotTakeAction());
                else
                    yield return new WaitUntil(() => throwInput || bankInput || gameEnded);

                canAcceptInput = false;

                if (gameEnded) break;

                if (bankInput)
                {
                    if (isFirstRollOfTurn)
                    {
                        Debug.LogWarning("Вы еще не сделали первый бросок!");
                        continue;
                    }

                    int[] selected = selectionManager.selectedDice.Select(d => d.CurrentValue).ToArray();
                    currentTurnScore += ScoreCalculator.CalculateScore(selected);
                    OnRoundScoreChanged?.Invoke(currentPlayer, currentTurnScore);
                    BankScore(ref isTurnActive);
                    continue;
                }

                if (throwInput)
                {
                    if (!isFirstRollOfTurn)
                    {
                        int[] selectedValues = selectionManager.selectedDice.Select(d => d.CurrentValue).ToArray();
                        int scoreToLock = ScoreCalculator.CalculateScore(selectedValues);

                        if (scoreToLock == 0)
                        {
                            Debug.LogWarning("Вы должны выбрать хотя бы одну выигрышную кость перед перебросом!");
                            continue;
                        }

                        currentTurnScore += scoreToLock;
                        OnRoundScoreChanged?.Invoke(currentPlayer, currentTurnScore);
                        Debug.Log($"<color=yellow>Очки зафиксированы: {scoreToLock}. Буфер хода: {currentTurnScore}. Бросаем оставшиеся!</color>");

                        foreach (var d in selectionManager.selectedDice) d.LockDice();
                        selectionManager.ClearSelection();

                        if (diceManager.Dices.All(d => d.GetComponent<DiceController>().IsLocked))
                        {
                            Debug.Log("<color=magenta>ГОРЯЧИЕ КОСТИ! Все 6 кубиков сыграли. Вы можете бросить их все снова!</color>");
                            diceManager.FullReset();
                        }
                    }

                    isFirstRollOfTurn = false;

                    yield return StartCoroutine(spawner.ThrowWithHand());
                    if (gameEnded) break;

                    yield return new WaitUntil(() => diceManager.Dices.Count > 0 || gameEnded);
                    if (gameEnded) break;

                    yield return new WaitUntil(() => diceManager.AllStopped() || gameEnded);
                    if (gameEnded) break;

                    List<int> rolledUnlockedValues = new List<int>();
                    foreach (var dice in diceManager.Dices)
                    {
                        DiceController controller = dice.GetComponent<DiceController>();
                        if (controller != null && !controller.IsLocked)
                        {
                            controller.CurrentValue = dice.GetValue();
                            rolledUnlockedValues.Add(controller.CurrentValue);
                        }
                    }

                    if (!ScoreCalculator.HasAnyScoringDice(rolledUnlockedValues.ToArray()))
                    {
                        Debug.Log("<color=red>ФАРКЛ (ЗОНК)! Ни одной выигрышной кости. Все очки за ход сгорают!</color>");

                        int lostScore = currentTurnScore;
                        currentTurnScore = 0;

                        OnRoundScoreChanged?.Invoke(currentPlayer, lostScore);
                        isTurnActive = false;
                        OnTurnBusted?.Invoke(currentPlayer);
                        break;
                    }

                    Debug.Log($"Бросок успешен. Выберите кости. Затем нажмите: [F] зафиксировать и перебросить остаток | [Space] забрать всё в банк.");
                }
            }

            if (gameEnded) break;

            currentPlayer = (currentPlayer == 1) ? 2 : 1;
        }

        canAcceptInput = false;
    }

    private IEnumerator BotTakeAction()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(botThinkDelayMin, botThinkDelayMax));

        if (gameEnded) yield break;

        if (isFirstRollOfTurn)
        {
            throwInput = true;
            yield break;
        }

        yield return StartCoroutine(BotSelectScoringDiceAnimated());

        // Пауза после выбора костей, чтобы подсветка была заметна перед решением
        yield return new WaitForSeconds(1f);

        if (gameEnded) yield break;

        int potentialScore = currentTurnScore + selectionManager.GetSelectedScore();
        int unlockedRemaining = diceManager.Dices
            .Count(d => !d.GetComponent<DiceController>().IsLocked)
            - selectionManager.selectedDice.Count;

        bool shouldBank = potentialScore >= botBankThreshold || unlockedRemaining <= 1;

        if (!shouldBank && UnityEngine.Random.value < 0.15f) shouldBank = true;
        if (shouldBank && potentialScore < botBankThreshold && UnityEngine.Random.value < 0.2f) shouldBank = false;

        if (shouldBank)
            bankInput = true;
        else
            throwInput = true;
    }

    [Header("Бот — визуальный выбор костей")]
    [Tooltip("Задержка между подсветкой каждой выбираемой кости, сек")]
    public float botSelectDiceDelay = 0.25f;

    private IEnumerator BotSelectScoringDiceAnimated()
    {
        var unlockedRolled = diceManager.Dices
            .Select(d => d.GetComponent<DiceController>())
            .Where(c => c != null && !c.IsLocked && c.CurrentValue > 0)
            .ToList();

        if (unlockedRolled.Count == 0) yield break;

        int[] counts = new int[7];
        foreach (var c in unlockedRolled) counts[c.CurrentValue]++;

        bool isFullStraight = counts.Skip(1).All(v => v == 1);
        bool isSmallStraight1to5 = counts[1] == 1 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1 && counts[6] == 0;
        bool isSmallStraight2to6 = counts[1] == 0 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1 && counts[6] == 1;

        List<DiceController> toSelect = new List<DiceController>();

        if (isFullStraight || isSmallStraight1to5 || isSmallStraight2to6)
        {
            toSelect.AddRange(unlockedRolled);
        }
        else
        {
            for (int value = 1; value <= 6; value++)
            {
                if (counts[value] >= 3)
                    toSelect.AddRange(unlockedRolled.Where(c => c.CurrentValue == value));
            }

            foreach (var c in unlockedRolled)
            {
                if (toSelect.Contains(c)) continue;
                if (c.CurrentValue == 1 || c.CurrentValue == 5) toSelect.Add(c);
            }
        }

        // Подсвечиваем по одной кости с паузой — так видно, что именно выбирает бот
        foreach (var c in toSelect)
        {
            if (gameEnded) yield break;

            if (!c.IsSelected)
            {
                c.ToggleSelection();
                selectionManager.selectedDice.Add(c);
            }

            yield return new WaitForSeconds(botSelectDiceDelay);
        }
    } 

    private void BankScore(ref bool isTurnActive)
    {
        scores[currentPlayer - 1] += currentTurnScore;
        Debug.Log($"<color=green>Игрок {currentPlayer} забирает в банк {currentTurnScore} очков! Общий счет: {scores[0]} - {scores[1]}</color>");

        OnScoreChanged?.Invoke(currentPlayer, scores[currentPlayer - 1]);

        currentTurnScore = 0;
        OnRoundScoreChanged?.Invoke(currentPlayer, currentTurnScore);

        bankInput = false;
        throwInput = false;
        isTurnActive = false;
        selectionManager.ClearSelection();

        if (scores[currentPlayer - 1] >= winScore)
        {
            gameEnded = true;
            OnGameWon?.Invoke(currentPlayer);
            Debug.Log($"<color=magenta>🏆 ИГРОК {currentPlayer} НАБРАЛ {scores[currentPlayer - 1]} ОЧКОВ И ВЫИГРЫВАЕТ ПАРТИЮ! 🏆</color>");

            if (winPanel != null)
            {
                winPanel.SetActive(true);
                if (winText != null)
                    winText.text = $"Игрок {currentPlayer} побеждает!\nСчёт: {scores[currentPlayer - 1]}";
            }
        }
    }

    // Логика сдачи с мгновенным перезапуском игры
    private void Surrender()
    {
        Debug.Log($"<color=red>Игрок {currentPlayer} сдался! Мгновенный перезапуск партии...</color>");
        escapeHoldTime = 0f;
        ResetAndRestartGame();
    }

    // Перезапуск партии после победы (по ENTER на экране победы)
    private void RestartGame()
    {
        Debug.Log("<color=cyan>Перезапуск партии после победы...</color>");
        if (winPanel != null) winPanel.SetActive(false);
        ResetAndRestartGame();
    }

    // Полный выход из игры (по ESC на экране победы)
    private void QuitGame()
    {
        Debug.Log("<color=red>Выход из игры (ESC на экране победы)...</color>");

#if UNITY_EDITOR
        // В редакторе Application.Quit() ничего не делает — вместо этого
        // просто останавливаем Play Mode, чтобы удобно было тестировать.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Общая логика полного сброса состояния и рестарта GameLoop —
    // используется и при сдаче (Surrender), и при перезапуске после победы (RestartGame)
    private void ResetAndRestartGame()
    {
        // 1. Останавливаем текущий корутинный цикл GameLoop
        StopAllCoroutines();

        player2IsBot = GameSettings.PlayVsBot;

        // 2. Полностью обнуляем внутреннее состояние игры
        scores[0] = 0;
        scores[1] = 0;
        currentTurnScore = 0;
        currentPlayer = 1;      // Первым всегда начинает Игрок 1
        gameEnded = false;      // Сбрасываем флаг завершения, чтобы цикл мог работать

        throwInput = false;
        bankInput = false;
        canAcceptInput = false;

        // 3. Очищаем кости со стола и убираем выделения кубиков мышкой
        if (diceManager != null) diceManager.FullReset();
        if (selectionManager != null) selectionManager.ClearSelection();

        // 4. Оповещаем UI-скрипты через события, что все очки стали равны 0
        OnScoreChanged?.Invoke(1, 0);
        OnScoreChanged?.Invoke(2, 0);
        OnRoundScoreChanged?.Invoke(1, 0);
        OnRoundScoreChanged?.Invoke(2, 0);

        // 5. Запускаем новый, чистый игровой цикл для Игрока 1
        StartCoroutine(GameLoop());
    }
}