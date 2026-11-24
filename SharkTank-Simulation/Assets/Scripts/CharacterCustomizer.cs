using UnityEngine;

public enum PartType { Body = 0, Face = 1, Hair = 2 }

// Va en la raíz del prefab del personaje (Player).
// Body y Hair: se activa un solo objeto de la lista y se desactivan los demás.
// Face: se cambia el material del renderer de la cara.
public class CharacterCustomizer : MonoBehaviour
{
    [Header("Body y Hair: objetos del prefab (uno activo a la vez)")]
    [SerializeField] private GameObject[] bodyOptions;
    [SerializeField] private GameObject[] hairOptions;

    [Header("Face: se cambia el material")]
    [SerializeField] private Renderer faceRenderer;
    [Tooltip("Posición del material de la cara en la lista Materials del renderer (normalmente 0)")]
    [SerializeField] private int faceMaterialSlot = 0;
    [SerializeField] private Material[] faceMaterials;

    private readonly int[] current = new int[3];

    public int GetCount(PartType part)
    {
        switch (part)
        {
            case PartType.Body: return bodyOptions != null ? bodyOptions.Length : 0;
            case PartType.Face: return faceMaterials != null ? faceMaterials.Length : 0;
            default: return hairOptions != null ? hairOptions.Length : 0;
        }
    }

    public int GetIndex(PartType part) => current[(int)part];

    // Aplica la opción "index" (con vuelta al inicio si se pasa)
    public void SetPart(PartType part, int index)
    {
        int count = GetCount(part);
        if (count == 0) return;

        index = ((index % count) + count) % count;
        current[(int)part] = index;

        switch (part)
        {
            case PartType.Body: ActivateOnly(bodyOptions, index); break;
            case PartType.Hair: ActivateOnly(hairOptions, index); break;
            case PartType.Face: ApplyFace(index); break;
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