using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonPressColor : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Objetivo (si se deja vacío usa la imagen del botón)")]
    [SerializeField] private Graphic target;

    [Header("Colores")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Opciones")]
    [SerializeField] private bool useHover = true;
    [Tooltip("0 = cambio instantáneo")]
    [SerializeField] private float fadeDuration = 0.05f;

    private Button button;
    private bool isHovering;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (target == null) target = button.targetGraphic;

        // Evita que el Button aplique su propio tinte y choque con este script
        button.transition = Selectable.Transition.None;
        SetColor(normalColor, true);
    }

    private void OnDisable()
    {
        isHovering = false;
        if (target != null) SetColor(normalColor, true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;
        SetColor(pressedColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable) return;
        SetColor(isHovering && useHover ? hoverColor : normalColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (!button.interactable || !useHover) return;
        SetColor(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (!button.interactable) return;
        SetColor(normalColor);
    }

    private void SetColor(Color color, bool instant = false)
    {
        if (target == null) return;
        target.CrossFadeColor(color, instant ? 0f : fadeDuration, true, true);
    }
}