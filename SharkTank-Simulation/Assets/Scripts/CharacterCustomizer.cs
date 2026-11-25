using UnityEngine;
using UnityEngine.Serialization;

public enum PartType { Face = 0, Hair = 1, Clothes = 2 }

// Va en la raíz del prefab del personaje (Player).
// Clothes y Hair: se activa un solo objeto de la lista y se desactivan los demás.
// Face: se cambia el material del renderer de la cara.
public class CharacterCustomizer : MonoBehaviour
{
    [Header("Clothes y Hair: objetos del prefab (uno activo a la vez)")]
    [FormerlySerializedAs("bodyOptions")]
    [SerializeField] private GameObject[] clothesOptions;
    [SerializeField] private GameObject[] hairOptions;

    [Header("Face: se cambia el material")]
    [SerializeField] private Renderer faceRenderer;
    [Tooltip("Posición del material de la cara en la lista Materials del renderer (normalmente 0)")]
    [SerializeField] private int faceMaterialSlot = 0;
    [SerializeField] private Material[] faceMaterials;

    [Header("Iconos para la cuadrícula (opcional, mismo orden que las opciones)")]
    [SerializeField] private Sprite[] faceIcons;
    [SerializeField] private Sprite[] hairIcons;
    [SerializeField] private Sprite[] clothesIcons;

    private readonly int[] current = new int[3];

    public int GetCount(PartType part)
    {
        switch (part)
        {
            case PartType.Face: return faceMaterials != null ? faceMaterials.Length : 0;
            case PartType.Hair: return hairOptions != null ? hairOptions.Length : 0;
            default: return clothesOptions != null ? clothesOptions.Length : 0;
        }
    }

    public int GetIndex(PartType part) => current[(int)part];

    public Sprite GetIcon(PartType part, int index)
    {
        Sprite[] icons;
        switch (part)
        {
            case PartType.Face: icons = faceIcons; break;
            case PartType.Hair: icons = hairIcons; break;
            default: icons = clothesIcons; break;
        }

        return (icons != null && index >= 0 && index < icons.Length) ? icons[index] : null;
    }

    // Aplica la opción "index" (con vuelta al inicio si se pasa)
    public void SetPart(PartType part, int index)
    {
        int count = GetCount(part);
        if (count == 0) return;

        index = ((index % count) + count) % count;
        current[(int)part] = index;

        switch (part)
        {
            case PartType.Face: ApplyFace(index); break;
            case PartType.Hair: ActivateOnly(hairOptions, index); break;
            case PartType.Clothes: ActivateOnly(clothesOptions, index); break;
        }
    }

    private void ActivateOnly(GameObject[] options, int index)
    {
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null) options[i].SetActive(i == index);
        }
    }

    private void ApplyFace(int index)
    {
        if (faceRenderer == null) return;

        Material[] mats = faceRenderer.sharedMaterials;
        if (faceMaterialSlot < 0 || faceMaterialSlot >= mats.Length)
        {
            Debug.LogWarning("Face Material Slot (" + faceMaterialSlot + ") no existe en " + faceRenderer.name);
            return;
        }

        mats[faceMaterialSlot] = faceMaterials[index];
        faceRenderer.sharedMaterials = mats;
    }
}