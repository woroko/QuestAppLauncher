using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OnControllerPairingInteraction : MonoBehaviour
{
    private float lastEntered = -1f;
    public float dwellTime = 3f;
    public TMP_Text pairingText;
    public Image bgImage;
    private Color originalBgImageColor = Color.black;
    private string originalPairingMessage = "";
    public string pairingHelpMessage = "Hold down the Oculus and Back buttons on the controller to pair";

    // Start is called before the first frame update
    void Start()
    {
        originalPairingMessage = pairingText.text;
        originalBgImageColor = bgImage.color;
    }

    public void OnHoverEnter(Transform t)
    {
        if (t.name == "PairingText")
        {
            lastEntered = Time.time;
            bgImage.color = new Color(0.6f, 0.1f, 0.22f);
        }
    }

    public void OnHoverExit(Transform t)
    {
        if (t.name == "PairingText")
        {
            lastEntered = -1f;
            bgImage.color = originalBgImageColor;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (lastEntered > 0f && Time.time - lastEntered > dwellTime)
        {
            StopCoroutine("ShowPairingMessage");
            RootRunner.instance.PairControllers();
            StartCoroutine(ShowPairingMessage());
            lastEntered = -1f;
        }
    }

    IEnumerator ShowPairingMessage()
    {
        pairingText.text = pairingHelpMessage;
        yield return new WaitForSeconds(60f);
        pairingText.text = originalPairingMessage;
    }
}
