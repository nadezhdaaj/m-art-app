using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mart.Editor
{
    public static class RegistrationSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Registration.unity";

        private static TMP_FontAsset headingFont;
        private static TMP_FontAsset bodyFont;
        private static Sprite welcomeBackground;

        private static readonly Color BackgroundColor = new Color(0.95f, 0.92f, 0.88f, 1f);
        private static readonly Color PanelColor = new Color(0.98f, 0.97f, 0.95f, 0.92f);
        private static readonly Color AccentColor = new Color(0.16f, 0.35f, 0.33f, 1f);
        private static readonly Color AccentSoftColor = new Color(0.74f, 0.83f, 0.80f, 1f);
        private static readonly Color TextPrimary = new Color(0.16f, 0.14f, 0.12f, 1f);
        private static readonly Color TextSecondary = new Color(0.37f, 0.34f, 0.30f, 1f);
        private static readonly Color InputBackground = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color BorderMuted = new Color(0.85f, 0.81f, 0.76f, 1f);

        [MenuItem("MART/Rebuild Registration Scene")]
        public static void BuildFromMenu()
        {
            BuildScene();
        }

        public static void BuildFromCommandLine()
        {
            BuildScene();
        }

        private static void BuildScene()
        {
            LoadAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Registration";

            Camera camera = CreateCamera();
            EventSystem eventSystem = CreateEventSystem();
            Canvas canvas = CreateCanvas();
            CreateCanvasBackground(canvas.transform);

            GameObject systemsRoot = CreateRoot("Systems", canvas.transform);
            BackendManager backendManager = systemsRoot.AddComponent<BackendManager>();
            AuthUIManager authUiManager = systemsRoot.AddComponent<AuthUIManager>();

            GameObject screenRoot = CreateRoot("Screen Root", canvas.transform);
            RectTransform screenRootRect = screenRoot.GetComponent<RectTransform>();
            Stretch(screenRootRect, 0f);

            GameObject welcomePanel = CreateScreen("Welcome Screen", screenRoot.transform, true);
            GameObject checkingPanel = CreateScreen("Checking Screen", screenRoot.transform, false);
            GameObject loginPanel = CreateScreen("Login Screen", screenRoot.transform, false);
            GameObject registerPanel = CreateScreen("Register Screen", screenRoot.transform, false);
            GameObject verifyPanel = CreateScreen("Status Screen", screenRoot.transform, false);

            BuildWelcomePanel(welcomePanel.transform, authUiManager, backendManager);
            BuildCheckingPanel(checkingPanel.transform, backendManager);

            TMP_InputField loginEmail;
            TMP_InputField loginPassword;
            TMP_Text loginOutput;
            BuildLoginPanel(loginPanel.transform, backendManager, authUiManager, out loginEmail, out loginPassword, out loginOutput);

            TMP_InputField registerName;
            TMP_InputField registerEmail;
            TMP_InputField registerPassword;
            TMP_InputField registerConfirmPassword;
            TMP_Text registerOutput;
            BuildRegisterPanel(registerPanel.transform, backendManager, authUiManager, out registerName, out registerEmail, out registerPassword, out registerConfirmPassword, out registerOutput);

            TMP_Text verifyText;
            BuildVerifyPanel(verifyPanel.transform, authUiManager, out verifyText);

            BindBackendManager(backendManager, loginEmail, loginPassword, loginOutput, registerName, registerEmail, registerPassword, registerConfirmPassword, registerOutput);
            BindAuthUiManager(authUiManager, checkingPanel, loginPanel, registerPanel, verifyPanel, verifyText, welcomePanel);

            EditorUtility.SetDirty(backendManager);
            EditorUtility.SetDirty(authUiManager);
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(eventSystem);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void LoadAssets()
        {
            headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ElMessiri SDF.asset");
            bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            welcomeBackground = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Welcome Screen (2).png");
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            return camera;
        }

        private static EventSystem CreateEventSystem()
        {
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return eventSystemObject.GetComponent<EventSystem>();
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void CreateCanvasBackground(Transform parent)
        {
            GameObject backdrop = CreateRoot("Backdrop", parent);
            RectTransform rect = backdrop.GetComponent<RectTransform>();
            Stretch(rect, 0f);

            Image backgroundImage = backdrop.AddComponent<Image>();
            backgroundImage.color = new Color(1f, 1f, 1f, 0.92f);
            backgroundImage.sprite = welcomeBackground;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.preserveAspect = false;

            CreateDecorBlob(parent, "Blob Top Left", new Vector2(190f, -140f), new Vector2(360f, 360f), new Color(0.89f, 0.74f, 0.63f, 0.52f));
            CreateDecorBlob(parent, "Blob Right", new Vector2(-110f, 160f), new Vector2(420f, 420f), new Color(0.53f, 0.67f, 0.63f, 0.22f), anchorMin: new Vector2(1f, 0.5f), anchorMax: new Vector2(1f, 0.5f));
            CreateDecorBlob(parent, "Blob Bottom", new Vector2(0f, 60f), new Vector2(680f, 240f), new Color(0.95f, 0.83f, 0.73f, 0.28f), anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(0.5f, 0f));
        }

        private static void CreateDecorBlob(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2? anchorMin = null, Vector2? anchorMax = null)
        {
            GameObject blob = CreateRoot(name, parent);
            RectTransform rect = blob.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin ?? new Vector2(0f, 1f);
            rect.anchorMax = anchorMax ?? new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = blob.AddComponent<Image>();
            image.color = color;
        }

        private static GameObject CreateScreen(string name, Transform parent, bool active)
        {
            GameObject screen = CreateRoot(name, parent);
            RectTransform rect = screen.GetComponent<RectTransform>();
            Stretch(rect, 72f);
            screen.SetActive(active);
            return screen;
        }

        private static void BuildWelcomePanel(Transform parent, AuthUIManager authUiManager, BackendManager backendManager)
        {
            GameObject hero = CreatePanel(parent, "Hero Card", new Vector2(0f, 60f), new Vector2(820f, 1220f));
            VerticalLayoutGroup layout = hero.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(64, 64, 72, 72);
            layout.spacing = 28f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = hero.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateBlockText(hero.transform, "Eyebrow", "M'ART Museum", 24, 42f, headingFont, AccentColor, FontStyles.Normal, TextAlignmentOptions.Center);
            CreateBlockText(hero.transform, "Title", "Вход в музей,\nкоторый хочется исследовать", 48, 140f, headingFont, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, true);
            CreateBlockText(hero.transform, "Subtitle", "Экран авторизации уже работает с backend: регистрация, вход и восстановление сессии по токену.", 24, 96f, bodyFont, TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, true);

            CreatePrimaryButton(hero.transform, "Войти", authUiManager, nameof(AuthUIManager.LoginScreen), 96f);
            CreateSecondaryButton(hero.transform, "Создать аккаунт", authUiManager, nameof(AuthUIManager.RegisterScreen), 88f);
            CreateGhostButton(hero.transform, "Сбросить локальную сессию", backendManager, nameof(BackendManager.ClearSavedSessionAndShowLogin), 68f);
            CreateBlockText(hero.transform, "Footer", "Если автологин мешает тестам, нажмите «Сбросить локальную сессию».", 20, 60f, bodyFont, TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, true);
        }

        private static void BuildCheckingPanel(Transform parent, BackendManager backendManager)
        {
            GameObject card = CreatePanel(parent, "Checking Card", Vector2.zero, new Vector2(760f, 420f));
            CreateAnchoredText(card.transform, "Checking Title", "Проверяем сохранённую сессию", 38, new Vector2(0f, 64f), new Vector2(600f, 80f), headingFont, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
            CreateAnchoredText(card.transform, "Checking Body", "Если токен ещё валиден, вы сразу попадёте в приложение.", 24, new Vector2(0f, 0f), new Vector2(600f, 72f), bodyFont, TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, true);

            GameObject bar = new("Status Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(card.transform, false);
            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 18f);
            rect.anchoredPosition = new Vector2(0f, 56f);
            Image image = bar.GetComponent<Image>();
            image.color = AccentSoftColor;

            GameObject resetButton = CreateAnchoredGhostButton(card.transform, "Сбросить сессию", backendManager, nameof(BackendManager.ClearSavedSessionAndShowLogin), new Vector2(0f, -124f), new Vector2(420f, 62f));
            resetButton.name = "Reset Session Button";
        }

        private static void BuildLoginPanel(Transform parent, BackendManager backendManager, AuthUIManager authUiManager, out TMP_InputField emailInput, out TMP_InputField passwordInput, out TMP_Text outputText)
        {
            GameObject card = CreateFormCard(parent, "Вход");
            CreateBlockText(card.transform, "Login Subtitle", "Используйте почту и пароль от backend-аккаунта.", 22, 58f, bodyFont, TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, true);

            emailInput = CreateInput(card.transform, "Login Email", "Почта");
            passwordInput = CreateInput(card.transform, "Login Password", "Пароль", true);

            outputText = CreateOutputText(card.transform, "Login Output");
            CreatePrimaryButton(card.transform, "Продолжить", backendManager, nameof(BackendManager.LoginButton), 88f);
            CreateSecondaryButton(card.transform, "У меня ещё нет аккаунта", authUiManager, nameof(AuthUIManager.RegisterScreen), 82f);
        }

        private static void BuildRegisterPanel(Transform parent, BackendManager backendManager, AuthUIManager authUiManager, out TMP_InputField nameInput, out TMP_InputField emailInput, out TMP_InputField passwordInput, out TMP_InputField confirmPasswordInput, out TMP_Text outputText)
        {
            GameObject card = CreateFormCard(parent, "Регистрация");
            CreateBlockText(card.transform, "Register Subtitle", "Создайте профиль для прогресса и персонального маршрута по музею.", 22, 74f, bodyFont, TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, true);

            nameInput = CreateInput(card.transform, "Register Name", "Имя");
            emailInput = CreateInput(card.transform, "Register Email", "Почта");
            passwordInput = CreateInput(card.transform, "Register Password", "Пароль", true);
            confirmPasswordInput = CreateInput(card.transform, "Register Password Confirm", "Повторите пароль", true);

            outputText = CreateOutputText(card.transform, "Register Output");
            CreatePrimaryButton(card.transform, "Создать аккаунт", backendManager, nameof(BackendManager.RegisterButton), 88f);
            CreateSecondaryButton(card.transform, "Уже есть аккаунт", authUiManager, nameof(AuthUIManager.LoginScreen), 82f);
        }

        private static void BuildVerifyPanel(Transform parent, AuthUIManager authUiManager, out TMP_Text verifyText)
        {
            GameObject card = CreatePanel(parent, "Status Card", Vector2.zero, new Vector2(760f, 600f));
            CreateAnchoredText(card.transform, "Status Title", "Статус авторизации", 38, new Vector2(0f, 86f), new Vector2(560f, 84f), headingFont, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
            verifyText = CreateAnchoredText(card.transform, "Status Message", "Здесь выводятся служебные сообщения сценария авторизации.", 24, new Vector2(0f, -6f), new Vector2(560f, 220f), bodyFont, TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, true);
            CreateSecondaryButton(card.transform, "Вернуться ко входу", authUiManager, nameof(AuthUIManager.LoginScreen), 82f);
        }

        private static GameObject CreateFormCard(Transform parent, string title)
        {
            GameObject card = CreatePanel(parent, title + " Card", Vector2.zero, new Vector2(820f, 1160f));
            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(64, 64, 64, 64);
            layout.spacing = 22f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = card.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateBlockText(card.transform, title + " Title", title, 40, 64f, headingFont, TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
            return card;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject panel = CreateRoot(name, parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = panel.AddComponent<Image>();
            image.color = PanelColor;

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.38f, 0.31f, 0.18f);
            outline.effectDistance = new Vector2(2f, -2f);

            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
            shadow.effectDistance = new Vector2(0f, -14f);
            shadow.useGraphicAlpha = true;

            return panel;
        }

        private static GameObject CreateRoot(string name, Transform parent)
        {
            GameObject root = new(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root;
        }

        private static TMP_Text CreateBlockText(Transform parent, string name, string text, float size, float preferredHeight, TMP_FontAsset font, Color color, FontStyles style, TextAlignmentOptions alignment, bool wordWrap = false)
        {
            GameObject textObject = new(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            TMP_Text tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = wordWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(16f, size * 0.55f);
            tmp.fontSizeMax = size;
            tmp.lineSpacing = -8f;

            RectTransform rect = tmp.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, preferredHeight);

            LayoutElement element = textObject.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = 1f;

            return tmp;
        }

        private static TMP_Text CreateAnchoredText(Transform parent, string name, string text, float size, Vector2 anchoredPosition, Vector2 sizeDelta, TMP_FontAsset font, Color color, FontStyles style, TextAlignmentOptions alignment, bool wordWrap = false)
        {
            GameObject textObject = new(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            TMP_Text tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = wordWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(16f, size * 0.6f);
            tmp.fontSizeMax = size;
            tmp.lineSpacing = -6f;

            RectTransform rect = tmp.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return tmp;
        }

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholder, bool isPassword = false)
        {
            GameObject field = new(name, typeof(RectTransform), typeof(Image));
            field.transform.SetParent(parent, false);

            Image fieldImage = field.GetComponent<Image>();
            fieldImage.color = InputBackground;

            LayoutElement layout = field.AddComponent<LayoutElement>();
            layout.preferredHeight = 112f;

            TMP_InputField input = field.AddComponent<TMP_InputField>();
            input.transition = Selectable.Transition.ColorTint;
            input.colors = DefaultSelectableColors();
            input.caretColor = AccentColor;
            input.selectionColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.25f);
            input.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;

            GameObject textArea = new("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(field.transform, false);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            Stretch(textAreaRect, 24f);

            TMP_Text placeholderText = CreateInnerText(textArea.transform, "Placeholder", placeholder, 30f, new Color(0.48f, 0.45f, 0.42f, 0.82f));
            TMP_Text inputText = CreateInnerText(textArea.transform, "Text", string.Empty, 30f, TextPrimary);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            placeholderText.fontStyle = FontStyles.Italic;

            input.textViewport = textAreaRect;
            input.textComponent = inputText as TextMeshProUGUI;
            input.placeholder = placeholderText;

            return input;
        }

        private static TMP_Text CreateInnerText(Transform parent, string name, string text, float size, Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            Stretch(rect, 0f);

            TMP_Text tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.font = bodyFont != null ? bodyFont : TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18f;
            tmp.fontSizeMax = size;
            return tmp;
        }

        private static TMP_Text CreateOutputText(Transform parent, string name)
        {
            TMP_Text text = CreateBlockText(parent, name, string.Empty, 28f, 64f, bodyFont, new Color(0.58f, 0.18f, 0.18f, 1f), FontStyles.Normal, TextAlignmentOptions.Center, true);
            text.text = string.Empty;
            return text;
        }

        private static GameObject CreatePrimaryButton(Transform parent, string label, Object target, string methodName, float height)
        {
            return CreateButton(parent, label, target, methodName, height, AccentColor, Color.white);
        }

        private static GameObject CreateSecondaryButton(Transform parent, string label, Object target, string methodName, float height)
        {
            return CreateButton(parent, label, target, methodName, height, new Color(1f, 1f, 1f, 0.86f), AccentColor, true);
        }

        private static GameObject CreateGhostButton(Transform parent, string label, Object target, string methodName, float height)
        {
            return CreateButton(parent, label, target, methodName, height, new Color(0f, 0f, 0f, 0f), AccentColor, false);
        }

        private static GameObject CreateButton(Transform parent, string label, Object target, string methodName, float height, Color backgroundColor, Color textColor, bool outlined = false)
        {
            GameObject buttonObject = new(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;

            Image image = buttonObject.GetComponent<Image>();
            image.color = backgroundColor;

            if (outlined)
            {
                Outline outline = buttonObject.AddComponent<Outline>();
                outline.effectColor = BorderMuted;
                outline.effectDistance = new Vector2(1f, -1f);
            }

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.colors = DefaultSelectableColors();
            BindButtonAction(button, target, methodName);

            CreateButtonLabel(buttonObject.transform, label, textColor);
            return buttonObject;
        }

        private static void BindButtonAction(Button button, Object target, string methodName)
        {
            if (target is AuthUIManager authUiManager)
            {
                if (methodName == nameof(AuthUIManager.LoginScreen))
                {
                    UnityEventTools.AddPersistentListener(button.onClick, authUiManager.LoginScreen);
                    return;
                }

                if (methodName == nameof(AuthUIManager.RegisterScreen))
                {
                    UnityEventTools.AddPersistentListener(button.onClick, authUiManager.RegisterScreen);
                    return;
                }
            }

            if (target is BackendManager backendManager)
            {
                if (methodName == nameof(BackendManager.LoginButton))
                {
                    UnityEventTools.AddPersistentListener(button.onClick, backendManager.LoginButton);
                    return;
                }

                if (methodName == nameof(BackendManager.RegisterButton))
                {
                    UnityEventTools.AddPersistentListener(button.onClick, backendManager.RegisterButton);
                    return;
                }
            }

            throw new System.InvalidOperationException($"Unsupported button binding: {target?.GetType().Name}.{methodName}");
        }

        private static void CreateButtonLabel(Transform parent, string label, Color textColor)
        {
            TMP_Text text = CreateAnchoredText(parent, "Label", label, 34f, Vector2.zero, new Vector2(0f, 64f), headingFont, textColor, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform rect = text.rectTransform;
            Stretch(rect, 0f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 34f;
            text.lineSpacing = -4f;
        }

        private static GameObject CreateAnchoredGhostButton(Transform parent, string label, Object target, string methodName, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject button = CreateButton(parent, label, target, methodName, sizeDelta.y, new Color(1f, 1f, 1f, 0.88f), AccentColor, true);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            LayoutElement layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                Object.DestroyImmediate(layoutElement);
            }

            return button;
        }

        private static void BindBackendManager(BackendManager manager, TMP_InputField loginEmail, TMP_InputField loginPassword, TMP_Text loginOutput, TMP_InputField registerName, TMP_InputField registerEmail, TMP_InputField registerPassword, TMP_InputField registerConfirmPassword, TMP_Text registerOutput)
        {
            SerializedObject serializedObject = new(manager);
            serializedObject.FindProperty("loginEmail").objectReferenceValue = loginEmail;
            serializedObject.FindProperty("loginPassword").objectReferenceValue = loginPassword;
            serializedObject.FindProperty("loginOutputText").objectReferenceValue = loginOutput;
            serializedObject.FindProperty("registerUsername").objectReferenceValue = registerName;
            serializedObject.FindProperty("registerEmail").objectReferenceValue = registerEmail;
            serializedObject.FindProperty("registerPassword").objectReferenceValue = registerPassword;
            serializedObject.FindProperty("registerConfimPassword").objectReferenceValue = registerConfirmPassword;
            serializedObject.FindProperty("registerOutputText").objectReferenceValue = registerOutput;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindAuthUiManager(AuthUIManager manager, GameObject checkingPanel, GameObject loginPanel, GameObject registerPanel, GameObject verifyPanel, TMP_Text verifyText, GameObject welcomePanel)
        {
            SerializedObject serializedObject = new(manager);
            serializedObject.FindProperty("checkigForAccountUI").objectReferenceValue = checkingPanel;
            serializedObject.FindProperty("loginUI").objectReferenceValue = loginPanel;
            serializedObject.FindProperty("registerUI").objectReferenceValue = registerPanel;
            serializedObject.FindProperty("verifyEmailUI").objectReferenceValue = verifyPanel;
            serializedObject.FindProperty("verifyEmailText").objectReferenceValue = verifyText;
            serializedObject.FindProperty("welcomeUI").objectReferenceValue = welcomePanel;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ColorBlock DefaultSelectableColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            return colors;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
