using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class Auto_setup_camera : MonoBehaviour
{
    [Header("玩家跟随设置")]
    [Tooltip("玩家目标Transform。如果为空，将自动搜索")]
    public Transform playerTarget;

    [Tooltip("跟随平滑速度（仅在非Cinemachine模式下使用）")]
    public float followSpeed = 5f;

    private CinemachineVirtualCamera virtualCamera;
    private Camera mainCamera;

    void Start()
    {
        SetupCamera();
    }

    void LateUpdate()
    {
        // 如果使用非Cinemachine模式，手动跟随
        if (virtualCamera == null && mainCamera != null && playerTarget != null)
        {
            FollowPlayerManually();
        }
    }

    /// <summary>
    /// 初始化相机设置
    /// </summary>
    void SetupCamera()
    {
        // 尝试获取Cinemachine虚拟相机组件
        virtualCamera = GetComponent<CinemachineVirtualCamera>();

        // 如果没有虚拟相机，获取主相机
        if (virtualCamera == null)
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        // 自动查找玩家
        if (playerTarget == null)
        {
            FindPlayer();
        }

        // 配置虚拟相机跟随
        if (virtualCamera != null)
        {
            if (playerTarget != null)
            {
                virtualCamera.Follow = playerTarget;
                virtualCamera.LookAt = playerTarget;
                Debug.Log($"[相机设置] Cinemachine虚拟相机已设置为跟随: {playerTarget.name}");
            }
            else
            {
                Debug.LogWarning("[相机设置] 未找到玩家目标，虚拟相机无法跟随！");
            }
        }
        else if (mainCamera != null)
        {
            Debug.Log("[相机设置] 使用标准相机模式（无Cinemachine）");
        }
    }

    /// <summary>
    /// 自动搜索玩家GameObject
    /// </summary>
    void FindPlayer()
    {
        // 方法1: 查找PlayerAction组件（你的玩家脚本）
        PlayerAction playerAction = FindObjectOfType<PlayerAction>();
        if (playerAction != null)
        {
            playerTarget = playerAction.transform;
            Debug.Log($"[相机设置] 通过PlayerAction组件找到玩家: {playerTarget.name}");
            return;
        }

        // 方法2: 通过标签查找
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            Debug.Log($"[相机设置] 通过'Player'标签找到玩家: {playerTarget.name}");
            return;
        }

        // 方法3: 通过名称查找（包含"Player"的GameObject）
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.activeInHierarchy &&
                (obj.name.ToLower().Contains("player") || obj.name.ToLower().Contains("角色")))
            {
                playerTarget = obj.transform;
                Debug.Log($"[相机设置] 通过名称找到玩家: {playerTarget.name}");
                return;
            }
        }

        Debug.LogWarning("[相机设置] 无法自动找到玩家！请在Inspector中手动指定playerTarget。");
    }

    /// <summary>
    /// 手动跟随玩家（当不使用Cinemachine时）
    /// </summary>
    void FollowPlayerManually()
    {
        if (playerTarget == null || mainCamera == null) return;

        Vector3 targetPosition = new Vector3(
            playerTarget.position.x,
            playerTarget.position.y,
            mainCamera.transform.position.z // 保持相机Z轴位置
        );

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 公共方法：更新跟随目标（用于场景切换时）
    /// </summary>
    public void SetPlayerTarget(Transform newTarget)
    {
        playerTarget = newTarget;

        if (virtualCamera != null)
        {
            virtualCamera.Follow = newTarget;
            virtualCamera.LookAt = newTarget;
        }
    }

    /// <summary>
    /// 当场景加载时重新搜索玩家（可在场景管理器调用）
    /// </summary>
    public void RefreshPlayerTarget()
    {
        playerTarget = null;
        FindPlayer();
        SetupCamera();
    }
}
