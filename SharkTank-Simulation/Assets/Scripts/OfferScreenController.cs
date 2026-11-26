using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class Offer
{
    public string judgeName;
    public int amount;
    public int equityPercent;
}

public class OfferScreenController : MonoBehaviour
{
    [SerializeField] TMP_Text offerText;
    [SerializeField] Offer[] offers;
    int current = 0;

    void Start() => ShowOffer();

    void ShowOffer()
    {
        if (current >= offers.Length) { FinishRound(); return; }
        var o = offers[current];
        offerText.text = $"{o.judgeName} te ofrece ${o.amount:N0} por el {o.equityPercent}% de tu empresa";
    }

    public void Accept()
    {
        Debug.Log($"Aceptaste la oferta de {offers[current].judgeName}");
        FinishRound();
    }

    public void Decline()
    {
        current++;
        ShowOffer();
    }

    void FinishRound()
    {
        SceneManager.LoadScene("EndScreen"); // nombre exacto de tu escena final
    }
}