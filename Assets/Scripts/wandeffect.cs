using UnityEngine;
using UnityEngine.Events;
using DigitalRuby.LightningBolt;

[RequireComponent(typeof(Collider2D))]
public class TriggerInteractable : MonoBehaviour
{
    [Tooltip("玩家的 Tag，进入触发器的对象必须带此 Tag")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("按键触发，默认 E")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Tooltip("触发交互时调用（可在 Inspector 里绑定方法）")]
    [SerializeField] private UnityEvent onInteract;

    [Tooltip("可选：在玩家进入范围时显示的提示 UI（例如 '按 E 交互'）")]
    [SerializeField] private GameObject promptUI;

    [Header("Effect (Optional)")]
    [Tooltip("要在交互时生成的特效预制体（例如 SimpleLightningBoltPrefab）")]
    [SerializeField] private GameObject effectPrefab;

    [Tooltip("可选：特效生成位置；优先使用此项；为空则寻找玩家魔法棒顶端或使用触发器位置")]
    [SerializeField] private Transform effectSpawnPoint;

    [Tooltip("特效是否跟随玩家的魔法棒顶端（如果启用，会尝试在玩家对象下查找名为 Wand Tip 的子 Transform）")]
    [SerializeField] private bool followPlayerWand = true;

    [Tooltip("当 followPlayerWand 为 true 时，用于查找魔法棒顶端的子物体名（例如 \"WandTip\" 或 \"Wand/Tip\"）")]
    [SerializeField] private string wandTipName = "WandTip";

    [Tooltip("可选：自动销毁生成的特效（<= 0 则不自动销毁）")]
    [SerializeField] private float effectAutoDestroyAfter = 2f;

    [Header("Behavior")]
    [Tooltip("如果启用，玩家随时按键触发特效（不需要进入触发器）。推荐挂在场景中的空物体上作为全局控制器。")]
    [SerializeField] private bool alwaysAllow = false;

    private bool playerInTrigger;

    private void Reset()
    {
        var col2d = GetComponent<Collider2D>();
        if (col2d != null) col2d.isTrigger = true;
    }

