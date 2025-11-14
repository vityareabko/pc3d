using System;
using System.Collections.Generic;
using H;
using UnityEngine;
using Random = UnityEngine.Random;

public class Cauldron : MonoBehaviour
{
    public CauldronUI cauldronUI;

    public Transform potionSpawnPoint;
    
    private List<IngredientType> _inCouldron = new();
    private readonly HashSet<IIngredient> _inside = new();
    
    private PotionData pendingPotion
    {
        get => G.run.pendingPotion;
        set => G.run.pendingPotion = value;
    }
    
    private void Awake()
    {
        G.main.cauldron = this;
        EventAggregator.Subscribe<PickUpIngredient>(OnPickUpedIngredientHandle);
    }

    private void OnDestroy()
    {
        EventAggregator.Unsubscribe<PickUpIngredient>(OnPickUpedIngredientHandle);
    }
    
    private void Update()
    {
        
        // if (Input.GetKeyDown(KeyCode.Alpha1))
        // {
        //     CookPotion();
        // }
        //
        // if (Input.GetKeyDown(KeyCode.Alpha2))
        // {
        //     Clear();
        // }
    }

    public void Clear()
    {
        pendingPotion = null;
        
        foreach (var i in _inside)
            if (i != null)
                Destroy(i.gameObject);
        
        _inCouldron.Clear();
        _inside.Clear();
        cauldronUI.Clear();
    }

    public void CookPotion()
    {
        // спавн поцион из cauldron
        var potion = G.fabric.GetPotion(pendingPotion, potionSpawnPoint.position);
        ThrowPotion(potion.gameObject);
        G.run.potionsInStock.Add(pendingPotion);
        // Debug.LogError($"Ura added potion in stock {G.run.potionsInStock.Count} - {pendingPotion.type}");
        Clear();
        
        cauldronUI.ShowPotionFrame(false);
    }

    private void ThrowPotion(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        
        if (rb == null) 
            return;

        var dir = Vector3.up;

        var randomTilt = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));

        var finalDir = (dir + randomTilt).normalized;

        float throwForce = Random.Range(5f, 10f);
        float torqueForce = Random.Range(2f, 4f);

        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        
        rb.AddForce(finalDir * throwForce, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.VelocityChange);

    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.TryGetComponent(out IIngredient ingredient))
        {
            if (ingredient.state == IngredientState.InCauldron) return;
            if (!_inside.Add(ingredient)) return;
            
            ingredient.data.sprite = G.main.potionDatas.GetIngredientSpriteByType(ingredient);
            ingredient.state = IngredientState.InCauldron;
            _inCouldron.Add(ingredient.data.type);
            cauldronUI.AddItem(ingredient);
            
            FindForUnbolckedReciepe();
        }
        else
        {
            ThrowPotion(collider.gameObject);
        }
    }

    private void FindForUnbolckedReciepe()
    {
        if (G.run.unblockedPotions.Count == 0)
        {
            pendingPotion = null;
            return;
        }

        foreach (var i in G.run.unblockedPotions)
        {
            if (Utils.UnorderedEqual(i.ingredients, _inCouldron))
            {
                Debug.LogError($"Ready To Cook - {i.type}");
                pendingPotion = i;
                cauldronUI.ShowInfoReadyToCook(i);
                return;
            }
            
            pendingPotion = null;
            cauldronUI.ShowPotionFrame(false);
        }
    }

    private void OnPickUpedIngredientHandle(object sender, PickUpIngredient eventData)
    {
        if (eventData.ingredient.state == IngredientState.InCauldron)
        {
            if (_inCouldron.Contains(eventData.ingredient.data.type))
            {
                _inCouldron.Remove(eventData.ingredient.data.type);
                cauldronUI.RemoveItem(eventData.ingredient.data.type);
                eventData.ingredient.state = IngredientState.World;
                _inside.Remove(eventData.ingredient);
                FindForUnbolckedReciepe();
            }
            
        }
    }
    
}