using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstantCalibManager : MonoBehaviour {

    public static double[] calibValue = new double[4] { 1, 1, 1, 1 };

    [SerializeField]
    InputField[] calibs = new InputField[4];
	// Use this for initialization
	void Start () {
        for (int i = 0; i < calibValue.Length; i++)
        {
            calibs[i].text = calibValue[i].ToString();
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for(int i = 0; i < calibValue.Length; i++)
            Debug.Log("Calib Value [" + i + "]" + calibValue[i]);
        }
	}

    public void UpdateCal()
    {
        for(int i = 0; i < calibValue.Length; i++)
        {
            calibValue[i] = double.Parse(calibs[i].text);
        }
    }
}
