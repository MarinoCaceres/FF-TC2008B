using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CustomizationManager : MonoBehaviour
{
    // Tarjeta de un juez: rol y un slider por modificador (índice 0 = M1, 1 = M2)
    [Serializable]
    public class JudgeCard
    {
        public TMP_Text roleLabel;
        [Tooltip("Tamaño 2: M1 y M2")]
        public Slider[] sliders;
        [Tooltip("Opcional, tamaño 2: muestra el valor (1 a 5) de M1 y M2")]
        public TMP_Text[] valueLabels;
    }

    [Header("Personaje (vista previa, solo visible en la pestaña Player)")]
    [SerializeField] private CharacterCustomizer player;

    [Header("Pestañas principales")]
    [SerializeField] private Button playerTabButton;
    [SerializeField] private Button judgesTabButton;
    [Tooltip("Panel con las categorías y la cuadrícula")]
    [SerializeField] private GameObject playerPanel;
    [Tooltip("Panel con las 3 tarjetas de jueces")]
    [SerializeField] private GameObject judgesPanel;

    [Header("Pestañas de categoría")]
    [SerializeField] private Button faceTabButton;
    [SerializeField] private Button hairTabButton;
    [SerializeField] private Button clothesTabButton;

    [Header("Cuadrícula con scroll")]
    [Tooltip("Opcional: se regresa arriba al cambiar de categoría")]
    [SerializeField] private ScrollRect scrollRect;
    [Tooltip("El Content del ScrollRect (con Grid Layout Group)")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private OptionItemUI optionItemPrefab;

    [Header("Jueces: una tarjeta por juez (tamaño 3)")]
    [SerializeField] private JudgeCard[] judgeCards;
    [Tooltip("Desactivado: el texto del rol se queda como lo escribiste en el Inspector. " +
             "Activado: el script lo reemplaza con los roles dummy del GameManager.")]
    [SerializeField] private bool overwriteRoleText = false;

    [Header("Marcas de los sliders (una por cada opción)")]
    [Tooltip("Opcional: sprite de la marca. Si se deja vacío, usa un cuadrado de color")]
    [SerializeField] private Sprite tickSprite;
    [SerializeField] private Color tickColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [Tooltip("Ancho y alto de cada marca en pixeles")]
    [SerializeField] private Vector2 tickSize = new Vector2(6f, 24f);

    [Header("Colores de pestañas")]
    [SerializeField] private Color selectedTabColor = Color.white;
    [SerializeField] private Color unselectedTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Volver")]
    [SerializeField] private Button backButton;
    [SerializeField] private string menuScene = "MainMenu";

    private GameManager gm;
    private PartType currentPart = PartType.Face;
    private readonly List<OptionItemUI> items = new List<OptionItemUI>();

    private void Start()
    {
        gm = GameManager.Instance;

        if (gm == null || !ValidateSetup())
        {
            enabled = false;
            return;
        }

        // Mostrar en el personaje lo que ya estaba guardado
        gm.ApplyTo(player);

        if (playerTabButton != null) playerTabButton.onClick.AddListener(() => ShowMode(true));
        if (judgesTabButton != null) judgesTabButton.onClick.AddListener(() => ShowMode(false));

        BindPartTab(faceTabButton, PartType.Face);
        BindPartTab(hairTabButton, PartType.Hair);
        BindPartTab(clothesTabButton, PartType.Clothes);

        SetupJudges();

        if (backButton != null) backButton.onClick.AddListener(() => SceneManager.LoadScene(menuScene));

        ShowMode(true);
        ShowPart(PartType.Face);
    }

    // ---------- Pestañas principales (Player / Judges) ----------

    private void ShowMode(bool showPlayer)
    {
        if (playerPanel != null) playerPanel.SetActive(showPlayer);
        if (judgesPanel != null) judgesPanel.SetActive(!showPlayer);

        player.gameObject.SetActive(showPlayer);

        SetTabColor(playerTabButton, showPlayer);
        SetTabColor(judgesTabButton, !showPlayer);
    }

    // ---------- Categorías (Face / Hair / Clothes) y cuadrícula ----------

    private void BindPartTab(Button button, PartType part)
    {
        if (button != null) button.onClick.AddListener(() => ShowPart(part));
    }

    private void ShowPart(PartType part)
    {
        currentPart = part;

        SetTabColor(faceTabButton, part == PartType.Face);
        SetTabColor(hairTabButton, part == PartType.Hair);
        SetTabColor(clothesTabButton, part == PartType.Clothes);

        RebuildGrid();
    }

    private void RebuildGrid()
    {
        foreach (Transform child in gridContent)
        {
            child.gameObject.SetActive(false); // evita que el layout lo cuente este frame
            Destroy(child.gameObject);
        }
        items.Clear();

        int count = player.GetCount(currentPart);
        int selectedIndex = player.GetIndex(currentPart);

        for (int i = 0; i < count; i++)
        {
            OptionItemUI item = Instantiate(optionItemPrefab, gridContent);
            item.Setup(i, player.GetIcon(currentPart, i), i == selectedIndex, OnOptionClicked);
            items.Add(item);
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void OnOptionClicked(int index)
    {
        player.SetPart(currentPart, index);

        gm.SetPart(currentPart, index);
        gm.Save();

        foreach (OptionItemUI item in items)
        {
            item.SetSelected(item.Index == index);
        }
    }

    private void SetTabColor(Button tab, bool selected)
    {
        if (tab != null && tab.targetGraphic != null)
        {
            tab.targetGraphic.color = selected ? selectedTabColor : unselectedTabColor;
        }
    }

    // ---------- Jueces ----------

    private void SetupJudges()
    {
        if (judgeCards == null) return;

        int cards = Mathf.Min(judgeCards.Length, GameManager.JudgeCount);

        for (int j = 0; j < cards; j++)
        {
            JudgeCard card = judgeCards[j];
            if (card == null) continue;

            if (overwriteRoleText && card.roleLabel != null) card.roleLabel.text = gm.GetJudgeRole(j);
            if (card.sliders == null) continue;

            int sliderCount = Mathf.Min(card.sliders.Length, GameManager.ModifierCount);

            for (int m = 0; m < sliderCount; m++)
            {
                Slider slider = card.sliders[m];
                if (slider == null) continue;

                int judge = j; // copias locales para el listener
                int modifier = m;
                int options = GameManager.MaxValue - GameManager.MinValue + 1;

                // El slider solo se detiene en números enteros: del 1 (más bajo) al 5
                slider.wholeNumbers = true;
                slider.minValue = GameManager.MinValue;
                slider.maxValue = GameManager.MaxValue;
                slider.SetValueWithoutNotify(gm.GetJudgeValue(j, m));
                slider.onValueChanged.AddListener(value => OnModifierChanged(judge, modifier, Mathf.RoundToInt(value)));

                BuildTicks(slider, options);
                RefreshModifierLabel(judge, modifier);
            }
        }
    }

    // Dibuja una marca por cada opción, repartidas a lo largo del slider
    private void BuildTicks(Slider slider, int count)
    {
        // Las marcas van dentro de "Handle Slide Area" para que coincidan exactamente con
        // los puntos donde se detiene el handle. Se colocan primero para quedar detrás de él.
        RectTransform parent = slider.handleRect != null
            ? slider.handleRect.parent as RectTransform
            : slider.GetComponent<RectTransform>();

        if (parent == null) return;

        // Limpia marcas anteriores (por si la escena se reinicia)
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith("Tick_"))
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;

            GameObject tick = new GameObject("Tick_" + i, typeof(RectTransform), typeof(Image));
            RectTransform rect = tick.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.SetSiblingIndex(i); // detrás del handle

            rect.anchorMin = new Vector2(t, 0.5f);
            rect.anchorMax = new Vector2(t, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = tickSize;
            rect.anchoredPosition = Vector2.zero;

            Image image = tick.GetComponent<Image>();
            image.sprite = tickSprite;
            image.color = tickColor;
            image.raycastTarget = false; // no estorba al arrastrar el slider
        }
    }

    private void OnModifierChanged(int judge, int modifier, int value)
    {
        gm.SetJudgeValue(judge, modifier, value);
        gm.Save();

        RefreshModifierLabel(judge, modifier);
    }

    private void RefreshModifierLabel(int judge, int modifier)
    {
        JudgeCard card = judgeCards[judge];

        if (card.valueLabels != null && modifier < card.valueLabels.Length && card.valueLabels[modifier] != null)
        {
            card.valueLabels[modifier].text = gm.GetJudgeValue(judge, modifier).ToString();
        }
    }

    // ---------- Validación ----------

    private bool ValidateSetup()
    {
        if (player == null)
        {
            Debug.LogError("Asigna el personaje (el que tiene CharacterCustomizer) en el campo Player.");
            return false;
        }

        if (gridContent == null || optionItemPrefab == null)
        {
            Debug.LogError("Asigna Grid Content (el Content del ScrollRect) y Option Item Prefab.");
            return false;
        }

        return true;
    }
}