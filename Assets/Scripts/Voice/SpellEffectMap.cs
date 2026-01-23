using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 法术效果条目
/// </summary>
[System.Serializable]
public class SpellEffectEntry
{
    [Tooltip("法术ID（用于匹配）")]
    public string spellId;
    
    [Tooltip("VFX预制体（可以是GameObject或ParticleSystem）")]
    public GameObject vfxPrefab;
    
    [Tooltip("SFX音效剪辑")]
    public AudioClip sfxClip;
    
    [Tooltip("是否跟随锚点（设为true时，实例会成为anchor的子物体）")]
    public bool followAnchor = true;
    
    [Tooltip("本地偏移量（相对于锚点的位置偏移）")]
    public Vector3 localOffset = Vector3.zero;
    
    [Tooltip("销毁延迟时间（秒），特效播放完成后自动销毁实例")]
    public float destroyDelay = 1.5f;
}

/// <summary>
/// 法术效果映射表（ScriptableObject）
/// 用于配置 spellId -> VFX prefab + SFX clip
/// </summary>
[CreateAssetMenu(fileName = "New Spell Effect Map", menuName = "Game/Spell Effect Map")]
public class SpellEffectMap : ScriptableObject
{
    [Tooltip("法术效果条目列表")]
    public List<SpellEffectEntry> entries = new List<SpellEffectEntry>();
    
    /// <summary>
    /// 根据spellId查找对应的效果条目（大小写不敏感，忽略首尾空格）
    /// </summary>
    /// <param name="spellId">法术ID</param>
    /// <param name="entry">找到的条目（如果找到）</param>
    /// <returns>是否找到匹配的条目</returns>
    public bool TryGet(string spellId, out SpellEffectEntry entry)
    {
        entry = null;
        
        if (entries == null || string.IsNullOrEmpty(spellId))
            return false;
        
        // 标准化spellId：去除首尾空格并转为小写
        string normalizedSpellId = spellId.Trim().ToLowerInvariant();
        
        if (string.IsNullOrEmpty(normalizedSpellId))
            return false;
        
        // 遍历查找匹配的条目
        foreach (var e in entries)
        {
            if (e != null && !string.IsNullOrEmpty(e.spellId))
            {
                // 标准化条目中的spellId
                string normalizedEntryId = e.spellId.Trim().ToLowerInvariant();
                
                if (normalizedEntryId == normalizedSpellId)
                {
                    entry = e;
                    return true;
                }
            }
        }
        
        return false;
    }
}
