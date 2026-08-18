using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayWithBotButton : MonoBehaviour
{
    public void PlayWithBot()
    {
        GameSettings.PlayVsBot = true;
        SceneManager.LoadScene("CubeDrop");
    }
}