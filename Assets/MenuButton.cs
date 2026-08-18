using UnityEngine;

public class MenuButton : MonoBehaviour
{
    [Header("Ссылки")]
    public PauseMenu pauseMenu; // Ссылка на менеджер паузы

    // Вызывается по клику на UI-кнопку "Menu"
    public void OpenMenuPanel()
    {
        if (pauseMenu != null)
        {
            pauseMenu.PauseGame();
        }
        else
        {
            // Запасной вариант: пытаемся найти PauseMenu на сцене, если забыли привязать в Инспекторе
            pauseMenu = FindFirstObjectByType<PauseMenu>();
            if (pauseMenu != null)
            {
                pauseMenu.PauseGame();
            }
            else
            {
                Debug.LogError("MenuButton: На сцене не найден скрипт PauseMenu!");
            }
        }
    }
}