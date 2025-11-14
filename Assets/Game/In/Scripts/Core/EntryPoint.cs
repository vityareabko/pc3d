
using H;
using UnityEngine;

public static class EntryPoint
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
        Debug.Log("[EarlyEntry] BeforeSceneLoad");

        var main = new GameObject("Main");
        main.AddComponent<Main>();

        var run = new Run();
        
    }
    
}
