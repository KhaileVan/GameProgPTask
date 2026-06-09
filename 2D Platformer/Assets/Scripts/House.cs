using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class House : MonoBehaviour
{
    private bool isPlayerInside = false;
    private bool isAllyInside = false;
    private bool gameEnded = false;

    private GameObject playerObj;
    private GameObject allyObj;
    private GameObject canvasObj;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (gameEnded) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            isPlayerInside = true;
            playerObj = other.gameObject;
            CheckEndingCondition();
        }
        else if (other.GetComponent<AllyController>() != null)
        {
            isAllyInside = true;
            allyObj = other.gameObject;
            CheckEndingCondition();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (gameEnded) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            isPlayerInside = false;
        }
        else if (other.GetComponent<AllyController>() != null)
        {
            isAllyInside = false;
        }
    }

    private void CheckEndingCondition()
    {
        if (isPlayerInside && isAllyInside)
        {
            TriggerGameEnd();
        }
    }

    private void TriggerGameEnd()
    {
        gameEnded = true;

        // Hide and disable player
        if (playerObj != null)
        {
            DisableCharacter(playerObj);
        }

        // Hide and disable ally
        if (allyObj != null)
        {
            DisableCharacter(allyObj);
        }

        // Create the ending canvas
        CreateEndingUI();
    }

    private void DisableCharacter(GameObject obj)
    {
        // Disable renderers
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            sr.enabled = false;
        }

        // Disable colliders
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Freeze physics
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable scripts (so they don't move or execute logic)
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this)
            {
                script.enabled = false;
            }
        }
    }

    private void CreateEndingUI()
    {
        // Ensure EventSystem is present
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if UNITY_INPUT_SYSTEM || ENABLE_INPUT_SYSTEM
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        // 1. Create Canvas
        canvasObj = new GameObject("EndingCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Prevent destruction
        DontDestroyOnLoad(canvasObj);

        // 2. Create Background Image
        GameObject bgObj = new GameObject("EndingBackground");
        bgObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0f);
        
        // Stretch bgImage to full screen
        RectTransform bgRect = bgImage.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 3. Create Text Image
        GameObject textObj = new GameObject("EndingTextImage");
        textObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image textImage = textObj.AddComponent<UnityEngine.UI.Image>();
        Sprite textSprite = GeneratePixelTextSprite("END OF THE GAME", new Color(1f, 0.82f, 0f), Color.black);
        textImage.sprite = textSprite;
        textImage.color = new Color(1f, 1f, 1f, 0f);

        // Adjust size
        RectTransform textRect = textImage.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);

        // 6x scale factor for crisp pixel look
        float pixelScale = 6f;
        textRect.sizeDelta = new Vector2(textSprite.rect.width * pixelScale, textSprite.rect.height * pixelScale);
        textRect.anchoredPosition = new Vector2(0f, 80f);

        // 4. Create "Play Again!" Button
        GameObject buttonObj = new GameObject("PlayAgainButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image btnImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        Sprite btnSprite = GeneratePixelTextSprite("PLAY AGAIN!", Color.white, Color.black);
        btnImage.sprite = btnSprite;
        btnImage.color = new Color(1f, 1f, 1f, 0f);

        RectTransform btnRect = btnImage.rectTransform;
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        float btnScale = 5f;
        btnRect.sizeDelta = new Vector2(btnSprite.rect.width * btnScale, btnSprite.rect.height * btnScale);
        btnRect.anchoredPosition = new Vector2(0f, -80f);

        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        button.transition = UnityEngine.UI.Button.Transition.ColorTint;
        button.targetGraphic = btnImage;

        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.82f, 0f); // gold-yellow highlight
        colors.pressedColor = Color.gray;
        colors.selectedColor = Color.white;
        button.colors = colors;

        button.onClick.AddListener(OnPlayAgainClicked);

        // Start fading
        StartCoroutine(FadeEndingUI(bgImage, textImage, btnImage));
    }

    private void OnPlayAgainClicked()
    {
        // Destroy the recruited companion so it does not carry over to Level 1
        AllyController ally = FindFirstObjectByType<AllyController>();
        if (ally != null)
        {
            Destroy(ally.gameObject);
        }

        // Destroy the canvas before scene load
        if (canvasObj != null)
        {
            Destroy(canvasObj);
        }

        // Load Level1 scene
        SceneManager.LoadScene("Level1");
    }

    private IEnumerator FadeEndingUI(UnityEngine.UI.Image bgImage, UnityEngine.UI.Image textImage, UnityEngine.UI.Image btnImage)
    {
        float duration = 2.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (bgImage != null)
            {
                bgImage.color = new Color(0f, 0f, 0f, t * 0.85f);
            }

            if (textImage != null)
            {
                textImage.color = new Color(1f, 1f, 1f, t);
            }

            if (btnImage != null)
            {
                btnImage.color = new Color(1f, 1f, 1f, t);
            }

            yield return null;
        }

        if (bgImage != null) bgImage.color = new Color(0f, 0f, 0f, 0.85f);
        if (textImage != null) textImage.color = new Color(1f, 1f, 1f, 1f);
        if (btnImage != null) btnImage.color = new Color(1f, 1f, 1f, 1f);
    }

    private Sprite GeneratePixelTextSprite(string text, Color textColor, Color outlineColor)
    {
        int charWidth = 5;
        int charHeight = 5;
        int spacing = 1;
        int border = 4;

        int textLength = text.Length;
        int contentWidth = textLength * charWidth + (textLength - 1) * spacing;
        int width = contentWidth + border * 2 + 2;
        int height = charHeight + border * 2 + 2;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, transparent);
            }
        }

        // Draw outline (8-directional black outline)
        int[] dx = { -1, 1, 0, 0, -1, 1, -1, 1 };
        int[] dy = { 0, 0, -1, 1, -1, -1, 1, 1 };

        for (int i = 0; i < textLength; i++)
        {
            int startX = border + i * (charWidth + spacing);
            int startY = border;
            for (int d = 0; d < dx.Length; d++)
            {
                DrawCharacter(texture, text[i], startX + dx[d], startY + dy[d], outlineColor);
            }
        }

        // Draw main text
        for (int i = 0; i < textLength; i++)
        {
            int startX = border + i * (charWidth + spacing);
            int startY = border;
            DrawCharacter(texture, text[i], startX, startY, textColor);
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
    }

    private void DrawCharacter(Texture2D tex, char c, int startX, int startY, Color color)
    {
        int[] pattern = GetPattern(c);
        for (int row = 0; row < 5; row++)
        {
            int rowData = pattern[row];
            int pixelY = startY + (4 - row);
            for (int col = 0; col < 5; col++)
            {
                int bitIndex = 4 - col;
                bool isSet = ((rowData >> bitIndex) & 1) == 1;
                if (isSet)
                {
                    tex.SetPixel(startX + col, pixelY, color);
                }
            }
        }
    }

    private int[] GetPattern(char c)
    {
        c = char.ToUpper(c);
        switch (c)
        {
            case 'A': return new int[] { 14, 17, 31, 17, 17 };
            case 'B': return new int[] { 30, 17, 30, 17, 30 };
            case 'C': return new int[] { 15, 16, 16, 16, 15 };
            case 'D': return new int[] { 30, 17, 17, 17, 30 };
            case 'E': return new int[] { 31, 16, 28, 16, 31 };
            case 'F': return new int[] { 31, 16, 28, 16, 16 };
            case 'G': return new int[] { 15, 16, 19, 17, 15 };
            case 'H': return new int[] { 17, 17, 31, 17, 17 };
            case 'I': return new int[] { 14, 4, 4, 4, 14 };
            case 'J': return new int[] { 7, 2, 2, 18, 12 };
            case 'K': return new int[] { 17, 18, 28, 18, 17 };
            case 'L': return new int[] { 16, 16, 16, 16, 31 };
            case 'M': return new int[] { 17, 27, 21, 17, 17 };
            case 'N': return new int[] { 17, 25, 21, 19, 17 };
            case 'O': return new int[] { 14, 17, 17, 17, 14 };
            case 'P': return new int[] { 30, 17, 30, 16, 16 };
            case 'Q': return new int[] { 14, 17, 21, 18, 13 };
            case 'R': return new int[] { 30, 17, 30, 18, 17 };
            case 'S': return new int[] { 15, 16, 14, 1, 30 };
            case 'T': return new int[] { 31, 4, 4, 4, 4 };
            case 'U': return new int[] { 17, 17, 17, 17, 14 };
            case 'V': return new int[] { 17, 17, 17, 10, 4 };
            case 'W': return new int[] { 17, 17, 21, 27, 17 };
            case 'X': return new int[] { 17, 10, 4, 10, 17 };
            case 'Y': return new int[] { 17, 10, 4, 4, 4 };
            case 'Z': return new int[] { 31, 2, 4, 8, 31 };
            case '1': return new int[] { 4, 12, 4, 4, 14 };
            case '2': return new int[] { 30, 1, 6, 8, 31 };
            case '3': return new int[] { 30, 1, 14, 1, 30 };
            case '4': return new int[] { 17, 17, 31, 1, 1 };
            case '5': return new int[] { 31, 16, 30, 1, 30 };
            case '6': return new int[] { 30, 16, 30, 17, 30 };
            case '7': return new int[] { 31, 1, 2, 4, 8 };
            case '8': return new int[] { 30, 17, 30, 17, 30 };
            case '9': return new int[] { 30, 17, 30, 1, 30 };
            case '0': return new int[] { 14, 17, 17, 17, 14 };
            case '-': return new int[] { 0, 0, 14, 0, 0 };
            case '/': return new int[] { 1, 2, 4, 8, 16 };
            case '!': return new int[] { 4, 4, 4, 0, 4 };
            case ',': return new int[] { 0, 0, 4, 4, 8 };
            case '\'': return new int[] { 8, 8, 0, 0, 0 };
            case '.': return new int[] { 0, 0, 0, 0, 4 };
            case ':': return new int[] { 0, 4, 0, 4, 0 };
            case ' ':
            default: return new int[] { 0, 0, 0, 0, 0 };
        }
    }
}
