using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AddInCar : MonoBehaviour
{
    // Start is called before the first frame update
    public int buyingNum; //¡ ∂Rº∆∂q
    public InputField buyingNumInput;
    public Button buyingNumPlus;
    public Button buyingNumMinus;
    public Button addInCart;
    public GameObject InfoPanel;

    private void Awake()
    {
        buyingNum = Int32.Parse(buyingNumInput.text); 

    }
    private void Update()
    {
        buyingNum = Int32.Parse(buyingNumInput.text);
    }

    


}
