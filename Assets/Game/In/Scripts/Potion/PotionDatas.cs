using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionDatas", menuName = "Database/PotionDB")]
public class PotionDatas : ScriptableObject
{
    public List<PotionData> potions; // зелья и его рецепт
    public List<PotionIngredientData> potionIngredientDatas; // ингредиенты для крафта зелья
    
    public Sprite GetIngredientSpriteByType(IIngredient ingredient)
    {
        foreach (var item in G.main.potionDatas.potionIngredientDatas)
            if (item.type == ingredient.data.type)
                return item.sprite;

        return null;
    }

    public Potion GetPotionPrefabByType(PotionType type)
    {
        foreach (var p in potions)
        {
            if (p.type == type)
                return p.prefab;
        }

        return null;
    }

}

public enum PotionType
{
    Health,
    Stamina,
    Mana,
    Speed,
    Power,
}

public enum PotionQuality
{
    Common,
    Rare,
    Epic,
    Legendary,
    Mystical,
}

public enum IngredientType
{
    CapsicumGreen,
    CapsicumOrange,
    Mushroom,
    Potato,
    Onion,
    Chicken,
    Tomato,
}

[System.Serializable]
public class PotionData
{
    public PotionType type;
    public PotionQuality quality;
    public List<IngredientType> ingredients = new();
    public Sprite sprite;
    public string name;
    public int price;
    public int cyclePerCooking;
    public Potion prefab;
}

[System.Serializable]
public class PotionIngredientData
{
    public IngredientType type;
    public Sprite sprite;
    public List<Vector2> trajectory;
}