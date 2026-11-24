using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int CharacterCount = 4; // 0 = Personaje, 1 a 3 = Jueces
    public static readonly PartType[] Parts = { PartType.Body, PartType.Face, PartType.Hair };

    private readonly int[] selection = new int[CharacterCount * 3];

    // Se crea solo antes de cargar cualquier escena: no hace falta ponerlo en el Hierarchy
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public int GetPart(int character, PartType part) => selection[SlotOf(character, part)];

    public void SetPart(int character, PartType part, int index)
    {
        selection[SlotOf(character, part)] = index;
    }

    public void Save()
    {
        for (int c = 0; c < CharacterCount; c++)
        {
            foreach (PartType part in Parts)
            {
                PlayerPrefs.SetInt(KeyOf(c, part), GetPart(c, part));
            }
        }
        PlayerPrefs.Save();
    }

    // Aplica la selección guardada de un personaje (0 = Personaje, 1 a 3 = Jueces)
    public void ApplyTo(CharacterCustomizer target, int character)
    {
        foreach (PartType part in Parts)
        {
            target.SetPart(part, GetPart(character, part));
        }
    }

    // Sin datos guardados, cada personaje arranca con la opción igual a su número (0, 1, 2, 3)
    private void Load()
    {
        for (int c = 0; c < CharacterCount; c++)
        {
            foreach (PartType part in Parts)
            {
                selection[SlotOf(c, part)] = PlayerPrefs.GetInt(KeyOf(c, part), c);
            }
        }
    }

    private static int SlotOf(int character, PartType part) => character * 3 + (int)part;

    private static string KeyOf(int character, PartType part) => "GM_" + character + "_" + part;
}