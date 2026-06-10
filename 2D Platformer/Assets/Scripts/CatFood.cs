using UnityEngine;
using UnityEngine.SceneManagement;

public class CatFood : MonoBehaviour
{
    [Header("Settings")]
    public string nextSceneName = "Level2";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            TriggerLevelTransition();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerController>() != null)
        {
            TriggerLevelTransition();
        }
    }

    private void TriggerLevelTransition()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();

        // Find the AllyCat in the scene
        AllyController ally = FindFirstObjectByType<AllyController>();
        if (ally != null && ally.gameObject != null)
        {
            // Make sure the AllyCat survives scene transitions
            DontDestroyOnLoad(ally.gameObject);
        }

        // Load Level2 scene
        SceneManager.LoadScene(nextSceneName);
    }
}
