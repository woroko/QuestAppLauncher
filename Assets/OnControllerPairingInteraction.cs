using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OnControllerPairingInteraction : MonoBehaviour
{
    private float lastEntered = -1f;
    public float dwellTime = 3f;
    public TMP_Text pairingText;
    private string originalPairingMessage = "";
    public string pairingHelpMessage = "Hold down the Oculus and Back buttons on the controller to pair";

    // Start is called before the first frame update
    void Start()
    {
        originalPairingMessage = pairingText.text;
    }

    public void OnHoverEnter(Transform t)
    {
        lastEntered = Time.time;
    }

    public void OnHoverExit(Transform t)
    {
        lastEntered = -1f;
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
