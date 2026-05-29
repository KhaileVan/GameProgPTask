using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Timer")]
    public float roundTime = 10f;
    float timer;
    bool roundActive;

    TextMeshProUGUI timerText;

    GameObject player1;
    GameObject player2;

    public string lastHolderTag;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        StartRound(); // 🔥 ensures timer works on first scene
    }

    void Update()
    {
        if (!roundActive) return;

        timer -= Time.deltaTime;

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(timer).ToString();

        if (timer <= 0f)
        {
            roundActive = false;
            SceneController.instance.NextScene();
        }
    }

    // =========================
    // SCENE LOAD
    // =========================

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;

        StartCoroutine(SetupSceneDelayed());
    }

    IEnumerator SetupSceneDelayed()
    {
        yield return null; // wait 1 frame

        SetupScene();
        StartCoroutine(SetupCollisionDelayed());

        StartRound(); // 🔥 restart timer every scene
    }

    void SetupScene()
    {
        player1 = GameObject.FindGameObjectWithTag("Player1");
        player2 = GameObject.FindGameObjectWithTag("Player2");

        timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
    }

    void StartRound()
    {
        timer = roundTime;
        roundActive = true;
    }

    // =========================
    // PLAYER COLLISION FIX
    // =========================

    IEnumerator SetupCollisionDelayed()
    {
        yield return null;
        yield return null;

        if (player1 == null || player2 == null) yield break;

        Collider2D c1 = player1.GetComponent<Collider2D>();
        Collider2D c2 = player2.GetComponent<Collider2D>();

        if (c1 != null && c2 != null)
        {
            Physics2D.IgnoreCollision(c1, c2, true);
        }
    }

    // =========================
    // LAST HOLDER
    // =========================

    public void SetLastHolder(Transform player)
    {
        if (player == null) return;
        lastHolderTag = player.tag;
    }
}