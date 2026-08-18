using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject pauseMenuPanel; // Ссылка на панель паузы

    [Header("Настройки сцен")]
    public string mainMenuSceneName = "Menu"; // Имя сцены главного меню (можно изменить в Инспекторе)

    private bool isPaused = false;

    public bool IsPaused => isPaused;

    // Метод для открытия/закрытия панели (удобно для кнопок)
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    // Ставит игру на паузу и показывает меню
    public void PauseGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);  // Показываем панель
        }

        Time.timeScale = 0f;                 // Останавливаем время и физику кубиков
        isPaused = true;
    }

    // Вызывается при нажатии кнопки "Вернуться в игру"
    public void ResumeGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false); // Скрываем панель
        }

        Time.timeScale = 1f;                 // Возобновляем время
        isPaused = false;
    }

    // Вызывается при нажатии кнопки "Выйти в главное меню"
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;                 // Обязательно возвращаем нормальную скорость времени перед сменой сцены!
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("CubeDrop");
    }
}