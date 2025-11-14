using Unity.Mathematics;
using UnityEngine;

namespace H
{
    public class Fabric : MonoBehaviour
    {
        [SerializeField] private CauldronUIItem _cauldronUIItem;
        
        public void Awake()
        {
            G.fabric = this;
        }

        public CauldronUIItem GetCauldronIngredientUIItem(IIngredient ingredient, Transform parent)
        {
            var item = Instantiate(_cauldronUIItem, parent);
            item.Initialize(ingredient.data.type,ingredient.data.sprite);
            return item;
        }

        public Potion GetPotion(PotionData potionData, Vector3 pos)
        {
            var prefab = G.main.potionDatas.GetPotionPrefabByType(potionData.type);
            var p = Instantiate(prefab, pos, quaternion.identity);
            p.Initialize(potionData);
            return p;
        }
    }

}