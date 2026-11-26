using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int JudgeCount = 3;
    public const int ModifierCount = 2; // M1 y M2
    public const int MinValue = 1;      // el valor más bajo del slider
    public const int MaxValue = 5;      // el valor más alto del slider
    public static readonly PartType[] Parts = { PartType.Face, PartType.Hair, PartType.Clothes };

    // ---------------- DATOS DUMMY (cámbialos cuando tengas los reales) ----------------

    // Rol que se muestra en la tarjeta de cada juez (solo si Overwrite Role Text está activo)
    private static readonly string[] JudgeRoles = { "Rol 1", "Rol 2", "Rol 3" };

    // Valor inicial de cada juez: [juez, modificador] -> 1 a 5
    private static readonly int[,] DefaultValues =
    {
        { 1, 3 },  // juez 0: M1, M2
        { 5, 2 },  // juez 1
        { 4, 5 }   // juez 2
    };

    // ------------------------------------------------------------------------------

    private readonly int[] playerSelection = new int[3];

    // Una variable entera por slider: 3 jueces x 2 modificadores = 6 valores
    private readonly int[] judgeValues = new int[JudgeCount * ModifierCount];

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

    // Valor de 1 a 5. Ej: GetJudgeValue(0, 0) -> M1 del primer juez
    public int GetJudgeValue(int judge, int modifier) => judgeValues[ModifierSlot(judge, modifier)];

    // Guarda el valor, limitado entre MinValue y MaxValue
    public void SetJudgeValue(int judge, int modifier, int value)
    {
        judgeValues[ModifierSlot(judge, modifier)] = Mathf.Clamp(value, MinValue, MaxValue);
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
                PlayerPrefs.SetInt(JudgeKey(j, m), GetJudgeValue(j, m));
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
                SetJudgeValue(j, m, PlayerPrefs.GetInt(JudgeKey(j, m), DefaultValues[j, m]));
            }
        }
    }

    private static int ModifierSlot(int judge, int modifier) => judge * ModifierCount + modifier;

    private static string PlayerKey(PartType part) => "GM_Player_" + part;

    // Nombre nuevo (_Value) para no chocar con lo guardado en versiones anteriores
    private static string JudgeKey(int judge, int modifier) => "GM_Judge_" + judge + "_M" + (modifier + 1) + "_Value";
}