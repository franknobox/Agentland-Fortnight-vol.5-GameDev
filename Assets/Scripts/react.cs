using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
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

    private bool playerInTrigger;

    private void Reset()
    {
        // 确保 Collider 是触发器，便于用户快速设置
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);

        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"{name}: Collider 的 IsTrigger 建议勾选 (已在 Reset 尝试设置)。");
        }
    }

    private void Update()
    {
        if (playerInTrigger && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInTrigger = true;
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInTrigger = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    private void Interact()
    {
        onInteract?.Invoke();
        Debug.Log($"Interact triggered on {name}.");
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        Gizmos.color = Color.yellow;
        if (col is SphereCollider sc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(sc.center, sc.radius);
        }
        else if (col is BoxCollider bc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bc.center, bc.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}