using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartMenu : MonoBehaviour
{
    private GameObject mainMenuPanel;
    private GameObject controlsPanel;

    private void Start()
    {
        CreateMenuUI();
    }

    private void CreateMenuUI()
    {
        // Ensure EventSystem is present in the scene for UI interactions
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

#if UNITY_INPUT_SYSTEM || ENABLE_INPUT_SYSTEM
            // Under the new Input System, we must use InputSystemUIInputModule instead of StandaloneInputModule
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        // 1. Create Canvas
        GameObject canvasObj = new GameObject("StartMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Add a nice dark background overlay (semi-transparent so it acts as an overlay)
        GameObject bgObj = new GameObject("MenuBackground");
        bgObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.08f, 0.6f); // Semi-transparent dark overlay
        RectTransform bgRect = bgImage.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 2. Create Main Menu Panel
        mainMenuPanel = new GameObject("MainMenuPanel");
        mainMenuPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform mainRect = mainMenuPanel.AddComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;

        // 2a. Title Image ("PUSA NA SI PAOLA")
        GameObject titleObj = new GameObject("TitleImage");
        titleObj.transform.SetParent(mainMenuPanel.transform, false);
        UnityEngine.UI.Image titleImage = titleObj.AddComponent<UnityEngine.UI.Image>();
        Sprite titleSprite = GeneratePixelTextSprite("Pusa na si Paola", new Color(1f, 0.82f, 0f), Color.black);
        titleImage.sprite = titleSprite;
        RectTransform titleRect = titleImage.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        float titleScale = 10f; // Large title
        titleRect.sizeDelta = new Vector2(titleSprite.rect.width * titleScale, titleSprite.rect.height * titleScale);
        titleRect.anchoredPosition = Vector2.zero;

        // 2b. Start Button
        GameObject startBtnObj = new GameObject("StartButton");
        startBtnObj.transform.SetParent(mainMenuPanel.transform, false);
        UnityEngine.UI.Image startImage = startBtnObj.AddComponent<UnityEngine.UI.Image>();
        Sprite startSprite = GeneratePixelTextSprite("Start", Color.white, Color.black);
        startImage.sprite = startSprite;
        RectTransform startRect = startImage.rectTransform;
        startRect.anchorMin = new Vector2(0.5f, 0.45f);
        startRect.anchorMax = new Vector2(0.5f, 0.45f);
        startRect.pivot = new Vector2(0.5f, 0.5f);
        float btnScale = 7f;
        startRect.sizeDelta = new Vector2(startSprite.rect.width * btnScale, startSprite.rect.height * btnScale);
        startRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Button startBtn = startBtnObj.AddComponent<UnityEngine.UI.Button>();
        ConfigureButtonTransition(startBtn, startImage);
        startBtn.onClick.AddListener(OnStartClicked);

        // 2c. Controls Button
        GameObject ctrlBtnObj = new GameObject("ControlsButton");
        ctrlBtnObj.transform.SetParent(mainMenuPanel.transform, false);
        UnityEngine.UI.Image ctrlImage = ctrlBtnObj.AddComponent<UnityEngine.UI.Image>();
        Sprite ctrlSprite = GeneratePixelTextSprite("Controls", Color.white, Color.black);
        ctrlImage.sprite = ctrlSprite;
        RectTransform ctrlRect = ctrlImage.rectTransform;
        ctrlRect.anchorMin = new Vector2(0.5f, 0.3f);
        ctrlRect.anchorMax = new Vector2(0.5f, 0.3f);
        ctrlRect.pivot = new Vector2(0.5f, 0.5f);
        ctrlRect.sizeDelta = new Vector2(ctrlSprite.rect.width * btnScale, ctrlSprite.rect.height * btnScale);
        ctrlRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Button ctrlBtn = ctrlBtnObj.AddComponent<UnityEngine.UI.Button>();
        ConfigureButtonTransition(ctrlBtn, ctrlImage);
        ctrlBtn.onClick.AddListener(OnControlsClicked);

        // 3. Create Controls Panel (initially inactive)
        controlsPanel = new GameObject("ControlsPanel");
        controlsPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform ctrlPanelRect = controlsPanel.AddComponent<RectTransform>();
        ctrlPanelRect.anchorMin = Vector2.zero;
        ctrlPanelRect.anchorMax = Vector2.one;
        ctrlPanelRect.offsetMin = Vector2.zero;
        ctrlPanelRect.offsetMax = Vector2.zero;
        controlsPanel.SetActive(false);

        // 3a. Controls Title ("Controls")
        GameObject ctrlTitleObj = new GameObject("ControlsTitle");
        ctrlTitleObj.transform.SetParent(controlsPanel.transform, false);
        UnityEngine.UI.Image ctrlTitleImage = ctrlTitleObj.AddComponent<UnityEngine.UI.Image>();
        Sprite ctrlTitleSprite = GeneratePixelTextSprite("Controls", new Color(1f, 0.82f, 0f), Color.black);
        ctrlTitleImage.sprite = ctrlTitleSprite;
        RectTransform ctrlTitleRect = ctrlTitleImage.rectTransform;
        ctrlTitleRect.anchorMin = new Vector2(0.5f, 0.75f);
        ctrlTitleRect.anchorMax = new Vector2(0.5f, 0.75f);
        ctrlTitleRect.pivot = new Vector2(0.5f, 0.5f);
        ctrlTitleRect.sizeDelta = new Vector2(ctrlTitleSprite.rect.width * 8f, ctrlTitleSprite.rect.height * 8f);
        ctrlTitleRect.anchoredPosition = Vector2.zero;

        // 3b. Control entries (Keys and their action explanations)
        CreateControlEntry("A - Move Left", 0.62f);
        CreateControlEntry("D - Move Right", 0.52f);
        CreateControlEntry("W - Jump", 0.42f);
        CreateControlEntry("Space - Attack", 0.32f);

        // 3c. Back Button
        GameObject backBtnObj = new GameObject("BackButton");
        backBtnObj.transform.SetParent(controlsPanel.transform, false);
        UnityEngine.UI.Image backImage = backBtnObj.AddComponent<UnityEngine.UI.Image>();
        Sprite backSprite = GeneratePixelTextSprite("Back", Color.white, Color.black);
        backImage.sprite = backSprite;
        RectTransform backRect = backImage.rectTransform;
        backRect.anchorMin = new Vector2(0.5f, 0.18f);
        backRect.anchorMax = new Vector2(0.5f, 0.18f);
        backRect.pivot = new Vector2(0.5f, 0.5f);
        backRect.sizeDelta = new Vector2(backSprite.rect.width * btnScale, backSprite.rect.height * btnScale);
        backRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Button backBtn = backBtnObj.AddComponent<UnityEngine.UI.Button>();
        ConfigureButtonTransition(backBtn, backImage);
        backBtn.onClick.AddListener(OnBackClicked);
    }

    private void CreateControlEntry(string text, float verticalAnchor)
    {
        GameObject entryObj = new GameObject("ControlEntry_" + text.Replace(" ", ""));
        entryObj.transform.SetParent(controlsPanel.transform, false);
        UnityEngine.UI.Image entryImage = entryObj.AddComponent<UnityEngine.UI.Image>();
        Sprite entrySprite = GeneratePixelTextSprite(text, Color.white, Color.black);
        entryImage.sprite = entrySprite;
        RectTransform entryRect = entryImage.rectTransform;
        entryRect.anchorMin = new Vector2(0.5f, verticalAnchor);
        entryRect.anchorMax = new Vector2(0.5f, verticalAnchor);
        entryRect.pivot = new Vector2(0.5f, 0.5f);
        float scale = 6f;
        entryRect.sizeDelta = new Vector2(entrySprite.rect.width * scale, entrySprite.rect.height * scale);
        entryRect.anchoredPosition = Vector2.zero;
    }

    private void ConfigureButtonTransition(UnityEngine.UI.Button button, UnityEngine.UI.Image image)
    {
        button.transition = UnityEngine.UI.Button.Transition.ColorTint;
        button.targetGraphic = image;

        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.82f, 0f); // Gold/yellow highlight
        colors.pressedColor = Color.gray;
        colors.selectedColor = Color.white;
        button.colors = colors;
    }

    private void OnStartClicked()
    {
        // Load Level1 scene
        SceneManager.LoadScene("Level1");
    }

    private void OnControlsClicked()
    {
        mainMenuPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    private void OnBackClicked()
    {
        controlsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
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
            case '-': return new int[] { 0, 0, 14, 0, 0 };
            case '/': return new int[] { 1, 2, 4, 8, 16 };
            case ' ':
            default: return new int[] { 0, 0, 0, 0, 0 };
        }
    }
}
