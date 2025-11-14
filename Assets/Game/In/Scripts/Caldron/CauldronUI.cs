using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CauldronUI : MonoBehaviour
{
    public Transform parentBar;
    public TMP_Text _textClue;
    
    [Header("Potion")]
    public Image potionFrame;
    public Image potionImage;
    public TMP_Text potionText;

    public Image _potionCookFillAmount;
    
    private List<CauldronUIItem> _items = new();
    
    public void AddItem(IIngredient ingredient)
    {
        var item = G.fabric.GetCauldronIngredientUIItem(ingredient, parentBar);
        _items.Add(item);
    }
    
    public void RemoveItem(IngredientType type)
    {
        var item = _items.Find(x => x.type == type);
        if (item == null) return;
        _items.Remove(item);
        Destroy(item.gameObject);
    }

    public void Clear()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
            if (_items[i] != null)
                Destroy(_items[i].gameObject);
        
        _items.Clear();
    }

    public void SetPotionCookFillAmount(float v)
    {
        Debug.Log(v);
        _potionCookFillAmount.fillAmount = v;
    }

    public void ShowPotionFrame(bool s)
    {
        potionFrame.gameObject.SetActive(s);
        _textClue.gameObject.SetActive(false);
    }

    public void ShowInfoReadyToCook(PotionData potionData)
    {
        ShowPotionFrame(true);
        potionImage.sprite = potionData.sprite;
        potionText.text = potionData.name;
        
        _textClue.gameObject.SetActive(true);
    }
    
}