using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiceSelectionManager : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Выбери здесь слой Dice")]
    public LayerMask diceLayer;

    [Header("Данные")]
    public List<DiceController> selectedDice = new List<DiceController>();

    // Для UI: очки выделенных прямо сейчас костей (обновляется при каждом клике)
    public event Action<int> OnSelectionScoreChanged;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, diceLayer))
        {
            DiceController clickedDice = hit.collider.GetComponentInParent<DiceController>();

            if (clickedDice != null)
            {
                clickedDice.ToggleSelection();

                if (clickedDice.IsSelected)
                {
                    selectedDice.Add(clickedDice);
                }
                else
                {
                    selectedDice.Remove(clickedDice);
                }

                // Считаем и выводим
                int score = CalculateScore(selectedDice);
                Debug.Log($"<color=green>Текущий счет выбранных костей: {score}</color>");
                OnSelectionScoreChanged?.Invoke(score);
            }
        }
    }

    // Текущие очки выделения (для начальной инициализации UI)
    public int GetSelectedScore() => CalculateScore(selectedDice);

    // Вызывается из GameManager перед новым броском
    public void ClearSelection()
    {
        // Принудительно отменяем выделение у всех, перед очисткой списка
        foreach (var dice in selectedDice)
        {
            if (!dice.IsLocked && dice.IsSelected) dice.ToggleSelection();
        }
        selectedDice.Clear();
        OnSelectionScoreChanged?.Invoke(0);
    }

    private int CalculateScore(List<DiceController> selected)
    {
        if (selected.Count == 0) return 0;

        // Используем ту же логику, что и GameManager при банковке/фиксации —
        // раньше здесь была отдельная урезанная копия, которая не умела
        // считать стриты (1-6, 1-5, 2-6) и обнуляла весь подсчёт из-за этого.
        int[] values = selected.Select(d => d.CurrentValue).ToArray();
        return ScoreCalculator.CalculateScore(values);
    }
}