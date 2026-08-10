using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    private bool m_IsLoading;

    public void LoadNextScene()
    {
        if (m_IsLoading) return;
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        m_IsLoading = true;

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        AsyncOperation operation = SceneManager.LoadSceneAsync(nextIndex);

        while (!operation.isDone)
        {
            yield return null;
        }
    }
}