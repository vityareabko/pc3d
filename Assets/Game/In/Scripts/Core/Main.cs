using UnityEngine;

[DefaultExecutionOrder(-99999)]
public class Main : MonoBehaviour
{
    public PotionDatas potionDatas { get; set; }

    public Cauldron cauldron { get; set; }
    public Player player { get; set; }
    public UI ui {get; set; }

    public void Awake()
    {
        G.main = this;
        
        potionDatas = ResourceService.Instance.Load<PotionDatas>("PotionDatas");
    }
}