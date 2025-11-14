using System;
using UnityEngine;

public class UI : MonoBehaviour
{
    public UIHud hud;
    
    private void Awake()
    {
        G.main.ui = this;
    }
}