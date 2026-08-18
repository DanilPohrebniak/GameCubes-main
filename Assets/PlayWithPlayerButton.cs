using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayWithPlayerButton : MonoBehaviour
{
    public void PlayWithPlayer()
    {
        GameSettings.PlayVsBot = false;
        SceneManager.LoadScene("CubeDrop");
    }
}