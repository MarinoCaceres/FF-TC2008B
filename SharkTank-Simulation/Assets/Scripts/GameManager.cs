using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int JudgeCount = 3;
    public const int ModifierCount = 2; // M1 y M2
    public static readonly PartType[] Parts = { PartType.Face, PartType.Hair, PartType.Clothes };

    // ---------------- DATOS DUMMY (cámbialos cuando tengas los reales) ----------------

    // Rol que se muestra en la tarjeta de cada juez
    private static readonly string[] JudgeRoles = { "Rol 1", "Rol 2", "Rol 3" };

    // Puntos definidos del slider de cada modificador (cada punto es un tag string)
    private static readonly string[][] ModifierOptions =
    {
        new[] { "M1_A", "M1_B", "M1_C", "M1_D" },         // M1
        new[] { "M2_A", "M2_B", "M2_C", "M2_D", "M2_E" }  // M2
    };

    // Punto inicial de cada juez: [juez, modificador]
    private static readonly int[,] DefaultModifierIndices = { { 0, 2 }, { 1, 0 }, { 3, 4 } };

    // ------------------------------------------------------------------------------

    private readonly int[] playerSelection = new int[3];
    private readonly int[] judgeModifiers = new int[JudgeCount * ModifierCount];

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

    // ---- Jugador ----
    public int GetPart(PartType part) => playerSelection[(int)part];

    public void SetPart(PartType part, int index) => playerSelection[(int)part] = index;

    // Aplica la selección guardada a un personaje que ya está en la escena
    public void ApplyTo(CharacterCustomizer target)
    {
        foreach (PartType part in Parts)
        {
            target.SetPart(part, GetPart(part));
        }
    }

    // ---- Jueces ---- (judge: 0, 1 o 2 | modifier: 0 = M1, 1 = M2)
    public string GetJudgeRole(int judge) => JudgeRoles[judge];

    public string[] GetModifierOptions(int modifier) => ModifierOptions[modifier];

    public int GetJudgeModifierIndex(int judge, int modifier) => judgeModifiers[ModifierSlot(judge, modifier)];

    public void SetJudgeModifierIndex(int judge, int modifier, int index)
    {
        int max = ModifierOptions[modifier].Length - 1;
        judgeModifiers[ModifierSlot(judge, modifier)] = Mathf.Clamp(index, 0, max);
    }

    // El tag (string) que el juez tiene elegido en ese modificador. Ej: GetJudgeTag(0, 0) -> "M1_C"
    public string GetJudgeTag(int judge, int modifier)
    {
        return ModifierOptions[modifier][GetJudgeModifierIndex(judge, modifier)];
    }

    // ---- Guardado ----
    public void Save()
    {
        foreach (PartType part in Parts)
        {
            PlayerPrefs.SetInt(PlayerKey(part), GetPart(part));
        }

        for (int j = 0; j < JudgeCount; j++)
        {
            for (int m = 0; m < ModifierCount; m++)
            {
                PlayerPrefs.SetInt(JudgeKey(j, m), GetJudgeModifierIndex(j, m));
            }
        }

        PlayerPrefs.Save();
    }

    private void Load()
    {
        foreach (PartType part in Parts)
        {
            playerSelection[(int)part] = PlayerPrefs.GetInt(PlayerKey(part), 0);
        }

        for (int j = 0; j < JudgeCount; j++)
        {
            for (int m = 0; m < ModifierCount; m++)
            {
                int saved = PlayerPrefs.GetInt(JudgeKey(j, m), DefaultModifierIndices[j, m]);
                SetJudgeModifierIndex(j, m, saved); // también corrige índices fuera de rango
            }
        }
    }

    private static int ModifierSlot(int judge, int modifier) => judge * ModifierCount + modifier;

    private static string PlayerKey(PartType part) => "GM_Player_" + part;

    private static string JudgeKey(int judge, int modifier) => "GM_Judge_" + judge + "_M" + (modifier + 1);
}