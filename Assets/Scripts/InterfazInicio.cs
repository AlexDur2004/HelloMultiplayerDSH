using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InterfazInicio : MonoBehaviour
{
    [SerializeField] private Text countdownText;
    [SerializeField] private Button boton;

    private void Start()
    {
        boton.onClick.AddListener(OnButtonClick);
    }

    public void OnButtonClick()
    {
        boton.interactable = false;
        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO";
        yield return new WaitForSeconds(1f);
        SceneTransition.Instance.LoadScene("Juego");
    }
}