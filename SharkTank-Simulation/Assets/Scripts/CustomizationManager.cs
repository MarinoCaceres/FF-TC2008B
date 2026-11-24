using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CustomizationManager : MonoBehaviour
{
    [Header("Personajes: 0 = Personaje, 1 a 3 = Jueces")]
    [SerializeField] private CharacterCustomizer[] characters;

    [Header("Elegir a quién editar (uno por personaje, mismo orden)")]
    [SerializeField] private Button[] selectButtons;
    [Tooltip("Opcional: objeto que se activa solo sobre el personaje seleccionado")]
    [SerializeField] private GameObject[] selectionIndicators;

    [Header("Botones de partes (afectan al personaje seleccionado)")]
    [SerializeField] private Button bodyPrevious;
    [SerializeField] private Button bodyNext;
    [SerializeField] private Button facePrevious;
    [SerializeField] private Button faceNext;
    [SerializeField] private Button hairPrevious;
    [SerializeField] private Button hairNext;

    [Header("Botones generales")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button backButton;

    [Header("Escenas")]
    [SerializeField] private string gameScene = "Game";
    [SerializeField] private string menuScene = "MainMenu";

    private GameManager gm;
    private int selected;

    private void Start()
    {
        gm = GameManager.Instance;

        if (gm == null || !ValidateSetup())
        {
            enabled = false;
            return;
        }

        LoadSelection();

        Bind(bodyPrevious, PartType.Body, -1);
        Bind(bodyNext, PartType.Body, 1);
        Bind(facePrevious, PartType.Face, -1);
        Bind(faceNext, PartType.Face, 1);
        Bind(hairPrevious, PartType.Hair, -1);
        Bind(hairNext, PartType.Hair, 1);

        if (selectButtons != null)
        {
            for (int i = 0; i < selectButtons.Length && i < characters.Length; i++)
            {
                int index = i; // copia local para el listener
                if (selectButtons[i] != null) selectButtons[i].onClick.AddListener(() => Select(index));
            }
        }

        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (backButton != null) backButton.onClick.AddListener(() => SceneManager.LoadScene(menuScene));

        Select(0);
    }

    private void Bind(Button button, PartType part, int direction)
    {
        if (button != null) button.onClick.AddListener(() => Change(part, direction));
    }

    private void Select(int index)
    {
        selected = index;

        if (selectionIndicators == null) return;
        for (int i = 0; i < selectionIndicators.Length; i++)
        {
            if (selectionIndicators[i] != null) selectionIndicators[i].SetActive(i == index);
        }
    }

    // Cambia una parte del personaje seleccionado (da la vuelta al llegar al final)
    private void Change(PartType part, int direction)
    {
        CharacterCustomizer character = characters[selected];
        character.SetPart(part, character.GetIndex(part) + direction);
    }

    private void LoadSelection()
    {
        for (int c = 0; c < characters.Length; c++)
        {
            foreach (PartType part in GameManager.Parts)
            {
                characters[c].SetPart(part, gm.GetPart(c, part));
            }
        }
    }

    private void Confirm()
    {
        for (int c = 0; c < characters.Length; c++)
        {
            foreach (PartType part in GameManager.Parts)
            {
                gm.SetPart(c, part, characters[c].GetIndex(part));
            }
        }

        gm.Save();
        SceneManager.LoadScene(gameScene);
    }

    private bool ValidateSetup()
    {
        if (characters == null || characters.Length != GameManager.CharacterCount)
        {
            Debug.LogError("Asigna exactamente 4 personajes (1 personaje + 3 jueces).");
            return false;
        }

        foreach (CharacterCustomizer c in characters)
        {
            if (c == null)
            {
                Debug.LogError("Hay un personaje sin asignar en la lista.");
                return false;
            }

            foreach (PartType part in GameManager.Parts)
            {
                if (c.GetCount(part) < 1)
                {
                    Debug.LogError(part + " no tiene opciones en " + c.name + ". Revisa que exista un hijo llamado '" + part + "' con las opciones dentro.");
                    return false;
                }
            }
        }

        return true;
    }
}