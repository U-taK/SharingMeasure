using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResendGetter : MonoBehaviour {
    InputField input;

    public static int ResendNum = 0;


    private void Start()
    {
        input = gameObject.GetComponent<InputField>();
    }
    public void CatchNum()
    {
        ResendNum = int.Parse(input.text);
    }
}
