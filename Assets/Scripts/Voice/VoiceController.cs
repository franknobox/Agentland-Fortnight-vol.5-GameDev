using System;
using Cysharp.Threading.Tasks;
using PlayKit_SDK;
using UnityEngine;

public class VoiceController : MonoBehaviour
{
    private const string TRANSCRIBE_MODEL = "whisper-large";
    private const string LLM_MODEL = "gemini-2.5-pro";
    private const int SAMPLE_RATE = 16000;
    private const int MAX_RECORDING_LENGTH = 30; // 秒
    
    [Header("效果设置")]
    [Tooltip("效果管理器")]
    public EffectManager effectManager;
    
    [Tooltip("屏幕特效路由器")]
    public ScreenEffectRouter screenEffectRouter;
    
    [Tooltip("默认锚点位置（如果为空则使用自身transform）")]
    public Transform defaultAnchor;
    
    [Tooltip("最小置信度阈值（0-1）")]
    [Range(0f, 1f)]
    public float minConfidence = 0.6f;
    
    [Header("魔法卡片生成")]
    [Tooltip("魔法卡片生成器")]
    public MagicCardGenerator magicCardGenerator;
    
    private PlayKit_AudioTranscriptionClient _transcriptionClient;
    private PlayKit_AIChatClient _chatClient;
    private bool _isInitialized = false;
    
    private string _currentDevice = null;
    private AudioClip _recordingClip = null;
    private int _lastSample = 0;
    
    public bool isRecording { get; private set; }
    public bool isBusy { get; private set; }
    
    // JSON Schema for VoiceCommandResult
    private const string VOICE_COMMAND_SCHEMA = @"{
        ""type"": ""object"",
        ""properties"": {
            ""hasIntent"": { ""type"": ""boolean"" },
            ""intentType"": { ""type"": ""string"" },
            ""spellId"": { ""type"": ""string"" },
            ""matchedKeyword"": { ""type"": ""string"" },
            ""normalizedText"": { ""type"": ""string"" },
            ""confidence"": { ""type"": ""number"" },
            ""reason"": { ""type"": ""string"" }
        },
        ""required"": [""hasIntent"", ""intentType"", ""spellId"", ""matchedKeyword"", ""normalizedText"", ""confidence"", ""reason""]
    }";
    
    // System prompt for LLM
    private const string SYSTEM_PROMPT = @"你是一个""语音咒语判定器""。我会给你一段语音转写文本 transcript。你的任务是：判断 transcript 是否触发某个法术特效，并输出严格 JSON。

