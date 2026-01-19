using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToScene2 : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "scene2";
    [SerializeField] private string playerTag = "Player";
    private bool isExiting;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isExiting) return;
        if (!other.CompareTag(playerTag)) return;

        isExiting = true;
        SceneManager.LoadScene(targetSceneName);
    }
}
