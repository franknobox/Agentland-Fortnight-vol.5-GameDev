using System;
using UnityEngine;

/// <summary>
/// 语音命令判定结果数据结构
/// 对应 LLM 返回的 JSON 结果
/// </summary>
[Serializable]
public class VoiceCommandResult
{
    [Tooltip("是否包含意图")]
    public bool hasIntent;
    
    [Tooltip("意图类型（如：CastSpell、None等）")]
    public string intentType;
    
    [Tooltip("法术ID（用于匹配SpellEffectMap）")]
    public string spellId;
    
    [Tooltip("匹配的关键词")]
    public string matchedKeyword;
    
    [Tooltip("标准化后的文本")]
    public string normalizedText;
    
    [Tooltip("置信度（0-1）")]
    public float confidence;
    
    [Tooltip("判定原因/说明")]
    public string reason;
    
    /// <summary>
    /// 检查结果是否有效（包含意图且置信度大于0）
    /// </summary>
    public bool IsValid()
    {
        return hasIntent && confidence > 0f && !string.IsNullOrEmpty(spellId);
    }
    
    /// <summary>
    /// 创建空的/无效的结果
    /// </summary>
    public static VoiceCommandResult CreateEmpty()
    {
        return new VoiceCommandResult
        {
            hasIntent = false,
            intentType = "None",
            spellId = string.Empty,
            matchedKeyword = string.Empty,
            normalizedText = string.Empty,
            confidence = 0f,
            reason = "No intent detected"
        };
    }
}
