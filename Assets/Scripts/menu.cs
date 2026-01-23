using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class menu : MonoBehaviour
{
    [Header("场景设置")]
    [Tooltip("要跳转的场景名称")]
    public string startSceneName = "scene1";
    
    [Header("按钮物体设置")]
    [Tooltip("Start 按钮物体（需要挂载 AudioSource 组件）")]
    public GameObject startButton;
    
    [Tooltip("Quit 按钮物体（需要挂载 AudioSource 组件）")]
    public GameObject quitButton;
    
    // Update is called once per frame
    void Update()
    {
        // 检测 Start 键（Enter 或 Space）
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
        
        // 检测 Quit 键（Escape 或 Q）
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
        {
            QuitGame();
        }
    }
    
    /// <summary>
    /// 开始游戏，跳转到指定场景
    /// </summary>
    public void StartGame()
    {
        // 播放 Start 按钮上的音效
        PlayButtonSound(startButton);
        
        if (!string.IsNullOrEmpty(startSceneName))
        {
            // 延迟跳转场景，确保音效能播放
            StartCoroutine(LoadSceneAfterSound(startSceneName, startButton));
            Debug.Log($"[Menu] 跳转到场景: {startSceneName}");
        }
        else
        {
            Debug.LogWarning("[Menu] 场景名称未设置！");
        }
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        // 播放 Quit 按钮上的音效
        PlayButtonSound(quitButton);
        
        Debug.Log("[Menu] 退出游戏");
        
        // 延迟退出，确保音效能播放
        StartCoroutine(QuitAfterSound(quitButton));
    }
    
    /// <summary>
    /// 播放按钮物体上的音效
    /// </summary>
    private void PlayButtonSound(GameObject buttonObject)
    {
        if (buttonObject != null)
        {
            AudioSource audioSource = buttonObject.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
                Debug.Log($"[Menu] 播放按钮音效: {buttonObject.name}");
            }
            else
            {
                Debug.LogWarning($"[Menu] {buttonObject.name} 上未找到 AudioSource 或 AudioClip");
            }
        }
    }
    
    /// <summary>
    /// 在音效播放后加载场景
    /// </summary>
    private IEnumerator LoadSceneAfterSound(string sceneName, GameObject buttonObject)
    {
        // 如果有音效，等待音效播放完成（或至少播放一小段时间）
        if (buttonObject != null)
        {
            AudioSource audioSource = buttonObject.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                yield return new WaitForSeconds(Mathf.Min(audioSource.clip.length, 0.5f));
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// 在音效播放后退出游戏
    /// </summary>
    private IEnumerator QuitAfterSound(GameObject buttonObject)
    {
        // 如果有音效，等待音效播放完成（或至少播放一小段时间）
        if (buttonObject != null)
        {
            AudioSource audioSource = buttonObject.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                yield return new WaitForSeconds(Mathf.Min(audioSource.clip.length, 0.5f));
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        #if UNITY_EDITOR
        // 在编辑器中停止播放
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 在构建版本中退出应用
        Application.Quit();
        #endif
    }
}
