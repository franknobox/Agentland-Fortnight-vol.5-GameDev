using UnityEngine;

/// <summary>
/// 效果管理器
/// 根据spellId播放VFX和SFX
/// </summary>
public class EffectManager : MonoBehaviour
{
    [Header("效果设置")]
    [Tooltip("法术效果映射表（ScriptableObject）")]
    public SpellEffectMap map;
    
    [Tooltip("音频源（用于播放SFX）")]
    public AudioSource audioSource;
    
    /// <summary>
    /// 播放指定spellId的效果
    /// </summary>
    /// <param name="spellId">法术ID</param>
    /// <param name="anchor">锚点位置（为空则使用自身transform）</param>
    /// <returns>是否成功播放</returns>
    public bool Play(string spellId, Transform anchor = null)
    {
        // 检查map是否有效
        if (map == null)
        {
            Debug.LogWarning($"[EffectManager] SpellEffectMap未设置，无法播放效果: {spellId}");
            return false;
        }
        
        // 查找匹配的条目（使用TryGet方法，大小写不敏感）
        if (!map.TryGet(spellId, out SpellEffectEntry entry))
        {
            Debug.LogWarning($"[EffectManager] 未找到spellId '{spellId}' 对应的效果条目");
            return false;
        }
        
        // 确定锚点位置和偏移
        Transform spawnTransform = anchor != null ? anchor : transform;
        Vector3 spawnPosition = spawnTransform.position + (anchor != null ? anchor.TransformDirection(entry.localOffset) : entry.localOffset);
        
        // 实例化VFX预制体
        if (entry.vfxPrefab != null)
        {
            GameObject vfxInstance = Instantiate(entry.vfxPrefab, spawnPosition, spawnTransform.rotation);
            
            // 如果设置了followAnchor，将实例设为anchor的子物体
            if (entry.followAnchor && anchor != null)
            {
                vfxInstance.transform.SetParent(anchor);
                vfxInstance.transform.localPosition = entry.localOffset;
            }
            
            // 使用延迟销毁，确保特效播放完成后自动消失
            Destroy(vfxInstance, entry.destroyDelay);
            
            Debug.Log($"[EffectManager] 已实例化VFX: {entry.vfxPrefab.name} (spellId: {spellId}), 将在 {entry.destroyDelay} 秒后销毁");
        }
        else
        {
            Debug.LogWarning($"[EffectManager] spellId '{spellId}' 的vfxPrefab为空");
        }
        
        // 播放SFX音效
        if (entry.sfxClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(entry.sfxClip);
            Debug.Log($"[EffectManager] 已播放SFX: {entry.sfxClip.name} (spellId: {spellId})");
        }
        else if (entry.sfxClip != null && audioSource == null)
        {
            Debug.LogWarning($"[EffectManager] AudioSource未设置，无法播放SFX: {entry.sfxClip.name}");
        }
        
        return true;
    }
}
