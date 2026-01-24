using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayKit_SDK;
using UnityEngine;

public enum TriggerType
{
    PrefabVfx,
    ScreenFrost
}

[Serializable]
public class SpellRule
{
    public string spellId;
    public string[] keywords;
    public TriggerType triggerType;
    public GameObject vfxPrefab;
    public float destroyDelay = 1.5f;
    public bool followAnchor = true;
    public Vector3 localOffset;
    public AudioClip sfxClip;
}

public class VoiceCommandController : MonoBehaviour
{
    [Header("语音命令设置")]
    [Tooltip("Frost效果组件（用于触发冰霜效果）。如果未指定，将自动查找场景中的FrostEffect组件。")]
    public FrostEffect frostEffect;
    
    [Header("规则配置")]
    public List<SpellRule> rules = new List<SpellRule>();
    
    [Header("锚点设置")]
    [Tooltip("默认锚点位置（如果为空则使用自身transform）")]
    public Transform defaultAnchor;
    
    [Header("音频设置")]
    [Tooltip("音频源（用于播放音效）")]
    public AudioSource audioSource;
    
    [Header("卡牌生成")]
    [Tooltip("魔法卡片生成器")]
    public MagicCardGenerator magicCardGenerator;
    
    [Header("默认行为设置")]
    [Tooltip("未命中规则时是否触发默认规则")]
    public bool defaultToAirWhenNoMatch = true;
    
    [Tooltip("默认触发的 spellId（未命中规则时使用）")]
    public string defaultSpellId = "air";

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
            
            // 初始化 AudioSource
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.playOnAwake = false;
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
            
            // 匹配规则并触发特效
            MatchAndTriggerRule(transcript);
            
            isBusy = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VoiceCommandController] 处理异常: {ex.Message}");
            isBusy = false;
        }
    }
    
    private void MatchAndTriggerRule(string transcript)
    {
        if (rules == null || rules.Count == 0)
        {
            Debug.LogWarning("[VoiceCommandController] 规则列表为空");
            return;
        }
        
        string normalized = transcript.Trim().ToLowerInvariant();
        
        foreach (var rule in rules)
        {
            if (rule == null || rule.keywords == null || rule.keywords.Length == 0)
            {
                continue;
            }
            
            foreach (var keyword in rule.keywords)
            {
                if (string.IsNullOrEmpty(keyword))
                {
                    continue;
                }
                
                string normalizedKeyword = keyword.Trim().ToLowerInvariant();
                if (normalized.Contains(normalizedKeyword))
                {
                    Debug.Log($"[VoiceCommandController] 命中规则: {rule.spellId}, 关键词: {keyword}");
                    TriggerRule(rule, transcript);
                    return;
                }
            }
        }
        
        Debug.Log("[VoiceCommandController] 未命中规则");
        
        // 未命中时触发默认规则
        if (defaultToAirWhenNoMatch && !string.IsNullOrEmpty(defaultSpellId))
        {
            Debug.Log($"[VoiceCommandController] 未命中规则，触发默认规则: {defaultSpellId}");
            TriggerBySpellId(defaultSpellId, transcript);
        }
    }
    
    public void TriggerBySpellId(string spellId, string transcript)
    {
        if (rules == null || rules.Count == 0)
        {
            Debug.LogWarning("[VoiceCommandController] 规则列表为空，无法触发");
            return;
        }
        
        if (string.IsNullOrEmpty(spellId))
        {
            Debug.LogWarning("[VoiceCommandController] spellId 为空，无法触发");
            return;
        }
        
        // 查找匹配的规则
        SpellRule matchedRule = null;
        foreach (var rule in rules)
        {
            if (rule != null && !string.IsNullOrEmpty(rule.spellId) && 
                rule.spellId.Equals(spellId, StringComparison.OrdinalIgnoreCase))
            {
                matchedRule = rule;
                break;
            }
        }
        
        if (matchedRule != null)
        {
            Debug.Log($"[VoiceCommandController] 通过 spellId 找到规则: {spellId}");
            TriggerRule(matchedRule, transcript);
        }
        else
        {
            Debug.LogWarning($"[VoiceCommandController] 未找到 spellId 为 '{spellId}' 的规则");
        }
    }
    
    private void TriggerRule(SpellRule rule, string transcript)
    {
        if (rule == null)
        {
            Debug.LogWarning("[VoiceCommandController] 规则为空");
            return;
        }
        
        switch (rule.triggerType)
        {
            case TriggerType.PrefabVfx:
                TriggerPrefabVfx(rule);
                break;
                
            case TriggerType.ScreenFrost:
                TriggerScreenFrost(rule);
                break;
        }
        
        // 生成魔法卡片
        if (magicCardGenerator != null && !string.IsNullOrEmpty(transcript))
        {
            string spellName = !string.IsNullOrEmpty(rule.spellId) ? rule.spellId : "Unknown";
            magicCardGenerator.GenerateCardAsync(transcript, spellName).Forget();
        }
        else if (magicCardGenerator == null)
        {
            Debug.LogWarning("[VoiceCommandController] MagicCardGenerator未设置，跳过卡片生成");
        }
    }
    
    private void TriggerPrefabVfx(SpellRule rule)
    {
        if (rule.vfxPrefab == null)
        {
            Debug.LogWarning($"[VoiceCommandController] 规则 {rule.spellId} 的 vfxPrefab 未设置");
            return;
        }
        
        Transform anchor = defaultAnchor != null ? defaultAnchor : transform;
        Vector3 position = anchor.position + rule.localOffset;
        
        GameObject instance = Instantiate(rule.vfxPrefab, position, anchor.rotation);
        
        if (rule.followAnchor)
        {
            instance.transform.SetParent(anchor);
            instance.transform.localPosition = rule.localOffset;
        }
        else
        {
            instance.transform.position = position;
        }
        
        if (rule.sfxClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(rule.sfxClip);
        }
        
        Destroy(instance, rule.destroyDelay);
        
        Debug.Log($"[VoiceCommandController] 触发 PrefabVfx: {rule.spellId}");
    }
    
    private void TriggerScreenFrost(SpellRule rule)
    {
        if (frostEffect != null)
        {
            frostEffect.TriggerOnce();
            Debug.Log($"[VoiceCommandController] 触发 ScreenFrost: {rule.spellId}");
        }
        else
        {
            Debug.LogWarning("[VoiceCommandController] FrostEffect未设置，无法触发屏幕冰霜效果");
        }
    }
}
