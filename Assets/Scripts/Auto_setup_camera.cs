using UnityEngine;
using Cinemachine;

public class CameraFollowPersist : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    CinemachineVirtualCamera vcam;

    void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        DontDestroyOnLoad(gameObject);
        BindPlayer();
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        BindPlayer();
    }

    void BindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null && vcam != null)
        {
            vcam.Follow = player.transform;
            vcam.LookAt = player.transform; // 2D ¿ÉÑ¡
        }
    }
}
