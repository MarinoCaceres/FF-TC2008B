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

    private void Start()
    {
        startButton.onClick.AddListener(OnStart);
        customizeButton.onClick.AddListener(OnCustomize);
        quitButton.onClick.AddListener(OnQuit);
    }

    private void OnStart()
    {
        SceneManager.LoadScene(gameScene);
    }

    private void OnCustomize()
    {
        SceneManager.LoadScene(customizationScene);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}