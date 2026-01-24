using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CardCollectionManager : MonoBehaviour
{
    private const int WIN_CARD_COUNT = 1;
    
    [Header("卡牌收集设置")]
    [Tooltip("通关界面Panel（收集1张卡牌后显示）")]
    public GameObject winPanel;
    
    [Header("事件")]
    [Tooltip("收集到新卡牌时触发")]
    public UnityEvent<int> OnCardCollected;
    
    [Tooltip("收集满1张卡牌时触发")]
    public UnityEvent OnAllCardsCollected;
    
    private HashSet<string> _collectedCards = new HashSet<string>();
    private int _cardCount = 0;
    
    public int CardCount => _cardCount;
    public bool HasWon => _cardCount >= WIN_CARD_COUNT;
    
    void Start()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }
    
    public bool AddCard(string spellName)
    {
        if (string.IsNullOrEmpty(spellName))
        {
            Debug.LogWarning("[CardCollectionManager] 卡牌名称为空，无法添加");
            return false;
        }
        
        if (_collectedCards.Contains(spellName))
        {
            Debug.Log($"[CardCollectionManager] 卡牌 '{spellName}' 已收集，跳过");
            return false;
        }
        
        _collectedCards.Add(spellName);
        _cardCount++;
        
            Debug.Log($"[CardCollectionManager] 收集到新卡牌: {spellName} (总计: {_cardCount}/{WIN_CARD_COUNT})");
            
            OnCardCollected?.Invoke(_cardCount);
            
            if (_cardCount >= WIN_CARD_COUNT)
            {
                Debug.Log($"[CardCollectionManager] 收集到 {WIN_CARD_COUNT} 张卡牌！通关！");
                OnAllCardsCollected?.Invoke();
                ShowWinPanel();
            }
        
        return true;
    }
    
    private void ShowWinPanel()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            
            Text winText = winPanel.GetComponentInChildren<Text>();
            if (winText != null)
            {
                winText.text = "通关！";
                Debug.Log("[CardCollectionManager] 设置通关文字");
            }
            else
            {
                CreateWinText();
            }
            
            Debug.Log("[CardCollectionManager] 显示通关界面");
            
            // 3秒后自动隐藏
            HideWinPanelAfterDelay(3f).Forget();
        }
        else
        {
            Debug.LogWarning("[CardCollectionManager] WinPanel未设置，无法显示通关界面");
        }
    }
    
    private async UniTaskVoid HideWinPanelAfterDelay(float delaySeconds)
    {
        await UniTask.Delay((int)(delaySeconds * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
        
        if (winPanel != null)
        {
            winPanel.SetActive(false);
            Debug.Log("[CardCollectionManager] 通关界面已自动隐藏");
        }
    }
    
    private void CreateWinText()
    {
        if (winPanel == null) return;
        
        GameObject textObj = new GameObject("WinText");
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.SetParent(winPanel.transform, false);
        
        textRect.anchorMin = new Vector2(0f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.sizeDelta = new Vector2(0f, 100f);
        textRect.anchoredPosition = Vector2.zero;
        
        Text winText = textObj.AddComponent<Text>();
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        winText.font = defaultFont;
        winText.text = "通关！";
        winText.fontSize = 72;
        winText.alignment = TextAnchor.MiddleCenter;
        winText.color = Color.yellow;
        winText.fontStyle = FontStyle.Bold;
        
        Debug.Log("[CardCollectionManager] 创建通关文字");
    }
    
    public void ResetCollection()
    {
        _collectedCards.Clear();
        _cardCount = 0;
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        Debug.Log("[CardCollectionManager] 卡牌收集已重置");
    }
    
    public List<string> GetCollectedCards()
    {
        return new List<string>(_collectedCards);
    }
}
