using UnityEngine;

/// <summary>
/// 火焰动画控制器
/// 按数字键1触发火焰动画
/// </summary>
[RequireComponent(typeof(Animation))]
[RequireComponent(typeof(SpriteRenderer))]
public class FireAnimationController : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("动画剪辑名称。默认为 'frame Animation'")]
    public string animationClipName = "frame Animation";
    
    [Tooltip("是否启用按键触发")]
    public bool enableKeyInput = true;
    
    [Header("渲染层级设置")]
    [Tooltip("是否自动设置渲染层级")]
    public bool autoSetSortingLayer = true;
    
    [Tooltip("Sorting Order（渲染顺序，数值越大越靠前）。建议设置为 10，确保在背景之上但不会覆盖其他重要元素")]
    public int sortingOrder = 10;
    
    private Animation animationComponent;
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        // 自动获取Animation组件
        animationComponent = GetComponent<Animation>();
        
        if (animationComponent == null)
        {
            Debug.LogError("[FireAnimationController] 未找到Animation组件！");
        }
        
        // 自动获取SpriteRenderer组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError("[FireAnimationController] 未找到SpriteRenderer组件！");
        }
        
        // 自动设置渲染层级
        if (autoSetSortingLayer && spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrder;
            Debug.Log($"[FireAnimationController] 已设置 Sorting Order 为: {sortingOrder}");
        }
    }
    
    private void Update()
    {
        // 只在运行时处理按键输入
        if (!Application.isPlaying) return;
        
        // 检测数字键1
        if (enableKeyInput && (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)))
        {
            PlayFireAnimation();
        }
    }
    
    /// <summary>
    /// 播放火焰动画（公共方法，可以被其他脚本调用）
    /// </summary>
    public void PlayFireAnimation()
    {
        if (animationComponent == null)
        {
            Debug.LogError("[FireAnimationController] Animation组件未找到，无法播放动画");
            return;
        }
        
        // 播放指定名称的动画
        if (!string.IsNullOrEmpty(animationClipName))
        {
            if (animationComponent[animationClipName] != null)
            {
                animationComponent.Play(animationClipName);
                Debug.Log($"[FireAnimationController] 播放火焰动画: {animationClipName}");
            }
            else
            {
                Debug.LogWarning($"[FireAnimationController] 未找到动画剪辑 '{animationClipName}'，请确保Animation组件中已添加该动画剪辑");
            }
        }
        else
        {
            // 如果没有指定名称，播放第一个动画
            if (animationComponent.clip != null)
            {
                animationComponent.Play();
                Debug.Log("[FireAnimationController] 播放默认动画");
            }
            else
            {
                Debug.LogWarning("[FireAnimationController] Animation组件中没有动画剪辑");
            }
        }
    }
}
