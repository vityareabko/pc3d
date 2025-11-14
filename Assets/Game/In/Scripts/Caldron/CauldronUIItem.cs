using UnityEngine;
using UnityEngine.UI;

public class CauldronUIItem : MonoBehaviour
{
    [SerializeField] private Image img;
    public IngredientType type { get; private set; }

    public void Initialize(IngredientType t, Sprite s)
    {
        Debug.Log(s.name);
        type = t;
        img.sprite = s;
    }
}