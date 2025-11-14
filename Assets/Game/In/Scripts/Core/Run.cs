

using System.Collections.Generic;

public class Run
{
    public List<PotionData> unblockedPotions = new();
    public List<PotionData> potionsInStock = new();

    public PotionData pendingPotion;
    
    public Run()
    {
        G.run = this;
        
        // test
        var i1 = G.main.potionDatas.potions[0];
        unblockedPotions.Add(i1);
    }

}