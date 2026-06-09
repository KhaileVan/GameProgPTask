using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelIntroOverlay : MonoBehaviour
{
    private PlayerController player;
    private AllyController ally;
    private Rigidbody2D playerRb;
    private Rigidbody2D allyRb;
    private GameObject canvasObj;

    private string[] level1Text = new string[]
    {
        "HELP PAOLA THE CAT ON HER JOURNEY TO RESCUE KHAILE!",
        "",
        "SUM BIRDS, RATS, OR DOGS MAY BOTHER YOU!",
        "PRESS SPACE TO ATTACK THEM.",
        "",
        "RESCUE KHAILE, HE MIGHT BE STUCK ON A BRIDGE AGAIN,",
        "HE WILL HELP YOU FIGHT AND FIND FOOD ALONG THE WAY",
        "",
        "YOU'RE SCARED OF WATER BTW!"
    };

    private string[] level2Text = new string[]
    {
        "FALLING PLATFORMS! WATCH OUT!",
        "BEWARE FROM AGGRESSIVE ANIMALS ALONG THE WAY,",
        "KHAILE WILL BE WITH YOU UNTIL YOU BOTH RETURN HOME.",
        "",
        "GOOD LUCK!"
    };

    private void Start()
    {
        // Find player and ally references
        player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            // Freeze controls
            player.enabled = false;
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.bodyType = RigidbodyType2D.Kinematic; // temporarily kinematic so they don't fall/move
            }
        }

        ally = FindFirstObjectByType<AllyController>();
        if (ally != null)
        {
            allyRb = ally.GetComponent<Rigidbody2D>();
            ally.enabled = false;
            if (allyRb != null)
            {
                allyRb.linearVelocity = Vector2.zero;
                allyRb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        CreateIntroUI();
    }

    private void CreateIntroUI()
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
        canvasObj = new GameObject("IntroOverlayCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 2. Add Background Overlay (translucent dark panel)
        GameObject bgObj = new GameObject("IntroBackground");
        bgObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.08f, 0.75f); // 75% dark tint overlay
        RectTransform bgRect = bgImage.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 3. Determine text lines and header based on scene name
        string sceneName = SceneManager.GetActiveScene().name;
        string[] textLines = level1Text; // default fallback
        string headerText = "LEVEL 1";
        if (sceneName.ToLower().Contains("level2"))
        {
            textLines = level2Text;
            headerText = "LEVEL 2";
        }

        // Add prominent Header at the top
        GameObject headerObj = new GameObject("IntroHeader");
        headerObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image headerImage = headerObj.AddComponent<UnityEngine.UI.Image>();
        Sprite headerSprite = GeneratePixelTextSprite(headerText, new Color(1f, 0.82f, 0f), Color.black); // gold-yellow header with black outline
        headerImage.sprite = headerSprite;

        RectTransform headerRect = headerImage.rectTransform;
        headerRect.anchorMin = new Vector2(0.5f, 0.5f);
        headerRect.anchorMax = new Vector2(0.5f, 0.5f);
        headerRect.pivot = new Vector2(0.5f, 0.5f);
        float headerScale = 6f; // Large size for header
        headerRect.sizeDelta = new Vector2(headerSprite.rect.width * headerScale, headerSprite.rect.height * headerScale);
        headerRect.anchoredPosition = new Vector2(0f, 215f);

        // 4. Spawn Text Lines
        float startY = 125f;
        float lineSpacing = 30f;
        float scale = 4f; // Clean readable scale

        for (int i = 0; i < textLines.Length; i++)
        {
            string lineText = textLines[i];
            if (string.IsNullOrEmpty(lineText)) continue;

            GameObject lineObj = new GameObject("IntroLine_" + i);
            lineObj.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image lineImage = lineObj.AddComponent<UnityEngine.UI.Image>();
            
            Sprite lineSprite = GeneratePixelTextSprite(lineText, Color.white, Color.black);
            lineImage.sprite = lineSprite;

            RectTransform lineRect = lineImage.rectTransform;
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(lineSprite.rect.width * scale, lineSprite.rect.height * scale);
            lineRect.anchoredPosition = new Vector2(0f, startY - (i * lineSpacing));
        }

        // 5. Add "Noted!" Button
        GameObject buttonObj = new GameObject("NotedButton");
        buttonObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image btnImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        Sprite btnSprite = GeneratePixelTextSprite("Noted!", Color.white, Color.black);
        btnImage.sprite = btnSprite;

        RectTransform btnRect = btnImage.rectTransform;
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        float btnScale = 5.5f;
        btnRect.sizeDelta = new Vector2(btnSprite.rect.width * btnScale, btnSprite.rect.height * btnScale);
        btnRect.anchoredPosition = new Vector2(0f, -180f);

        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        button.transition = UnityEngine.UI.Button.Transition.ColorTint;
        button.targetGraphic = btnImage;

        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.82f, 0f); // gold-yellow highlight
        colors.pressedColor = Color.gray;
        colors.selectedColor = Color.white;
        button.colors = colors;

        button.onClick.AddListener(OnNotedClicked);
    }

    private void OnNotedClicked()
    {
        // Restore player physics & controls
        if (player != null)
        {
            player.enabled = true;
            if (playerRb != null)
            {
                playerRb.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        // Restore ally physics & controls
        if (ally != null)
        {
            ally.enabled = true;
            if (allyRb != null)
            {
                allyRb.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        // Destroy overlay canvas & self
        if (canvasObj != null)
        {
            Destroy(canvasObj);
        }
        Destroy(gameObject);
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
