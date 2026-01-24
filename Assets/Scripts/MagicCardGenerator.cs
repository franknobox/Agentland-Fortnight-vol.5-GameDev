using System;
using Cysharp.Threading.Tasks;
using PlayKit_SDK;
using UnityEngine;
using UnityEngine.UI;

public class MagicCardGenerator : MonoBehaviour
{
    private const string IMAGE_MODEL = "flux-1-schnell";
    private const string IMAGE_SIZE = "1024x1024";
    
    [Header("UI设置")]
    [Tooltip("卡片父容器（Canvas下的Panel）")]
    public Transform cardParent;
    
    [Tooltip("卡片预制体（可选，如果为空则动态创建）")]
    public GameObject cardPrefab;
    
    [Header("卡牌收集")]
    [Tooltip("卡牌收集管理器")]
    public CardCollectionManager cardCollectionManager;
    
    private PlayKit_AIImageClient _imageClient;
    private bool _isInitialized = false;
    
    private async void Start()
    {
        await InitializeAsync();
    }
    
    private async UniTask InitializeAsync()
    {
        if (_isInitialized) return;
        
        try
        {
            await PlayKitSDK.InitializeAsync();
            
            _imageClient = PlayKitSDK.Factory.CreateImageClient(IMAGE_MODEL);
            if (_imageClient == null)
            {
                Debug.LogError("[MagicCardGenerator] 创建图像客户端失败");
                return;
            }
            
            _isInitialized = true;
            Debug.Log($"[MagicCardGenerator] 初始化完成，使用模型: {IMAGE_MODEL}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MagicCardGenerator] 初始化异常: {ex.Message}");
        }
    }
    
    public async UniTask GenerateCardAsync(string transcript, string spellName)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[MagicCardGenerator] 未初始化，等待初始化完成");
            await InitializeAsync();
            if (!_isInitialized)
            {
                Debug.LogError("[MagicCardGenerator] 初始化失败，无法生成卡片");
                return;
            }
        }
        
        if (string.IsNullOrEmpty(transcript))
        {
            Debug.LogWarning("[MagicCardGenerator] 转写文本为空，无法生成卡片");
            return;
        }
        
        try
        {
            Debug.Log($"[MagicCardGenerator] 开始生成魔法卡片 - 文本: {transcript}, 魔法名: {spellName}");
            
            string prompt = BuildImagePrompt(transcript, spellName);
            Debug.Log($"[MagicCardGenerator] 图像生成提示词: {prompt}");
            
            var generatedImage = await _imageClient.GenerateImageAsync(
                prompt,
                IMAGE_SIZE,
                null,
                this.GetCancellationTokenOnDestroy()
            );
            
            if (generatedImage == null)
            {
                Debug.LogError("[MagicCardGenerator] 图像生成失败");
                return;
            }
            
        Debug.Log("[MagicCardGenerator] 图像生成成功，创建UI卡片");
        CreateCardUI(spellName, generatedImage);
        
        if (cardCollectionManager != null)
        {
            cardCollectionManager.AddCard(spellName);
        }
        else
        {
            Debug.LogWarning("[MagicCardGenerator] CardCollectionManager未设置，无法记录卡牌");
        }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MagicCardGenerator] 生成卡片异常: {ex.Message}");
        }
    }
    
    private string BuildImagePrompt(string transcript, string spellName)
    {
        string spellDescription = GetSpellDescription(spellName);
        return $"A magical spell card illustration, {spellDescription}, inspired by the text '{transcript}'. Fantasy art style, detailed, mystical atmosphere, card game aesthetic, centered composition";
    }
    
    private string GetSpellDescription(string spellId)
    {
        if (string.IsNullOrEmpty(spellId)) return "mystical magic";
        
        switch (spellId.ToLower())
        {
            case "fire": return "fire magic with flames and embers";
            case "frozen": return "ice magic with frost and snowflakes";
            case "potions": return "magical potions and alchemy";
            case "attack": return "combat magic with energy slashes";
            case "book": return "ancient spellbook with glowing runes";
            case "magic circle": return "magical circle with arcane symbols";
            case "coin": return "golden coins and treasure";
            case "explode": return "explosive magic with energy bursts";
            case "lightening": return "lightning magic with electric sparks";
            case "air": return "wind magic with swirling air currents";
            default: return "mystical magic energy";
        }
    }
    
    private void CreateCardUI(string spellName, PlayKit_GeneratedImage generatedImage)
    {
        GameObject cardObj;
        
        if (cardPrefab != null)
        {
            cardObj = Instantiate(cardPrefab);
        }
        else
        {
            cardObj = CreateCardFromScratch();
        }
        
        if (cardParent != null)
        {
            cardObj.transform.SetParent(cardParent, false);
        }
        else
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                cardObj.transform.SetParent(canvas.transform, false);
            }
        }
        
        RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }
        
        Image[] images = cardObj.GetComponentsInChildren<Image>();
        Image cardImage = null;
        
        if (cardPrefab != null)
        {
            foreach (Image img in images)
            {
                if (img.gameObject.name.Contains("Image") || img.gameObject.name.Contains("CardImage"))
                {
                    cardImage = img;
                    break;
                }
            }
            if (cardImage == null && images.Length > 1)
            {
                cardImage = images[1];
            }
        }
        else
        {
            GameObject imageObj = cardObj.transform.Find("CardImage")?.gameObject;
            if (imageObj != null)
            {
                cardImage = imageObj.GetComponent<Image>();
            }
        }
        
        if (cardImage != null)
        {
            Sprite sprite = generatedImage.ToSprite();
            if (sprite != null)
            {
                cardImage.sprite = sprite;
                Debug.Log("[MagicCardGenerator] 卡片图像已设置");
            }
        }
        else
        {
            Debug.LogWarning("[MagicCardGenerator] 未找到卡片图像组件");
        }
        
        Text nameText = cardObj.GetComponentInChildren<Text>();
        if (nameText != null && !string.IsNullOrEmpty(spellName))
        {
            nameText.text = spellName;
            Debug.Log($"[MagicCardGenerator] 卡片名称已设置: {spellName}");
        }
        
        Debug.Log("[MagicCardGenerator] 魔法卡片创建完成");
        
        HideCardAfterDelay(cardObj, 3f).Forget();
    }
    
    private async UniTaskVoid HideCardAfterDelay(GameObject cardObj, float delaySeconds)
    {
        if (cardObj == null) return;
        
        await UniTask.Delay((int)(delaySeconds * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
        
        if (cardObj != null)
        {
            cardObj.SetActive(false);
            Debug.Log("[MagicCardGenerator] 卡牌已隐藏");
        }
    }
    
    private GameObject CreateCardFromScratch()
    {
        GameObject card = new GameObject("MagicCard");
        
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(400, 600);
        
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
        
        GameObject imageObj = new GameObject("CardImage");
        RectTransform imageRect = imageObj.AddComponent<RectTransform>();
        imageRect.SetParent(cardRect, false);
        imageRect.anchorMin = new Vector2(0.1f, 0.2f);
        imageRect.anchorMax = new Vector2(0.9f, 0.8f);
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;
        
        Image cardImage = imageObj.AddComponent<Image>();
        cardImage.preserveAspect = true;
        
        GameObject nameObj = new GameObject("CardName");
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.SetParent(cardRect, false);
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0.2f);
        nameRect.sizeDelta = Vector2.zero;
        nameRect.anchoredPosition = Vector2.zero;
        
        Text nameText = nameObj.AddComponent<Text>();
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        nameText.font = defaultFont;
        nameText.fontSize = 24;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.white;
        
        return card;
    }
}
