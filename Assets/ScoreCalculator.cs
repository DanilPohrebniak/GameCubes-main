using System.Linq;
using System;

public static class ScoreCalculator
{
    public static int CalculateScore(int[] diceValues)
    {
        if (diceValues == null || diceValues.Length == 0) return 0;

        int score = 0;
        int[] counts = new int[7]; // индексы 1–6

        foreach (int v in diceValues)
        {
            if (v >= 1 && v <= 6) counts[v]++;
        }

        // Строгая проверка на большой стрит (1, 2, 3, 4, 5, 6)
        bool isFullStraight = counts.Skip(1).All(c => c == 1);
        if (isFullStraight) return 1500;

        // Строгая проверка на малый стрит 1-5 (по одной штуке от 1 до 5)
        bool isSmallStraight1to5 = counts[1] == 1 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1 && counts[6] == 0;
        if (isSmallStraight1to5) return 500;

        // Строгая проверка на малый стрит 2-6 (по одной штуке от 2 до 6)
        bool isSmallStraight2to6 = counts[1] == 0 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1 && counts[6] == 1;
        if (isSmallStraight2to6) return 750;

        // Тройки и больше
        for (int i = 1; i <= 6; i++)
        {
            if (counts[i] >= 3)
            {
                int baseScore = (i == 1) ? 1000 : i * 100;
                int extra = counts[i] - 3;
                score += baseScore * (int)Math.Pow(2, extra);

                counts[i] = 0; // Обнуляем, чтобы не учитывать эти кубики снова
            }
        }

        // Единицы и пятерки (оставшиеся)
        score += counts[1] * 100;
        score += counts[5] * 50;
        counts[1] = 0;
        counts[5] = 0;

        // Если среди выбранных костей остались 2/3/4/6, которые никуда не "вошли"
        // (не часть тройки, не часть стрита) — это НЕВАЛИДНЫЙ набор.
        // По правилам нельзя фиксировать/банковать кости, которые сами по себе
        // не приносят очков, вместе с очковыми — весь выбор должен считаться из очковых.
        bool hasLeftoverJunk = counts[2] > 0 || counts[3] > 0 || counts[4] > 0 || counts[6] > 0;
        if (hasLeftoverJunk) return 0;

        return score;
    }

    /// <summary>
    /// Мягкая проверка: есть ли среди ЭТИХ костей вообще хоть одна очковая
    /// кость или комбинация — не важно, сколько вокруг "мусора" (2/3/4/6,
    /// не входящих в тройку/стрит). Мусор в целом броске — это норма, не зонк.
    ///
    /// Используется ТОЛЬКО для проверки ФАРКЛА (зонка) по всем выпавшим
    /// (незалоченным) костям. Для проверки конкретного ВЫБОРА игрока перед
    /// фиксацией/банковкой используйте строгий CalculateScore — там мусор
    /// в наборе как раз должен считаться невалидным выбором.
    /// </summary>
    public static bool HasAnyScoringDice(int[] diceValues)
    {
        if (diceValues == null || diceValues.Length == 0) return false;

        int[] counts = new int[7];
        foreach (int v in diceValues)
        {
            if (v >= 1 && v <= 6) counts[v]++;
        }

        bool isFullStraight = counts.Skip(1).All(c => c == 1);
        if (isFullStraight) return true;

        bool isSmallStraight1to5 = counts[1] == 1 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1 && counts[6] == 0;
        if (isSmallStraight1to5) return true;

        bool isSmallStraight2to6 = counts[1] == 0 && counts[2] == 1 && counts[3] == 1 && counts[4] == 1 && counts[5] == 1 && counts[6] == 1;
        if (isSmallStraight2to6) return true;

        for (int i = 1; i <= 6; i++)
        {
            if (counts[i] >= 3) return true;
        }

        // Хотя бы одна одиночная единица или пятёрка тоже спасает от зонка
        return counts[1] > 0 || counts[5] > 0;
    }
}