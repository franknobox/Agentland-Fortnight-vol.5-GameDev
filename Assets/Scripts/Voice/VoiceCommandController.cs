using System;
using Cysharp.Threading.Tasks;
using PlayKit_SDK;
using UnityEngine;

public class VoiceCommandController : MonoBehaviour
{
    [Header("语音命令设置")]
    [Tooltip("Frost效果组件（用于触发冰霜效果）。如果未指定，将自动查找场景中的FrostEffect组件。")]
    public FrostEffect frostEffect;

    private PlayKit_MicrophoneRecorder _recorder;
    private PlayKit_AudioTranscriptionClient _transcriptionClient;
    private bool _isInitialized = false;

    public bool isRecording { get; private set; }
    public bool isBusy { get; private set; }

    private async void Start()
    {
        await InitializeAsync();
    }

    void Update()
    {
        // 长按空格键录音：按下时开始，松开时停止
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 按下空格键时开始录音
            if (!isRecording && !isBusy)
            {
                StartRecord();
            }
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            // 松开空格键时停止录音并处理
            if (isRecording)
            {
                StopRecordAndProcess();
            }
        }
    }

    void OnGUI()
    {
        // 在画面中显示录音状态
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperCenter;

        if (isRecording)
        {
            // 录音中：显示红色闪烁提示
            float alpha = Mathf.PingPong(Time.time * 2f, 1f);
            style.normal.textColor = new Color(1f, 0f, 0f, alpha);
            GUI.Label(new Rect(Screen.width / 2 - 100, 50, 200, 50), "● 录音中...", style);
        }
        else if (isBusy)
        {
            // 处理中：显示黄色提示
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(Screen.width / 2 - 100, 50, 200, 50), "处理中...", style);
        }
        else
        {
            // 待机状态：显示提示信息
            style.fontSize = 18;
            style.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
            GUI.Label(new Rect(Screen.width / 2 - 150, 50, 300, 50), "按住空格键开始录音", style);
        }
    }

    private async UniTask InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var result = await PlayKitSDK.InitializeAsync();
            if (!result)
            {
                Debug.LogError("[VoiceCommandController] SDK初始化失败");
                return;
            }

            _recorder = GetComponent<PlayKit_MicrophoneRecorder>();
            if (_recorder == null)
            {
                _recorder = gameObject.AddComponent<PlayKit_MicrophoneRecorder>();
            }

            _transcriptionClient = PlayKitSDK.Factory.CreateTranscriptionClient("whisper-large");
            if (_transcriptionClient == null)
            {
                Debug.LogError("[VoiceCommandController] 创建转录客户端失败");
                return;
            }

            // 如果没有手动指定FrostEffect，尝试自动查找
            if (frostEffect == null)
            {
                frostEffect = FindObjectOfType<FrostEffect>();
                if (frostEffect != null)
                {
                    Debug.Log("[VoiceCommandController] 自动找到FrostEffect组件");
                }
            }

            _isInitialized = true;
            Debug.Log("[VoiceCommandController] 初始化完成");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VoiceCommandController] 初始化异常: {ex.Message}");
        }
    }

    public void StartRecord()
    {
        if (!_isInitialized)
        {
            Debug.LogError("[VoiceCommandController] SDK未初始化，请等待初始化完成");
            return;
        }

        if (isRecording)
        {
            Debug.LogWarning("[VoiceCommandController] 正在录音中");
            return;
        }

        if (isBusy)
        {
            Debug.LogWarning("[VoiceCommandController] 正在处理中，请稍候");
            return;
        }

        if (_recorder == null)
        {
            Debug.LogError("[VoiceCommandController] 录音器未初始化");
            return;
        }

        if (_recorder.StartRecording())
        {
            isRecording = true;
            Debug.Log("[VoiceCommandController] 开始录音");
        }
        else
        {
            Debug.LogError("[VoiceCommandController] 启动录音失败");
        }
    }

    public async void StopRecordAndProcess()
    {
        if (!_isInitialized)
        {
            Debug.LogError("[VoiceCommandController] SDK未初始化");
            return;
        }

        if (!isRecording)
        {
            Debug.LogWarning("[VoiceCommandController] 未在录音状态");
            return;
        }

        if (isBusy)
        {
            Debug.LogWarning("[VoiceCommandController] 正在处理中");
            return;
        }

        isBusy = true;

        try
        {
            AudioClip audioClip = _recorder.StopRecording();
            isRecording = false;

            if (audioClip == null)
            {
                Debug.LogError("[VoiceCommandController] 录音失败，AudioClip为空");
                isBusy = false;
                return;
            }

            Debug.Log("[VoiceCommandController] 录音完成，开始转写");

            var transcriptionResult = await _transcriptionClient.TranscribeAudioClipAsync(audioClip);
            
            if (transcriptionResult == null || string.IsNullOrEmpty(transcriptionResult.Text))
            {
                Debug.LogError("[VoiceCommandController] 转写失败或结果为空");
                isBusy = false;
                return;
            }

            string transcript = transcriptionResult.Text;
            
            // 在终端输出转写结果
            Debug.Log($"[VoiceCommandController] 转写结果: {transcript}");
            
            // 检测转写结果中是否包含"冰霜"
            if (transcript.Contains("冰霜"))
            {
                Debug.Log("[VoiceCommandController] 检测到'冰霜'，触发Frost效果");
                if (frostEffect != null)
                {
                    frostEffect.TriggerOnce();
                }
                else
                {
                    Debug.LogWarning("[VoiceCommandController] FrostEffect未找到，无法触发效果");
                }
            }
            
            isBusy = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VoiceCommandController] 处理异常: {ex.Message}");
            isBusy = false;
        }
    }
}