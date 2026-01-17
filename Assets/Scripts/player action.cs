using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f; // 移动速度
    
    [Header("动画设置")]
    [Tooltip("角色动画控制器。如果未指定，将自动从当前GameObject获取。")]
    public Animator playerAnim; // 动画控制器（可在Inspector中手动指定）
    
    private Vector2 movement; // 存储移动方向
    
    // Start is called before the first frame update
    void Start()
    {
        // 如果没有手动指定Animator，则自动获取
        if (playerAnim == null)
        {
            playerAnim = GetComponent<Animator>();
            if (playerAnim == null)
            {
                Debug.LogWarning("未找到Animator组件！请确保角色上已添加Animator组件，或在Inspector中手动指定playerAnim。");
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // 获取输入
        movement.x = 0;
        movement.y = 0;
        
        // W键 - 向上移动
        if (Input.GetKey(KeyCode.W))
        {
            movement.y = 1;
        }
        
        // S键 - 向下移动
        if (Input.GetKey(KeyCode.S))
        {
            movement.y = -1;
        }
        
        // A键 - 向左移动
        if (Input.GetKey(KeyCode.A))
        {
            movement.x = -1;
        }
        
        // D键 - 向右移动
        if (Input.GetKey(KeyCode.D))
        {
            movement.x = 1;
        }
        
        // 规范化移动向量，使对角线移动不会更快
        movement = movement.normalized;
        
        // 控制动画
        if (playerAnim != null)
        {
            // 判断是否在移动
            bool isMoving = movement.magnitude > 0.1f;
            
            if (isMoving)
            {
                // 移动时切换到Walking动画
                // 直接使用Play强制切换到Walking状态（第0层）
                playerAnim.Play("Walking", 0, 0f);
            }
            else
            {
                // 不移动时切换到Idle状态
                playerAnim.Play("Idle", 0, 0f);
            }
        }
        
        // 应用移动
        transform.Translate(movement * moveSpeed * Time.deltaTime);
    }
}