    private void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);

        var col2d = GetComponent<Collider2D>();
        if (col2d != null && !col2d.isTrigger)
        {
            Debug.LogWarning($"{name}: Collider2D 的 IsTrigger 建议勾选 (已在 Reset 尝试设置)。");
        }

        if (GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogWarning($"{name}: 建议在交互对象或玩家上添加 Rigidbody2D（否则 2D 触发器回调可能不会触发）。");
        }
    }

    private void Update()
    {
        // 原有：在触发器内按键触发
        if (playerInTrigger && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"{name}: Interact key '{interactKey}' 按下（在触发器内），准备交互。");
            Interact();
        }

        // 新增：全局按键触发（不需要进入触发器）
        if (alwaysAllow && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"{name}: Interact key '{interactKey}' 按下（全局），准备在玩家魔法棒处生成特效。");
            InteractAtPlayer();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"{name}: OnTriggerEnter2D with '{other.gameObject.name}' (Tag='{other.gameObject.tag}').");
        if (other.CompareTag(playerTag))
        {
            playerInTrigger = true;
            if (promptUI != null) promptUI.SetActive(true);
            Debug.Log($"{name}: 玩家进入触发范围（Tag 匹配 '{playerTag}'）。");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"{name}: OnTriggerExit2D with '{other.gameObject.name}' (Tag='{other.gameObject.tag}').");
        if (other.CompareTag(playerTag))
        {
            playerInTrigger = false;
            if (promptUI != null) promptUI.SetActive(false);
            Debug.Log($"{name}: 玩家离开触发范围。");
        }
    }

    private void Interact()
    {
        Debug.Log($"{name}: Interact() 调用 —— onInteract.Invoke()");
        onInteract?.Invoke();
        SpawnEffectUsingCurrentConfig();
    }

    // 在玩家当前位置/魔法棒顶端生成（用于全局按键触发）
    private void InteractAtPlayer()
    {
        Debug.Log($"{name}: InteractAtPlayer() 调用 —— onInteract.Invoke()");
        onInteract?.Invoke();

        if (effectPrefab == null)
        {
            Debug.LogWarning($"{name}: 未设置 effectPrefab，无法生成特效。");
            return;
        }

        Vector3 pos;
        Quaternion rot;
        Transform followParent = null;

        if (effectSpawnPoint != null)
        {
            pos = effectSpawnPoint.position;
            rot = effectSpawnPoint.rotation;
            Debug.Log($"{name}: 使用 effectSpawnPoint (对象 '{effectSpawnPoint.name}') 作为生成位置。");
        }
        else
        {
            var player = GameObject.FindWithTag(playerTag);
            if (player != null && followPlayerWand)
            {
                var tip = FindChildRecursive(player.transform, wandTipName);
                if (tip != null)
                {
                    // 以 wand tip 作为父物体并将实例对齐到局部原点
                    pos = tip.position;
                    rot = tip.rotation;
                    followParent = tip;
                    Debug.Log($"{name}: 找到 WandTip '{tip.name}'，位置 {tip.position}，旋转 {tip.rotation.eulerAngles}。");
                }
                else
                {
                    pos = player.transform.position;
                    rot = Quaternion.identity;
                    Debug.LogWarning($"{name}: 未找到 WandTip，退回到玩家中心位置生成。");
                }
            }
            else if (player != null)
            {
                pos = player.transform.position;
                rot = Quaternion.identity;
            }
            else
            {
                pos = transform.position;
                rot = Quaternion.identity;
                Debug.LogWarning($"{name}: 未找到玩家对象 (Tag='{playerTag}')，在控制器位置生成特效。");
            }
        }

        var instance = Instantiate(effectPrefab, pos, rot);
        if (instance == null)
        {
            Debug.LogError($"{name}: Instantiate 返回 null（请检查 effectPrefab 是否有效）。");
            return;
        }

        // 如果需要跟随 wand tip，先把实例设为父的子物体，然后重置本地变换以保证对齐
        if (followParent != null)
        {
            instance.transform.SetParent(followParent, false); // 使用 false 以便更容易控制局部对齐
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            Debug.Log($"{name}: 实例设为 '{followParent.name}' 子物体，localPosition={instance.transform.localPosition}。");

            // --- 新增：配置 LightningBoltScript 使起点精确为 WandTip ---
            var bolt = instance.GetComponentInChildren<LightningBoltScript>();
            if (bolt != null)
            {
                // 强制手动模式并绑定起点为场景中的 WandTip（运行时对象）
                bolt.ManualMode = true;
                bolt.StartObject = followParent.gameObject;
                bolt.StartPosition = Vector3.zero;

                // 计算终点（以 wandTip 的朝向为准，可调 endDistance）
                Vector3 dir = (followParent.up != Vector3.zero) ? followParent.up : followParent.forward;
                float endDistance = 0.5f; // 可在 Inspector 或这里调整
                bolt.EndObject = null;
                bolt.EndPosition = followParent.position + dir * endDistance;

                // 触发一次闪电并立即检查 LineRenderer（便于调试）
                bolt.Trigger();
                var lr = instance.GetComponentInChildren<LineRenderer>();
                if (lr != null)
                {
                    Debug.Log($"{name}: Lightning created. LineRenderer enabled={lr.enabled}, positionCount={lr.positionCount}");
                }
                else
                {
                    Debug.LogWarning($"{name}: 找到 LightningBoltScript，但未找到 LineRenderer（检查 prefab 结构）。");
                }
            }
            else
            {
                Debug.LogWarning($"{name}: 未在实例中找到 LightningBoltScript，无法自动绑定 StartObject。");
            }
        }

        if (effectAutoDestroyAfter > 0f)
        {
            Destroy(instance, effectAutoDestroyAfter);
        }
    }

    // 复用：根据当前 Inspector 配置生成特效（触发器内调用、非全局）
    private void SpawnEffectUsingCurrentConfig()
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning($"{name}: 未设置 effectPrefab，无法生成特效。");
            return;
        }

        Vector3 pos;
        Quaternion rot;
        Transform followParent = null;

        if (effectSpawnPoint != null)
        {
            pos = effectSpawnPoint.position;
            rot = effectSpawnPoint.rotation;
        }
        else
        {
            var player = GameObject.FindWithTag(playerTag);
            if (player != null && followPlayerWand)
            {
                var tip = FindChildRecursive(player.transform, wandTipName);
                if (tip != null)
                {
                    pos = tip.position;
                    rot = tip.rotation;
                    followParent = tip;
                    Debug.Log($"{name}: 找到 WandTip '{tip.name}'，位置 {tip.position}，旋转 {tip.rotation.eulerAngles}。");
                }
                else
                {
                    pos = player.transform.position;
                    rot = Quaternion.identity;
                }
            }
            else
            {
                pos = transform.position;
                rot = Quaternion.identity;
            }
        }

        var instance = Instantiate(effectPrefab, pos, rot);
        if (instance == null)
        {
            Debug.LogError($"{name}: Instantiate 返回 null（请检查 effectPrefab 是否有效）。");
            return;
        }

        if (followParent != null)
        {
            instance.transform.SetParent(followParent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            Debug.Log($"{name}: 实例设为 '{followParent.name}' 子物体，localPosition={instance.transform.localPosition}。");

            // 同样配置 LightningBoltScript
            var bolt = instance.GetComponentInChildren<LightningBoltScript>();
            if (bolt != null)
            {
                bolt.StartObject = followParent.gameObject;
                bolt.StartPosition = Vector3.zero;
                Vector3 forwardDir = (followParent.up != Vector3.zero) ? followParent.up : followParent.forward;
                bolt.EndObject = null;
                bolt.EndPosition = followParent.position + forwardDir * 0.5f;
                bolt.Trigger();
            }
        }

        if (effectAutoDestroyAfter > 0f)
        {
            Destroy(instance, effectAutoDestroyAfter);
        }
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName)) return null;

        if (childName.Contains("/"))
        {
            var t = root.Find(childName);
            if (t != null) return t;
        }

        var direct = root.Find(childName);
        if (direct != null) return direct;

        foreach (Transform child in root)
        {
            if (child.name == childName) return child;
            var found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        var col2d = GetComponent<Collider2D>();
        Gizmos.color = Color.yellow;

        if (col2d is BoxCollider2D bc)
        {
            Vector3 center = transform.TransformPoint(bc.offset);
            Vector3 size = new Vector3(bc.size.x * transform.lossyScale.x, bc.size.y * transform.lossyScale.y, 1f);
            Gizmos.DrawWireCube(center, size);
        }
        else if (col2d is CircleCollider2D cc)
        {
            Vector3 center = transform.TransformPoint(cc.offset);
            float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            Gizmos.DrawWireSphere(center, cc.radius * maxScale);
        }
        else if (col2d != null)
        {
            Vector3 center = transform.TransformPoint(col2d.offset);
            Gizmos.DrawWireSphere(center, 1f);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }

        if (effectSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(effectSpawnPoint.position, 0.1f);
        }
    }
}