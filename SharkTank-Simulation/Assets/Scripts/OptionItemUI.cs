using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Va en la raíz del prefab de cada opción de la cuadrícula (un Button con su icono).
public class OptionItemUI : MonoBehaviour
{
    [Tooltip("Si se deja vacío usa el Button de este mismo objeto")]
    [SerializeField] private Button button;
    [Tooltip("Image hija donde se muestra el icono de la opción")]
    [SerializeField] private Image icon;
    [Tooltip("Opcional: marco u objeto que se activa solo en la opción seleccionada")]
    [SerializeField] private GameObject selectedMark;
    [Tooltip("Opcional: muestra el número de la opción cuando no hay icono")]
    [SerializeField] private TMP_Text label;

    public int Index { get; private set; }

    public void Setup(int index, Sprite iconSprite, bool selected, Action<int> onClick)
    {
        Index = index;

        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
        }

        if (label != null) label.text = iconSprite == null ? (index + 1).ToString() : "";

        SetSelected(selected);

        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => onClick(index));
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null) selectedMark.SetActive(selected);
    }
}