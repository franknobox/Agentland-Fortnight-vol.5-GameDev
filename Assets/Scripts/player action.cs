using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAction : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f; // 移动速度
   
    private Rigidbody2D rb; // Rigidbody2D组件
    private   Animator anim; // 动画控制器    
    private Vector2 movement; // 存储移动输入
    
    void Awake()
    {
        // 自动获取Rigidbody2D组件
        rb = GetComponent<Rigidbody2D>();
        
        // 配置Rigidbody2D物理属性（俯视角游戏设置）
        if (rb != null)
        {
            rb.gravityScale = 0f; // 关闭重力（俯视角游戏无重力）
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 冻结Z轴旋转
        }
        
        // 自动获取Animator组件
        anim = GetComponent<Animator>();
    }
    
    void Update()
    {
        // 获取输入（支持WASD和方向键）
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // 构建移动向量并归一化（使斜向移动不会更快）
        movement = new Vector2(horizontal, vertical).normalized;
        
        // 根据水平移动方向翻转角色（左右转向）
        if (Mathf.Abs(horizontal) > 0.1f)
        {
            // 向右移动：scale.x为正；向左移动：scale.x为负
            Vector3 scale = transform.localScale;
            scale.x = horizontal > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        
        // 根据输入是否为零设置动画参数IsMoving
        if (anim && anim != null)
        {
            bool isMoving = movement.magnitude > 0.1f;
            anim.SetBool("IsMoving", isMoving);
        }
    }
    
    void FixedUpdate()
    {
        // 使用Rigidbody2D.MovePosition进行物理移动
        if (rb != null)
        {
            // 计算目标位置（移动向量已归一化）
            Vector2 targetPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
    }
}
