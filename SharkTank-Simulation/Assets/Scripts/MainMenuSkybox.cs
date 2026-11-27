using UnityEngine;

public class MainMenuSkybox : MonoBehaviour
{
    [SerializeField] public float rotationSpeed = 1.2f;
    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationSpeed);
    }
}
