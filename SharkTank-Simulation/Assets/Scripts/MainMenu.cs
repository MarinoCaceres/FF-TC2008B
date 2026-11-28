using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button customizeButton;
    [SerializeField] private Button quitButton;

    [Header("Nombres de escenas")]
    [SerializeField] private string gameScene = "Game";
    [SerializeField] private string customizationScene = "Customization";

    [Header("Animación de inicio")]
    [SerializeField] private Transform platform;   
    
    [Header("Audio de subida")]
    [SerializeField] private AudioSource riseAudio;       // Center_Platform_Player
    [SerializeField] private float riseHeight = 15f;      // cuánto sube
    [SerializeField] private float riseDuration = 2.5f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private ScreenFader fader;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float fadeStartsAt = 0.6f;   // en qué % de la subida empieza el fade
    [SerializeField] private CanvasGroup menuUI;          // opcional: para ocultar los botones

    private bool transitioning;

    private void Start()
    {
        startButton.onClick.AddListener(OnStart);
        customizeButton.onClick.AddListener(OnCustomize);
        quitButton.onClick.AddListener(OnQuit);
    }

    private void OnStart()
    {
        if (transitioning) return;
        StartCoroutine(StartSequence());
    }

    private IEnumerator StartSequence()
    {
        if (riseAudio != null) riseAudio.Play();
        transitioning = true;
        startButton.interactable = customizeButton.interactable = quitButton.interactable = false;
        if (menuUI != null) menuUI.alpha = 0f;

        Vector3 from = platform.position;
        Vector3 to = from + Vector3.up * riseHeight;
        bool fadeStarted = false;
        float t = 0f;

        while (t < riseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / riseDuration);
            platform.position = Vector3.LerpUnclamped(from, to, riseCurve.Evaluate(k));

            if (!fadeStarted && k >= fadeStartsAt)
            {
            fadeStarted = true;
            StartCoroutine(fader.FadeOut(fadeDuration));
            if (riseAudio != null) StartCoroutine(FadeAudio(riseAudio, fadeDuration));
        }
            yield return null;
        }

        if (!fadeStarted) yield return fader.FadeOut(fadeDuration);
        else yield return new WaitForSeconds(fadeDuration * 0.5f); // margen para que termine el fade

        SceneManager.LoadScene(gameScene);
    }
    private IEnumerator FadeAudio(AudioSource source, float duration)
{
    float startVolume = source.volume;
    float t = 0f;
    while (t < duration)
    {
        t += Time.deltaTime;
        source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
        yield return null;
    }
    source.Stop();
    source.volume = startVolume;
}
    private void OnCustomize() { SceneManager.LoadScene(customizationScene); }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}