using UnityEngine;
using TMPro;

/// <summary>
/// Простой UI для отображения счёта, как в KCD2:
///
///            Игрок      Goal      Opponent
///   Score      350       3000        0
///   Round        0                   0
///   Selected     0                   0
///
/// Ничего лишнего: только актуальные цифры. Оформление UI (шрифты, рамки,
/// подложки и т.п.) не входит в этот скрипт — только логика вывода чисел.
///
/// НАСТРОЙКА В ИНСПЕКТОРЕ:
/// 1. Создайте Canvas -> под ним 3 колонки текстов (TextMeshPro - Text), как в таблице выше.
/// 2. Перетащите созданные TMP_Text объекты в соответствующие поля ниже.
/// 3. Перетащите на сцене объекты с GameManager и DiceSelectionManager (или оставьте
///    пустыми — GameManager подхватится сам через GameManager.Instance).
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("Ссылки")]
    public GameManager gameManager;
    public DiceSelectionManager selectionManager;

    [Header("Игрок (слева, как 'Henry')")]
    public TMP_Text player1ScoreText;    // общий счёт (банк)
    public TMP_Text player1RoundText;    // очки текущего раунда
    public TMP_Text player1SelectedText; // очки выделенных сейчас костей

    [Header("Оппонент (справа)")]
    public TMP_Text player2ScoreText;
    public TMP_Text player2RoundText;
    public TMP_Text player2SelectedText;

    [Header("Цель")]
    public TMP_Text goalText;

    void Awake()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
    }

    void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.OnScoreChanged += HandleScoreChanged;
            gameManager.OnRoundScoreChanged += HandleRoundScoreChanged;
        }
        if (selectionManager != null)
            selectionManager.OnSelectionScoreChanged += HandleSelectionScoreChanged;
    }

    void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnScoreChanged -= HandleScoreChanged;
            gameManager.OnRoundScoreChanged -= HandleRoundScoreChanged;
        }
        if (selectionManager != null)
            selectionManager.OnSelectionScoreChanged -= HandleSelectionScoreChanged;
    }

    void Start()
    {
        // Начальные значения при старте партии
        if (goalText != null) goalText.text = gameManager.winScore.ToString();

        SetText(player1ScoreText, gameManager.GetScore(1));
        SetText(player2ScoreText, gameManager.GetScore(2));

        SetText(player1RoundText, 0);
        SetText(player2RoundText, 0);
        SetText(player1SelectedText, 0);
        SetText(player2SelectedText, 0);
    }

    private void HandleScoreChanged(int playerIndex, int newScore)
    {
        SetText(playerIndex == 1 ? player1ScoreText : player2ScoreText, newScore);
    }

    private void HandleRoundScoreChanged(int playerIndex, int roundScore)
    {
        // Обновляем только колонку игрока, у которого изменился счёт раунда.
        // Колонка второго игрока не трогается — так его последний незабаженный
        // (или "замороженный" после зонка) счёт раунда остаётся на экране,
        // пока не начнётся его собственный новый ход.
        SetText(playerIndex == 1 ? player1RoundText : player2RoundText, roundScore);

        // Выбор костей относится к текущему броску — сбрасываем "Selected"
        // только у того игрока, чей раунд только что изменился.
        SetText(playerIndex == 1 ? player1SelectedText : player2SelectedText, 0);
    }

    private void HandleSelectionScoreChanged(int selectedScore)
    {
        int activePlayer = gameManager.CurrentPlayer;
        SetText(activePlayer == 1 ? player1SelectedText : player2SelectedText, selectedScore);
    }

    private void SetText(TMP_Text field, int value)
    {
        if (field != null) field.text = value.ToString();
    }
}