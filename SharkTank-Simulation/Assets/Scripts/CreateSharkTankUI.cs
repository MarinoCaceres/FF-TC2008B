using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public static class CreateSharkTankUI
{
    private const string CanvasName = "SharkTankUI";
    private static readonly Color Glass = new Color(20f / 255f, 22f / 255f, 26f / 255f, 0.85f);
    private static readonly Color Outline = new Color(0.78f, 0.80f, 0.82f, 0.78f);
    private static readonly Color Text = new Color(0.96f, 0.97f, 0.98f, 1f);
    private static readonly Color Muted = new Color(0.78f, 0.81f, 0.84f, 1f);

    [MenuItem("Tools/Generate Shark Tank UI")]
    public static void Generate()
    {
        EnsureEventSystem();

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Undo.RecordObject(mainCamera, "Set Shark Tank background");
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(140f / 255f, 140f / 255f, 140f / 255f, 1f);
        }

        GameObject previous = GameObject.Find(CanvasName);
        if (previous != null)
            Undo.DestroyObjectImmediate(previous);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Generate Shark Tank UI");

        GameObject canvasObject = new GameObject(CanvasName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Shark Tank UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CreateTopBar(canvasObject.transform);
        CreateAgentArea(canvasObject.transform);
        CreateEntrepreneurConsole(canvasObject.transform);
        CreateProposalPanel(canvasObject.transform);
        CreateHistoryPanel(canvasObject.transform);

        Selection.activeGameObject = canvasObject;
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        EditorUtility.DisplayDialog("Shark Tank UI",
            "La interfaz de Shark Tank se genero correctamente.", "Aceptar");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem",
            typeof(RectTransform), typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    private static void CreateTopBar(Transform parent)
    {
        RectTransform bar = AddPanel(parent, "TopBar",
            new Vector2(0.008f, 0.918f), new Vector2(0.992f, 0.985f));

        HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.padding = new RectOffset(18, 18, 8, 8);
        layout.spacing = 14f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TMP_Text title = CreateText(bar, "TituloPrincipal",
            "Simulador Shark Tank", 22, TextAlignmentOptions.MidlineLeft);
        SetLayout(title.rectTransform, 300f, 54f);

        TMP_Text proposal = CreateText(bar, "PropuestaActiva",
            "Propuesta: EcoLogix | $500,000 MXN por 15%", 16,
            TextAlignmentOptions.MidlineLeft);
        SetLayout(proposal.rectTransform, 520f, 54f);
        proposal.fontStyle = FontStyles.Bold;

        GameObject spacer = CreateObject("EspaciadorSuperior", bar);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

        CreateButton(bar, "VerHistorial", "Ver Historial", 190f);
        CreateButton(bar, "ReiniciarSesion", "Reiniciar Sesión", 210f);
    }

    private static void CreateAgentArea(Transform parent)
    {
        GameObject area = AddPanel(parent, "AgentsDialogueArea",
            new Vector2(0.02f, 0.20f), new Vector2(0.98f, 0.49f)).gameObject;
        area.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        Object.DestroyImmediate(area.GetComponent<Outline>());

        HorizontalLayoutGroup layout = area.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreateJudgeCard(area.transform,
            "TarjetaTiburon1",
            "Tiburón 1 | Tecnología e Innovación",
            "Interés: Alto",
            "¿Qué tan difícil es replicar su tecnología y cómo planean escalar la infraestructura?");

        CreateJudgeCard(area.transform,
            "TarjetaTiburon2",
            "Tiburón 2 | Ventas y Mercado",
            "Evaluando",
            "¿Cuál es su costo de adquisición de clientes y qué tracción real tienen este trimestre?");

        CreateJudgeCard(area.transform,
            "TarjetaTiburon3",
            "Tiburón 3 | Finanzas y Rentabilidad",
            "Oferta: $500k por 22%",
            "Tu valuación es alta para los márgenes actuales, pero te ofrezco los $500,000 por el 22%.");
    }

    private static void CreateJudgeCard(Transform parent, string name,
        string header, string status, string dialogue)
    {
        RectTransform card = AddPanel(parent, name,
            Vector2.zero, Vector2.one);

        RectTransform headerRow = CreateObject("Encabezado", card).GetComponent<RectTransform>();
        StretchTop(headerRow, 58f);

        TMP_Text headerText = CreateText(headerRow, "NombreEspecialidad",
            header, 17, TextAlignmentOptions.MidlineLeft);
        SetAnchors(headerText.rectTransform, 0f, 0f, 0.68f, 1f);
        headerText.rectTransform.offsetMin = new Vector2(16f, 8f);
        headerText.rectTransform.offsetMax = new Vector2(-8f, -8f);
        headerText.fontStyle = FontStyles.Bold;

        RectTransform badgeBackground = CreateObject("Estado", headerRow).GetComponent<RectTransform>();
        SetAnchors(badgeBackground, 0.68f, 0.12f, 1f, 0.88f);
        badgeBackground.offsetMin = new Vector2(4f, 4f);
        badgeBackground.offsetMax = new Vector2(-12f, -4f);
        badgeBackground.gameObject.AddComponent<Image>().color =
            new Color(66f / 255f, 69f / 255f, 76f / 255f, 0.96f);
        Outline badgeOutline = badgeBackground.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = Outline;
        badgeOutline.effectDistance = new Vector2(1f, -1f);

        TMP_Text badge = CreateText(badgeBackground, "EstadoTexto",
            status, 16, TextAlignmentOptions.Center);
        badge.color = new Color(0.96f, 0.92f, 0.70f);

        TMP_Text dialogueText = CreateText(card, "Dialogo",
            dialogue, 18, TextAlignmentOptions.TopLeft);
        SetAnchors(dialogueText.rectTransform, 0f, 0f, 1f, 1f);
        dialogueText.rectTransform.offsetMin = new Vector2(16f, 16f);
        dialogueText.rectTransform.offsetMax = new Vector2(-16f, -66f);
        dialogueText.overflowMode = TextOverflowModes.Overflow;
    }

    private static void CreateEntrepreneurConsole(Transform parent)
    {
        RectTransform console = AddPanel(parent, "EntrepreneurConsole",
            new Vector2(0.02f, 0.025f), new Vector2(0.98f, 0.18f));

        HorizontalLayoutGroup layout = console.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 12f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TMP_InputField input = CreateInputField(console, "Input_Respuesta",
            "Escribe tu pitch o responde a las preguntas de los tiburones...");
        SetLayout(input.GetComponent<RectTransform>(), 1400f, 112f);

        RectTransform actions = CreateObject("AccionesEmprendedor", console).GetComponent<RectTransform>();
        SetLayout(actions, 300f, 112f);

        GridLayoutGroup grid = actions.gameObject.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(2, 2, 2, 2);
        grid.cellSize = new Vector2(146f, 50f);
        grid.spacing = new Vector2(8f, 8f);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        CreateButton(actions, "EnviarRespuesta", "Enviar Respuesta", 146f);
        CreateButton(actions, "AceptarOferta", "Aceptar Oferta", 146f);
        CreateButton(actions, "Contraofertar", "Contraofertar", 146f);
        CreateButton(actions, "Rechazar", "Rechazar", 146f);
    }

    private static void CreateProposalPanel(Transform parent)
    {
        RectTransform panel = AddPanel(parent, "Panel_ElegirPropuesta",
            new Vector2(0.34f, 0.20f), new Vector2(0.66f, 0.82f));
        panel.gameObject.SetActive(false);

        TMP_Text title = CreateText(panel, "TituloPropuesta",
            "Elegir propuesta", 22, TextAlignmentOptions.Center);
        SetAnchors(title.rectTransform, 0f, 0.88f, 1f, 1f);
        title.fontStyle = FontStyles.Bold;

        CreateLabeledInput(panel, "NombreNegocio", "Nombre del negocio",
            "EcoLogix", 0.77f, 0.88f);
        CreateLabeledInput(panel, "DescripcionNegocio", "Descripción breve",
            "Plataforma de inteligencia ambiental para medir y reducir emisiones.", 0.61f, 0.76f);
        CreateLabeledInput(panel, "MontoSolicitado", "Monto solicitado ($ MXN)",
            "$500,000 MXN", 0.50f, 0.60f);
        CreateLabeledInput(panel, "PorcentajeOfrecido", "Porcentaje ofrecido (%)",
            "15%", 0.39f, 0.49f);

        CreateAnchoredButton(panel, "ComenzarPresentacion",
            "Comenzar presentación", 320f,
            new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.32f));
    }

    private static void CreateHistoryPanel(Transform parent)
    {
        RectTransform panel = AddPanel(parent, "Panel_Historial",
            new Vector2(0.71f, 0.12f), new Vector2(0.98f, 0.89f));
        panel.gameObject.SetActive(false);

        TMP_Text title = CreateText(panel, "TituloHistorial",
            "Historial de la presentación", 20,
            TextAlignmentOptions.Center);
        SetAnchors(title.rectTransform, 0f, 0.91f, 1f, 1f);
        title.fontStyle = FontStyles.Bold;

        RectTransform scrollRectTransform = CreateObject("HistorialScroll", panel).GetComponent<RectTransform>();
        SetAnchors(scrollRectTransform, 0f, 0f, 1f, 0.90f);
        scrollRectTransform.offsetMin = new Vector2(14f, 14f);
        scrollRectTransform.offsetMax = new Vector2(-14f, -14f);
        scrollRectTransform.gameObject.AddComponent<Image>().color =
            new Color(11f / 255f, 13f / 255f, 16f / 255f, 0.94f);

        ScrollRect scroll = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        RectTransform viewport = CreateObject("Viewport", scrollRectTransform).GetComponent<RectTransform>();
        SetFullStretch(viewport);
        viewport.offsetMin = new Vector2(6f, 6f);
        viewport.offsetMax = new Vector2(-6f, -6f);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

        RectTransform content = CreateObject("ContenidoHistorial", viewport).GetComponent<RectTransform>();
        SetAnchors(content, 0f, 1f, 1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(0f, 760f);

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(14, 14, 14, 14);
        contentLayout.spacing = 12f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        scroll.viewport = viewport;
        scroll.content = content;

        AddTranscriptEntry(content, "Emprendedor",
            "Hoy presentamos EcoLogix: una plataforma que convierte datos ambientales en decisiones rentables.");
        AddTranscriptEntry(content, "Tiburón 1 | Tecnología",
            "¿Qué tan difícil es replicar su tecnología y cómo planean escalar la infraestructura?");
        AddTranscriptEntry(content, "Emprendedor",
            "Nuestro núcleo combina sensores propios y modelos patentables; comenzamos con un piloto regional y luego desplegaremos API.");
        AddTranscriptEntry(content, "Tiburón 2 | Ventas",
            "¿Cuál es su costo de adquisición de clientes y qué tracción real tienen este trimestre?");
        AddTranscriptEntry(content, "Emprendedor",
            "El costo de adquisición es de $18,400 MXN y ya tenemos 42 clientes activos con 31% de crecimiento.");
        AddTranscriptEntry(content, "Tiburón 3 | Finanzas",
            "Tu valuación es alta para los márgenes actuales, pero te ofrezco los $500,000 por el 22%.");
    }

    private static void AddTranscriptEntry(Transform parent, string speaker, string message)
    {
        RectTransform entry = CreateObject("Conversacion", parent).GetComponent<RectTransform>();
        entry.gameObject.AddComponent<Image>().color =
            new Color(30f / 255f, 33f / 255f, 38f / 255f, 0.96f);
        LayoutElement layout = entry.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 96f;
        layout.preferredHeight = 96f;

        TMP_Text text = CreateText(entry, "Mensaje",
            speaker + "\n" + message, 17, TextAlignmentOptions.TopLeft);
        SetFullStretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(12f, 8f);
        text.rectTransform.offsetMax = new Vector2(-12f, -8f);
        text.overflowMode = TextOverflowModes.Overflow;
    }

    private static void CreateLabeledInput(Transform parent, string name,
        string label, string value, float minY, float maxY)
    {
        TMP_Text labelText = CreateText(parent, name + "_Etiqueta",
            label, 16, TextAlignmentOptions.MidlineLeft);
        SetAnchors(labelText.rectTransform, 0.08f, maxY - 0.025f, 0.92f, maxY);

        TMP_InputField input = CreateInputField(parent, name, value);
        SetAnchors(input.GetComponent<RectTransform>(), 0.08f, minY, 0.92f, minY + 0.055f);
    }

    private static TMP_InputField CreateInputField(Transform parent,
        string name, string placeholder)
    {
        RectTransform fieldRect = CreateObject(name, parent).GetComponent<RectTransform>();
        SetFullStretch(fieldRect);

        Image background = fieldRect.gameObject.AddComponent<Image>();
        background.color = new Color(11f / 255f, 13f / 255f, 16f / 255f, 0.96f);
        Outline outline = fieldRect.gameObject.AddComponent<Outline>();
        outline.effectColor = Outline;
        outline.effectDistance = new Vector2(1f, -1f);

        RectTransform viewport = CreateObject("TextViewport", fieldRect).GetComponent<RectTransform>();
        SetFullStretch(viewport);
        viewport.offsetMin = new Vector2(16f, 8f);
        viewport.offsetMax = new Vector2(-16f, -8f);

        TMP_Text content = CreateText(viewport, "Texto", "", 18,
            TextAlignmentOptions.MidlineLeft);
        SetFullStretch(content.rectTransform);

        TMP_Text placeholderText = CreateText(viewport, "Placeholder",
            placeholder, 18, TextAlignmentOptions.MidlineLeft);
        SetFullStretch(placeholderText.rectTransform);
        placeholderText.color = Muted;
        placeholderText.fontStyle = FontStyles.Italic;

        TMP_InputField input = fieldRect.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = viewport;
        input.textComponent = content;
        input.placeholder = placeholderText;
        input.lineType = TMP_InputField.LineType.MultiLineSubmit;

        if (name != "Input_Respuesta")
            input.text = placeholder;

        return input;
    }

    private static void CreateButton(Transform parent, string name,
        string label, float width)
    {
        RectTransform rect = CreateObject(name, parent).GetComponent<RectTransform>();
        SetLayout(rect, width, 54f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(34f / 255f, 37f / 255f, 43f / 255f, 0.98f);
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Outline;
        outline.effectDistance = new Vector2(1f, -1f);
        rect.gameObject.AddComponent<Button>();

        TMP_Text text = CreateText(rect, "Texto", label, 16,
            TextAlignmentOptions.Center);
        SetFullStretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(8f, 4f);
        text.rectTransform.offsetMax = new Vector2(-8f, -4f);
        text.fontStyle = FontStyles.Bold;
    }

    private static void CreateAnchoredButton(Transform parent, string name,
        string label, float width, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = CreateObject(name, parent).GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.sizeDelta = new Vector2(width, 54f);

        rect.gameObject.AddComponent<Image>().color =
            new Color(34f / 255f, 37f / 255f, 43f / 255f, 0.98f);
        rect.gameObject.AddComponent<Button>();

        TMP_Text text = CreateText(rect, "Texto", label, 16,
            TextAlignmentOptions.Center);
        SetFullStretch(text.rectTransform);
        text.fontStyle = FontStyles.Bold;
    }

    private static TMP_Text CreateText(Transform parent, string name,
        string value, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateObject(name, parent).GetComponent<RectTransform>();
        SetFullStretch(rect);

        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = fontSize;
        tmp.color = Text;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static RectTransform AddPanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = CreateObject(name, parent).GetComponent<RectTransform>();
        SetAnchors(rect, anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        rect.gameObject.AddComponent<Image>().color = Glass;
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Outline;
        outline.effectDistance = new Vector2(1f, -1f);
        return rect;
    }

    private static GameObject CreateObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void SetFullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect,
        float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StretchTop(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = Vector2.zero;
    }

    private static void SetLayout(RectTransform rect, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);

        LayoutElement layout = rect.gameObject.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;
    }
}
