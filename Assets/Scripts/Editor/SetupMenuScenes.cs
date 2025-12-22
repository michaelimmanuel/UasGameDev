using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public sealed class SetupMenuScenes : EditorWindow
{
    // Car-themed color palette
    private static readonly Color RACING_RED = new Color(0.9f, 0.1f, 0.1f, 1f);
    private static readonly Color RACING_ORANGE = new Color(1f, 0.4f, 0f, 1f);
    private static readonly Color DARK_GRAY = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color LIGHT_GRAY = new Color(0.3f, 0.3f, 0.3f, 1f);
    private static readonly Color ACCENT_YELLOW = new Color(1f, 0.85f, 0f, 1f);
    private static readonly Color BACKGROUND_DARK = new Color(0.05f, 0.05f, 0.08f, 1f);

    [MenuItem("Tools/Setup Menu Scenes")]
    public static void ShowWindow()
    {
        GetWindow<SetupMenuScenes>("Setup Menu Scenes");
    }

    private void OnGUI()
    {
        GUILayout.Label("Full Throttle - Menu Scene Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("Car-themed racing menu design", EditorStyles.helpBox);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup All Scenes (Full Throttle Design)", GUILayout.Height(30)))
        {
            SetupStartMenu();
            SetupTrackSelect();
            SetupTrack1Pause();
            SetupTrack2Pause();
            SetupTrack3Pause();
            EditorUtility.DisplayDialog("Complete", "All Full Throttle menus have been set up!", "OK");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Individual Setup:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Setup Start_Menu Scene"))
        {
            SetupStartMenu();
        }
        
        if (GUILayout.Button("Setup Track_Select Scene"))
        {
            SetupTrackSelect();
        }
        
        if (GUILayout.Button("Setup Track 1 Pause Menu"))
        {
            SetupTrack1Pause();
        }
        
        if (GUILayout.Button("Setup Track 2 Pause Menu"))
        {
            SetupTrack2Pause();
        }
        
        if (GUILayout.Button("Setup Track 3 Pause Menu"))
        {
            SetupTrack3Pause();
        }
    }

    private static void SetupStartMenu()
    {
        string scenePath = "Assets/Scenes/Start_Menu.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        GameObject canvas = FindOrCreateCanvas();
        GameObject eventSystem = FindOrCreateEventSystem();
        
        // Create background with gradient effect
        GameObject background = CreatePanel(canvas.transform, "Background");
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = BACKGROUND_DARK;
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Create speed lines effect (decorative)
        for (int i = 0; i < 5; i++)
        {
            GameObject speedLine = new GameObject($"SpeedLine_{i}");
            speedLine.transform.SetParent(background.transform, false);
            RectTransform lineRect = speedLine.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0, 0);
            lineRect.anchorMax = new Vector2(1, 0);
            lineRect.sizeDelta = new Vector2(0, 2);
            lineRect.anchoredPosition = new Vector2(0, 100 + i * 150);
            Image lineImage = speedLine.AddComponent<Image>();
            lineImage.color = new Color(RACING_RED.r, RACING_RED.g, RACING_RED.b, 0.1f);
        }
        
        // Create Main Menu Panel
        GameObject mainPanel = CreatePanel(canvas.transform, "MainPanel");
        Image mainPanelImage = mainPanel.GetComponent<Image>();
        mainPanelImage.color = new Color(0, 0, 0, 0);
        
        VerticalLayoutGroup mainLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = 30;
        mainLayout.childAlignment = TextAnchor.MiddleCenter;
        mainLayout.childControlHeight = false;
        mainLayout.childControlWidth = false;
        mainLayout.childForceExpandHeight = false;
        mainLayout.childForceExpandWidth = false;
        mainLayout.padding = new RectOffset(0, 0, 0, 0);
        
        RectTransform mainPanelRect = mainPanel.GetComponent<RectTransform>();
        mainPanelRect.anchorMin = new Vector2(0, 0);
        mainPanelRect.anchorMax = new Vector2(1, 1);
        mainPanelRect.sizeDelta = Vector2.zero;
        mainPanelRect.anchoredPosition = Vector2.zero;
        
        // Create "Full Throttle" title (scaled down for better fit)
        TextMeshProUGUI titleText = CreateStyledText(mainPanel.transform, "FULL THROTTLE", new Vector2(0, 150), 90, RACING_RED);
        titleText.fontStyle = FontStyles.Bold;
        titleText.enableVertexGradient = true;
        titleText.colorGradient = new VertexGradient(RACING_RED, RACING_ORANGE, RACING_RED, RACING_ORANGE);
        
        // Add subtitle
        TextMeshProUGUI subtitleText = CreateStyledText(mainPanel.transform, "RACING GAME", new Vector2(0, 80), 35, ACCENT_YELLOW);
        subtitleText.fontStyle = FontStyles.Italic;
        
        // Create buttons with car-themed styling (reduced spacing)
        Button startButton = CreateRacingButton(mainPanel.transform, "START", new Vector2(0, -20), RACING_RED);
        Button optionsButton = CreateRacingButton(mainPanel.transform, "OPTIONS", new Vector2(0, -100), DARK_GRAY);
        Button quitButton = CreateRacingButton(mainPanel.transform, "QUIT", new Vector2(0, -180), DARK_GRAY);
        
        // Create Options Panel
        GameObject optionsPanel = CreatePanel(canvas.transform, "OptionsPanel");
        optionsPanel.SetActive(false);
        Image optionsPanelImage = optionsPanel.GetComponent<Image>();
        optionsPanelImage.color = new Color(0, 0, 0, 0.85f);
        
        RectTransform optionsRect = optionsPanel.GetComponent<RectTransform>();
        optionsRect.anchorMin = new Vector2(0, 0);
        optionsRect.anchorMax = new Vector2(1, 1);
        optionsRect.sizeDelta = Vector2.zero;
        optionsRect.anchoredPosition = Vector2.zero;
        
        // Options panel content with styled background
        GameObject optionsContent = new GameObject("OptionsContent");
        optionsContent.transform.SetParent(optionsPanel.transform, false);
        RectTransform optionsContentRect = optionsContent.AddComponent<RectTransform>();
        optionsContentRect.anchorMin = new Vector2(0.5f, 0.5f);
        optionsContentRect.anchorMax = new Vector2(0.5f, 0.5f);
        optionsContentRect.sizeDelta = new Vector2(750, 625);
        optionsContentRect.anchoredPosition = Vector2.zero;
        
        // Options background panel
        GameObject optionsBg = CreatePanel(optionsContent.transform, "OptionsBackground");
        Image optionsBgImage = optionsBg.GetComponent<Image>();
        optionsBgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        RectTransform optionsBgRect = optionsBg.GetComponent<RectTransform>();
        optionsBgRect.anchorMin = new Vector2(0, 0);
        optionsBgRect.anchorMax = new Vector2(1, 1);
        optionsBgRect.sizeDelta = Vector2.zero;
        optionsBgRect.anchoredPosition = Vector2.zero;
        
        // Options title
        TextMeshProUGUI optionsTitle = CreateStyledText(optionsContent.transform, "OPTIONS", new Vector2(0, 120), 70, RACING_RED);
        optionsTitle.fontStyle = FontStyles.Bold;
        
        // Options placeholder text
        TextMeshProUGUI optionsPlaceholder = CreateStyledText(optionsContent.transform, "Settings coming soon...", new Vector2(0, 0), 28, Color.white);
        optionsPlaceholder.fontStyle = FontStyles.Italic;
        
        Button optionsBackButton = CreateRacingButton(optionsContent.transform, "BACK", new Vector2(0, -120), RACING_RED);
        
        // Add controller
        GameObject controller = new GameObject("MainMenuController");
        MainMenuController mainMenuController = controller.AddComponent<MainMenuController>();
        
        // Use reflection to set private fields
        System.Reflection.FieldInfo startField = typeof(MainMenuController).GetField("startButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo optionsField = typeof(MainMenuController).GetField("optionsButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo quitField = typeof(MainMenuController).GetField("quitButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo panelField = typeof(MainMenuController).GetField("optionsPanel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo backField = typeof(MainMenuController).GetField("optionsBackButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        startField?.SetValue(mainMenuController, startButton);
        optionsField?.SetValue(mainMenuController, optionsButton);
        quitField?.SetValue(mainMenuController, quitButton);
        panelField?.SetValue(mainMenuController, optionsPanel);
        backField?.SetValue(mainMenuController, optionsBackButton);
        
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Start_Menu scene set up successfully with Full Throttle design!");
    }

    private static void SetupTrackSelect()
    {
        string scenePath = "Assets/Scenes/Track_Select.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        GameObject canvas = FindOrCreateCanvas();
        GameObject eventSystem = FindOrCreateEventSystem();
        
        // Background
        GameObject background = CreatePanel(canvas.transform, "Background");
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = BACKGROUND_DARK;
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        GameObject panel = CreatePanel(canvas.transform, "TrackSelectPanel");
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0);
        
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 25;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.padding = new RectOffset(0, 0, 0, 0);
        
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        // Title (scaled down)
        TextMeshProUGUI titleText = CreateStyledText(panel.transform, "SELECT TRACK", new Vector2(0, 200), 80, RACING_RED);
        titleText.fontStyle = FontStyles.Bold;
        
        // Subtitle
        TextMeshProUGUI subtitleText = CreateStyledText(panel.transform, "Choose your racing challenge", new Vector2(0, 140), 30, ACCENT_YELLOW);
        subtitleText.fontStyle = FontStyles.Italic;
        
        // Track buttons with different styling (reduced spacing)
        Button track1Button = CreateRacingButton(panel.transform, "TRACK 1", new Vector2(0, 20), RACING_RED);
        Button track2Button = CreateRacingButton(panel.transform, "TRACK 2", new Vector2(0, -70), RACING_ORANGE);
        Button track3Button = CreateRacingButton(panel.transform, "TRACK 3", new Vector2(0, -160), RACING_ORANGE);
        Button backButton = CreateRacingButton(panel.transform, "BACK", new Vector2(0, -250), DARK_GRAY);
        
        GameObject controller = new GameObject("TrackSelectMenuController");
        TrackSelectMenuController trackController = controller.AddComponent<TrackSelectMenuController>();
        
        System.Reflection.FieldInfo track1Field = typeof(TrackSelectMenuController).GetField("track1Button", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo track2Field = typeof(TrackSelectMenuController).GetField("track2Button", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo track3Field = typeof(TrackSelectMenuController).GetField("track3Button", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo backField = typeof(TrackSelectMenuController).GetField("backButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        track1Field?.SetValue(trackController, track1Button);
        track2Field?.SetValue(trackController, track2Button);
        track3Field?.SetValue(trackController, track3Button);
        backField?.SetValue(trackController, backButton);
        
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Track_Select scene set up successfully with Full Throttle design!");
    }

    private static void SetupTrackPause(string scenePath, string trackName)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"Scene not found: {scenePath}");
            return;
        }
        
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // Remove existing pause menu if it exists (to recreate with new design)
        GameObject existingPause = GameObject.Find("PauseMenuController");
        if (existingPause != null)
        {
            Debug.Log($"Removing existing pause menu to recreate with Full Throttle theme...");
            Object.DestroyImmediate(existingPause);
        }
        
        // Remove existing pause panel if it exists
        GameObject existingPanel = GameObject.Find("PausePanel");
        if (existingPanel != null)
        {
            Object.DestroyImmediate(existingPanel);
        }
        
        GameObject canvas = FindOrCreateCanvas();
        GameObject eventSystem = FindOrCreateEventSystem();
        
        // Only configure Canvas Scaler if it doesn't exist - don't modify existing settings
        // to avoid affecting existing UI elements like LapTimeText
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100;
        }
        
        // Create pause panel (initially inactive) with Full Throttle theme
        GameObject pausePanel = CreatePanel(canvas.transform, "PausePanel");
        pausePanel.SetActive(false);
        
        // Make it a full-screen overlay with themed background
        Image panelImage = pausePanel.GetComponent<Image>();
        panelImage.color = new Color(BACKGROUND_DARK.r, BACKGROUND_DARK.g, BACKGROUND_DARK.b, 0.9f);
        
        RectTransform pauseRect = pausePanel.GetComponent<RectTransform>();
        pauseRect.anchorMin = new Vector2(0, 0);
        pauseRect.anchorMax = new Vector2(1, 1);
        pauseRect.sizeDelta = Vector2.zero;
        pauseRect.anchoredPosition = Vector2.zero;
        
        // Create pause menu content with styled background (reduced size)
        GameObject pauseContent = new GameObject("PauseContent");
        pauseContent.transform.SetParent(pausePanel.transform, false);
        RectTransform contentRect = pauseContent.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(625, 562);
        contentRect.anchoredPosition = Vector2.zero;
        
        // Background panel for pause menu (matching options panel style)
        GameObject pauseBg = CreatePanel(pauseContent.transform, "PauseBackground");
        Image pauseBgImage = pauseBg.GetComponent<Image>();
        pauseBgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        RectTransform pauseBgRect = pauseBg.GetComponent<RectTransform>();
        pauseBgRect.anchorMin = new Vector2(0, 0);
        pauseBgRect.anchorMax = new Vector2(1, 1);
        pauseBgRect.sizeDelta = Vector2.zero;
        pauseBgRect.anchoredPosition = Vector2.zero;
        
        VerticalLayoutGroup contentLayout = pauseContent.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 20;
        contentLayout.childAlignment = TextAnchor.MiddleCenter;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = false;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        
        // Simple pause title
        TextMeshProUGUI pauseTitle = CreateStyledText(pauseContent.transform, "PAUSED", new Vector2(0, 60), 60, Color.white);
        pauseTitle.fontStyle = FontStyles.Bold;
        
        // Subtitle
        TextMeshProUGUI subtitle = CreateStyledText(pauseContent.transform, "Press ESC to resume", new Vector2(0, 10), 22, new Color(0.8f, 0.8f, 0.8f));
        subtitle.fontStyle = FontStyles.Italic;
        
        Button resumeButton = CreateRacingButton(pauseContent.transform, "RESUME", new Vector2(0, -40), DARK_GRAY);
        Button exitToMenuButton = CreateRacingButton(pauseContent.transform, "EXIT TO MENU", new Vector2(0, -100), DARK_GRAY);
        Button quitButton = CreateRacingButton(pauseContent.transform, "QUIT", new Vector2(0, -160), DARK_GRAY);
        
        // Add controller
        GameObject controller = new GameObject("PauseMenuController");
        PauseMenuController pauseController = controller.AddComponent<PauseMenuController>();
        
        System.Reflection.FieldInfo panelField = typeof(PauseMenuController).GetField("pausePanel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo resumeField = typeof(PauseMenuController).GetField("resumeButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo exitField = typeof(PauseMenuController).GetField("exitToMenuButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo quitField = typeof(PauseMenuController).GetField("quitButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        panelField?.SetValue(pauseController, pausePanel);
        resumeField?.SetValue(pauseController, resumeButton);
        exitField?.SetValue(pauseController, exitToMenuButton);
        quitField?.SetValue(pauseController, quitButton);
        
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{trackName} pause menu set up successfully with Full Throttle design!");
    }

    private static void SetupTrack1Pause()
    {
        SetupTrackPause("Assets/Scenes/track 1.unity", "Track 1");
    }

    private static void SetupTrack2Pause()
    {
        SetupTrackPause("Assets/Scenes/track 2.unity", "Track 2");
    }

    private static void SetupTrack3Pause()
    {
        SetupTrackPause("Assets/Scenes/track 3.unity", "Track 3");
    }

    private static GameObject FindOrCreateCanvas()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Configure Canvas Scaler for proper screen scaling
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Balance between width and height matching
            scaler.referencePixelsPerUnit = 100;
            
            canvas.AddComponent<GraphicRaycaster>();
        }
        else
        {
            // Don't modify existing Canvas Scaler to preserve existing UI element positioning
            // Only add Canvas Scaler if it doesn't exist
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                scaler.referencePixelsPerUnit = 100;
            }
        }
        return canvas;
    }

    private static GameObject FindOrCreateEventSystem()
    {
        GameObject eventSystem = GameObject.Find("EventSystem");
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return eventSystem;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>();
        return panel;
    }

    private static Button CreateRacingButton(Transform parent, string text, Vector2 position, Color buttonColor)
    {
        GameObject buttonObj = new GameObject(text + "Button");
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(375, 75);
        rect.anchoredPosition = position;
        
        // Button background with gradient effect
        Image image = buttonObj.AddComponent<Image>();
        image.color = buttonColor;
        
        // Add shadow/outline effect
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(buttonObj.transform, false);
        shadowObj.transform.SetAsFirstSibling();
        RectTransform shadowRect = shadowObj.AddComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0, 0);
        shadowRect.anchorMax = new Vector2(1, 1);
        shadowRect.sizeDelta = new Vector2(4, -4);
        shadowRect.anchoredPosition = new Vector2(2, -2);
        Image shadowImage = shadowObj.AddComponent<Image>();
        shadowImage.color = new Color(0, 0, 0, 0.5f);
        
        Button button = buttonObj.AddComponent<Button>();
        
        // Configure button colors
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = new Color(
            Mathf.Min(buttonColor.r * 1.3f, 1f),
            Mathf.Min(buttonColor.g * 1.3f, 1f),
            Mathf.Min(buttonColor.b * 1.3f, 1f),
            1f
        );
        colors.pressedColor = new Color(
            Mathf.Max(buttonColor.r * 0.7f, 0.3f),
            Mathf.Max(buttonColor.g * 0.7f, 0.3f),
            Mathf.Max(buttonColor.b * 0.7f, 0.3f),
            1f
        );
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        
        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 45;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.enableVertexGradient = true;
        textComponent.colorGradient = new VertexGradient(Color.white, Color.white, ACCENT_YELLOW, ACCENT_YELLOW);
        
        button.targetGraphic = image;
        
        return button;
    }

    private static TextMeshProUGUI CreateStyledText(Transform parent, string text, Vector2 position, float fontSize, Color color)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800, fontSize + 20);
        rect.anchoredPosition = position;
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = color;
        
        return textComponent;
    }
}

