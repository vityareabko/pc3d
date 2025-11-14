using UnityEngine;

public enum IngredientState
{
    World,     
    InCauldron 
}

public interface IIngredient
{
    GameObject gameObject { get; }
    IngredientState state { get; set; }
    PotionIngredientData data { get; }
}

public class PotionIngredient : MonoBehaviour, IIngredient
{
    [field: SerializeField] public PotionIngredientData data { get; private set; }
    public IngredientState state { get; set; }
    public Transform gameObject => gameObject;

}