【输出要求】
- 只能输出 JSON，禁止输出任何解释、markdown、代码块、额外字符。
- JSON 字段必须且只能包含以下键：
  hasIntent (boolean)
  intentType (string: ""CastSpell"" 或 ""None"")
  spellId (string: 下方列表之一，或空字符串 """")
  matchedKeyword (string: 命中的关键词或空字符串 """")
  normalizedText (string: 你对 transcript 的清洗/规范化文本)
  confidence (number: 0~1)
  reason (string: 简短原因)

【判定规则】
- 先把 transcript 做规范化：去除首尾空格，统一大小写，中文保留，英文转小写；normalizedText 输出规范化后的文本。
- 只要 normalizedText 中""包含""任意关键词（子串匹配即可，不需要完全一致），就判定为触发：
  hasIntent=true, intentType=""CastSpell"", spellId=对应法术ID, matchedKeyword=命中关键词之一, confidence给出0~1。
- 如果没有命中任何关键词：
  hasIntent=true
  intentType=""CastSpell""
  spellId=""air""
  matchedKeyword=""""
  confidence=0.6
  reason=""未命中已知关键词，默认触发 air""

【spellId 与关键词映射（严格按照此表输出 spellId）】

1) spellId=""fire""
关键词：火, 蜡烛,fire, incendio, incendo

2) spellId=""frozen""
关键词：冰, frozen, freeze, frost, ice

3) spellId=""potions""
关键词：药, potions, potion, brew

4) spellId=""attack""
关键词：攻击, 砍, 斩, 气, sword, slash, attack, hit

5) spellId=""book""
关键词：书, book, spellbook, tome

6) spellId=""magic circle""
关键词：魔法阵, 法阵, 门，传送, 符文阵, magic circle, circle, rune circle

7) spellId=""coin""
关键词：画像,老人，奶奶，老巫师，金加隆，coin, coins, gold

8) spellId=""explode""
关键词：爆炸, 炸弹, 爆破, explode, explosion, bomb, boom

9) spellId=""lightening""
关键词：闪电, 雷电, 打雷，锋, lightning, thunder, bolt, lightening

【冲突处理】
- 如果同时命中多个 spellId，选择""更像法术触发""的那个；若仍冲突，按下列优先级从上到下选第一个命中的：
fire > frozen > potions > attack > book > magic circle > coin > explode > lightening > air

【示例（仅用于你理解，实际输出不要包含示例文字）】
输入：transcript=""来个火焰咒""
输出：{""hasIntent"":true,""intentType"":""CastSpell"",""spellId"":""fire"",""matchedKeyword"":""火焰咒"",""normalizedText"":""来个火焰咒"",""confidence"":0.9,""reason"":""命中关键词 火焰咒""}";
    
    private async void Start()
    {
        await InitializeAsync();
    }
    
    void Update()
    {
        // 按住空格键录音：按下时开始，松开时停止
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isRecording && !isBusy)
            {
                StartRecording();
            }
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            if (isRecording)
            {
                StopRecordingAndProcess();
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
            // 初始化 PlayKit SDK
            bool result = await PlayKitSDK.InitializeAsync();
            if (!result)
            {
                Debug.LogError("[VoiceController] SDK初始化失败");
                return;
            }
            
            // 创建转录客户端
            _transcriptionClient = PlayKitSDK.Factory.CreateTranscriptionClient(TRANSCRIBE_MODEL);
            if (_transcriptionClient == null)
            {
                Debug.LogError("[VoiceController] 创建转录客户端失败");
                return;
            }
            
            // 创建AI聊天客户端
            _chatClient = PlayKitSDK.Factory.CreateChatClient(LLM_MODEL);
            if (_chatClient == null)
            {
                Debug.LogError("[VoiceController] 创建AI聊天客户端失败");
                return;
            }
            
            // 检查麦克风设备
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Debug.LogError("[VoiceController] 未检测到麦克风设备");
                return;
            }
            
            _currentDevice = Microphone.devices[0];
            Debug.Log($"[VoiceController] 使用麦克风设备: {_currentDevice}");
            
            _isInitialized = true;
            Debug.Log("[VoiceController] 初始化完成");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VoiceController] 初始化异常: {ex.Message}");
        }
    }
    
    private void StartRecording()
    {
        if (!_isInitialized)
        {
            Debug.LogError("[VoiceController] SDK未初始化，请等待初始化完成");
            return;
        }
        
        if (isRecording)
        {
            Debug.LogWarning("[VoiceController] 正在录音中");
            return;
        }
        
        if (isBusy)
        {
            Debug.LogWarning("[VoiceController] 正在处理中，请稍候");
            return;
        }
        
        // 检查麦克风设备
        if (string.IsNullOrEmpty(_currentDevice))
        {
            Debug.LogError("[VoiceController] 麦克风设备未设置");
            return;
        }
        
        try
        {
            // 开始录音
            _recordingClip = Microphone.Start(_currentDevice, false, MAX_RECORDING_LENGTH, SAMPLE_RATE);
            
            if (_recordingClip == null)
            {
                Debug.LogError("[VoiceController] 启动录音失败，AudioClip为空");
                return;
            }
            
            _lastSample = 0;
            isRecording = true;
            Debug.Log("[VoiceController] 开始录音");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VoiceController] 启动录音异常: {ex.Message}");
        }
    }
    
    private async void StopRecordingAndProcess()
    {
        if (!_isInitialized)
        {
            Debug.LogError("[VoiceController] SDK未初始化");
            return;
        }
        
        if (!isRecording)
        {
            Debug.LogWarning("[VoiceController] 未在录音状态");
            return;
        }
        
        if (isBusy)
        {
            Debug.LogWarning("[VoiceController] 正在处理中");
            return;
        }
        
        isBusy = true;
        
        try
        {
            // 停止录音
            int lastPosition = Microphone.GetPosition(_currentDevice);
            Microphone.End(_currentDevice);
            isRecording = false;
            
            if (_recordingClip == null)
            {
                Debug.LogError("[VoiceController] 录音失败，AudioClip为空");
                isBusy = false;
                return;
            }
            
            // 检查录音长度
            if (lastPosition == 0)
            {
                Debug.LogError("[VoiceController] 录音长度为0，可能没有录制到声音");
                Destroy(_recordingClip);
                _recordingClip = null;
                isBusy = false;
                return;
            }
            
            Debug.Log($"[VoiceController] 录音完成，采样数: {lastPosition}");
            
            // 转写音频
            Debug.Log("[VoiceController] 开始转写音频");
            var transcriptionResult = await _transcriptionClient.TranscribeAudioClipAsync(_recordingClip);
            
            if (transcriptionResult == null || string.IsNullOrEmpty(transcriptionResult.Text))
            {
                Debug.LogError("[VoiceController] 转写失败或结果为空");
                Destroy(_recordingClip);
                _recordingClip = null;
                isBusy = false;
                return;
            }
            
            string transcript = transcriptionResult.Text.Trim();
            Debug.Log($"[VoiceController] 转写结果: {transcript}");
            
            // LLM 理解文本
            Debug.Log("[VoiceController] 开始LLM理解");
            string userPrompt = $"用户说：{transcript}";
            
            var jsonResult = await _chatClient.GenerateStructuredWithSchemaAsync(
                VOICE_COMMAND_SCHEMA,
                userPrompt,
                "VoiceCommand",
                SYSTEM_PROMPT,
                temperature: 0.3f,
                maxTokens: 200
            );
            
            if (jsonResult == null)
            {
                Debug.LogError("[VoiceController] LLM返回结果为空");
                Destroy(_recordingClip);
                _recordingClip = null;
                isBusy = false;
                return;
            }
            
            // 解析JSON结果
            string jsonString = jsonResult.ToString();
            Debug.Log($"[VoiceController] LLM返回JSON: {jsonString}");
            
            VoiceCommandResult result;
            try
            {
                result = JsonUtility.FromJson<VoiceCommandResult>(jsonString);
                
                if (result == null)
                {
                    Debug.LogError("[VoiceController] JSON解析失败，结果为null");
                    Destroy(_recordingClip);
                    _recordingClip = null;
                    isBusy = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoiceController] JSON解析异常: {ex.Message}");
                Destroy(_recordingClip);
                _recordingClip = null;
                isBusy = false;
                return;
            }
            
            // 输出转写结果和JSON
            Debug.Log($"[VoiceController] 转写: {transcript}");
            Debug.Log($"[VoiceController] JSON: {jsonString}");
            
            // 输出识别结果摘要
            Debug.Log($"transcript: {transcript}");
            Debug.Log($"matchedKeyword: {result.matchedKeyword ?? ""}");
            Debug.Log($"spellId: {result.spellId ?? ""}");
            
            // 生成魔法卡片
            if (magicCardGenerator != null && !string.IsNullOrEmpty(transcript))
            {
                string spellName = !string.IsNullOrEmpty(result.spellId) ? result.spellId : "Unknown";
                magicCardGenerator.GenerateCardAsync(transcript, spellName).Forget();
            }
            else if (magicCardGenerator == null)
            {
                Debug.LogWarning("[VoiceController] MagicCardGenerator未设置，跳过卡片生成");
            }
            
            // 检查结果并播放效果 
            if (result.confidence >= minConfidence && !string.IsNullOrEmpty(result.spellId))
            {
                // 特殊处理 frozen 特效，不走 EffectManager
                if (result.spellId == "frozen")
                {
                    if (screenEffectRouter != null)
                    {
                        screenEffectRouter.PlayBySpellId(result.spellId);
                        Debug.Log($"[VoiceController] 成功播放屏幕特效: {result.spellId}");
                    }
                    else
                    {
                        Debug.LogWarning("[VoiceController] ScreenEffectRouter未设置，无法播放屏幕特效");
                    }
                }
                else
                {
                    // 其他特效走 EffectManager
                    Transform anchor = defaultAnchor != null ? defaultAnchor : transform;

                    if (effectManager != null)
                    {
                        bool success = effectManager.Play(result.spellId, anchor);
                        if (success)
                        {
                            Debug.Log($"[VoiceController] 成功播放效果: {result.spellId}");
                        }
                        else
                        {
                            Debug.LogWarning($"[VoiceController] 播放效果失败: {result.spellId}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[VoiceController] EffectManager未设置，无法播放效果");
                    }
                }
            }
            else
            {
                Debug.Log($"[VoiceController] 未满足播放条件 - hasIntent: {result.hasIntent}, confidence: {result.confidence}, spellId: {result.spellId ?? "null"}");
            }
            
            // 清理录音资源
            Destroy(_recordingClip);
            _recordingClip = null;
            
            isBusy = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VoiceController] 处理异常: {ex.Message}");
            Debug.LogError($"[VoiceController] 堆栈跟踪: {ex.StackTrace}");
            
            // 清理资源
            if (_recordingClip != null)
            {
                Destroy(_recordingClip);
                _recordingClip = null;
            }
            
            isBusy = false;
        }
    }
    
    void OnDestroy()
    {
        // 清理资源
        if (isRecording)
        {
            Microphone.End(_currentDevice);
        }
        
        if (_recordingClip != null)
        {
            Destroy(_recordingClip);
        }
    }
}
