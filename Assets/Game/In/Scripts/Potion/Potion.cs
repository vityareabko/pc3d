using UnityEngine;

public interface IPotion
{
    PotionType type { get; }
    PotionData potionData {get;}
}

public class Potion : MonoBehaviour, IPotion
{
    [field: SerializeField] public PotionType type { get; private set; }
    public PotionData potionData { get; private set; }

    public Rigidbody rb;
    
    public void Initialize(PotionData data) => potionData = data;
}
