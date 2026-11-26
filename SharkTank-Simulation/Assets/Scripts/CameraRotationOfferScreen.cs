using UnityEngine;

public class CameraRotationOfferScreen : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float speed = 2f;
    [SerializeField] CanvasGroup uiToShow;   // el Canvas que aparece al final
    [SerializeField] float fadeSpeed = 2f;

    bool arrived = false;

    void Start()
    {
        if (uiToShow != null)
        {
            uiToShow.alpha = 0f;
            uiToShow.interactable = false;
            uiToShow.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (!arrived)
        {
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * speed);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * speed);

            bool closeEnough =
                Vector3.Distance(transform.position, target.position) < 0.05f &&
                Quaternion.Angle(transform.rotation, target.rotation) < 0.5f;

            if (closeEnough)
            {
                transform.SetPositionAndRotation(target.position, target.rotation);
                arrived = true;
            }
        }
        else if (uiToShow != null && uiToShow.alpha < 1f)
        {
            uiToShow.alpha = Mathf.MoveTowards(uiToShow.alpha, 1f, Time.deltaTime * fadeSpeed);
            if (uiToShow.alpha >= 1f)
            {
                uiToShow.interactable = true;
                uiToShow.blocksRaycasts = true;
            }
        }
    }
}
