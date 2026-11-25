using UnityEngine;

// Ponlo en la raíz de cualquier personaje (menú, juego, etc.) para que
// al iniciar la escena muestre el aspecto guardado en la personalización.
[RequireComponent(typeof(CharacterCustomizer))]
public class ApplySavedAppearance : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.ApplyTo(GetComponent<CharacterCustomizer>());
    }
}
