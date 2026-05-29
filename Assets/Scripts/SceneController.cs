using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

    public int currentRound = 1;
    public int maxRounds = 5;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void NextScene()
    {
        currentRound++;

        if (currentRound > maxRounds)
        {
            Debug.Log("GAME OVER");
            return;
        }

        string sceneName = (currentRound % 2 == 1) ? "Level1" : "Level2";

        SceneManager.LoadScene(sceneName);
    }
}