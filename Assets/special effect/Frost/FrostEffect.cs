using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/Frost")]
public class FrostEffect : MonoBehaviour
{
    public float FrostAmount = 0.5f; //0-1 (0=minimum Frost, 1=maximum frost)
    public float EdgeSharpness = 1; //>=1
    public float minFrost = 0; //0-1
    public float maxFrost = 1; //0-1
    public float seethroughness = 0.2f; //blends between 2 ways of applying the frost effect: 0=normal blend mode, 1="overlay" blend mode
    public float distortion = 0.1f; //how much the original image is distorted through the frost (value depends on normal map)
    public Texture2D Frost; //RGBA
    public Texture2D FrostNormals; //normalmap
    public Shader Shader; //ImageBlendEffect.shader

    // Trigger 设置
    public KeyCode triggerKey = KeyCode.F; // 按键触发
    public float triggerDuration = 2f; // 持续时间（秒）
    public float triggeredFrostAmount = 1f; // 触发时的 FrostAmount 值（默认最大）

    // 平滑过渡设置
    public bool smoothTransition = true; // 是否使用平滑过渡
    public float transitionDuration = 0.2f; // 淡入/淡出时长（秒）
    public bool allowRetriggerDuringTransition = true; // 允许在过渡中重触发（会重启过渡）

    // 新增：启动时禁用效果（默认 true，Inspector 可改）
    public bool startDisabled = true;

    // 音效设置
    public AudioClip frostSound; // 特效触发时播放的音效
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    public bool playSoundOnTrigger = true; // 是否在触发时播放音效
    public bool stopSoundOnEnd = true; // 在特效结束时停止音效（用于 loop 音效）

    private Material material;
    private bool isTriggered;
    private float backupFrostAmount;
    private Coroutine transitionCoroutine;

    private AudioSource audioSource;

    private void Awake()
    {
        material = new Material(Shader);
        material.SetTexture("_BlendTex", Frost);
        material.SetTexture("_BumpMap", FrostNormals);

        // 准备 AudioSource（如果不存在则添加）
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = Mathf.Clamp01(soundVolume);
        // 不在 Awake 中强行绑定 clip，因为在 Inspector 运行时可能会更改；在播放前会再次设置 clip。
    }

    private void Start()
    {
        // Play 时如果选择 startDisabled，则把当前 FrostAmount 临时改为 minFrost（便于按 F 激活）
        if (Application.isPlaying && startDisabled)
        {
            backupFrostAmount = FrostAmount;
            FrostAmount = minFrost;
        }
    }

    private void Update()
    {
        // 仅在播放时响应按键
        if (!Application.isPlaying) return;

        if (Input.GetKeyDown(triggerKey))
        {
            if (isTriggered && !allowRetriggerDuringTransition)
            {
                // 忽略重复触发
                return;
            }

            if (transitionCoroutine != null)
            {
                // 如果允许重触发，停止当前过渡，让新过渡重新开始
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;

                // 停止当前音效（如果需要）
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }

            if (smoothTransition)
            {
                transitionCoroutine = StartCoroutine(TriggerFrostSmoothCoroutine(triggeredFrostAmount, triggerDuration));
            }
            else
            {
                // 立即切换（旧行为）
                StartCoroutine(TriggerFrostCoroutine());
            }
        }
    }

    // 旧的瞬时触发（保留作为回退）
    private IEnumerator TriggerFrostCoroutine()
    {
        isTriggered = true;
        // 保存当前值以便恢复（可能是 start 时的 minFrost 或用户设置）
        backupFrostAmount = FrostAmount;
        // 使用触发值（可用 maxFrost 或自定义 triggeredFrostAmount）
        FrostAmount = Mathf.Clamp01(triggeredFrostAmount);

        // 播放音效（如果有设置）
        if (playSoundOnTrigger && frostSound != null && audioSource != null)
        {
            audioSource.clip = frostSound;
            audioSource.volume = Mathf.Clamp01(soundVolume);
            audioSource.loop = true; // 持续播放直到结束（在结束时会停止）
            audioSource.Play();
        }

        yield return new WaitForSeconds(triggerDuration);

        FrostAmount = backupFrostAmount;

        if (stopSoundOnEnd && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        isTriggered = false;
    }

    // 平滑过渡：淡入 -> 保持 -> 淡出
    private IEnumerator TriggerFrostSmoothCoroutine(float targetAmount, float holdDuration)
    {
        isTriggered = true;

        float original = FrostAmount;
        targetAmount = Mathf.Clamp01(targetAmount);
        float halfDuration = Mathf.Max(0.0001f, transitionDuration); // 防止除零

        // 播放音效（如果有设置）
        if (playSoundOnTrigger && frostSound != null && audioSource != null)
        {
            audioSource.clip = frostSound;
            audioSource.volume = Mathf.Clamp01(soundVolume);
            audioSource.loop = true; // 在整个触发期间循环播放
            audioSource.Play();
        }

        // 淡入
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            FrostAmount = Mathf.Lerp(original, targetAmount, Mathf.Clamp01(t / halfDuration));
            yield return null;
        }
        FrostAmount = targetAmount;

        // 保持
        float elapsed = 0f;
        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 淡出（返回到 original）
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            FrostAmount = Mathf.Lerp(targetAmount, original, Mathf.Clamp01(t / halfDuration));
            yield return null;
        }
        FrostAmount = original;

        // 停止音效（如果需要）
        if (stopSoundOnEnd && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        isTriggered = false;
        transitionCoroutine = null;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!Application.isPlaying)
        {
            material.SetTexture("_BlendTex", Frost);
            material.SetTexture("_BumpMap", FrostNormals);
            EdgeSharpness = Mathf.Max(1, EdgeSharpness);
        }

        // 更新材质参数
        material.SetTexture("_BlendTex", Frost);
        material.SetTexture("_BumpMap", FrostNormals);
        material.SetFloat("_BlendAmount", Mathf.Clamp01(Mathf.Clamp01(FrostAmount) * (maxFrost - minFrost) + minFrost));
        material.SetFloat("_EdgeSharpness", EdgeSharpness);
        material.SetFloat("_SeeThroughness", seethroughness);
        material.SetFloat("_Distortion", distortion);

        Graphics.Blit(source, destination, material);
    }

    // 可供外部代码直接触发（例如其它脚本调用）
    public void TriggerOnce()
    {
        if (!Application.isPlaying) return;

        if (isTriggered && !allowRetriggerDuringTransition) return;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
        }

        if (smoothTransition)
        {
            transitionCoroutine = StartCoroutine(TriggerFrostSmoothCoroutine(triggeredFrostAmount, triggerDuration));
        }
        else
        {
            StartCoroutine(TriggerFrostCoroutine());
        }
    }
}