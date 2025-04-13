using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;
    private CanvasGroup canvasGroup;
    public float transitionSpeed = 1.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantener entre escenas
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        StartCoroutine(FadeIn());
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeOut(sceneName));
    }

    IEnumerator FadeIn()
    {
        canvasGroup.alpha = 1;
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * transitionSpeed;
            yield return null;
        }
        canvasGroup.blocksRaycasts = false;
    }

    IEnumerator FadeOut(string sceneName)
    {
        canvasGroup.blocksRaycasts = true;
        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime * transitionSpeed;
            yield return null;
        }
        SceneManager.LoadScene(sceneName);
        StartCoroutine(FadeIn());
    }
}