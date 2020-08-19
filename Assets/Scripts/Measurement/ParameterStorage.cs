using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParameterStorage : MonoBehaviour {

    //計算した音響インテンシティの値を座標ごとに保持しておく
    Vector3[] soundIntensity;
    float[] intensityLevel;
    int i= 0;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.O))
        {
            ParameterChange();
            CallIntensity();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            Vector3 sum = Vector3.zero;
            for(int count = 0; count < soundIntensity.Length; count++)
            {
                sum += soundIntensity[count];
            }
            var sumLevel = AcousticMathNew.CalcuIntensityLevel(sum);
            Debug.Log("Sound intensity of average is " + sumLevel);
            transform.localRotation = Quaternion.LookRotation(sum * 10000000000);
            transform.localScale = new Vector3(1f, 1f, 4f);
            Color vecObjColor = ColorBar.DefineColor(1, sumLevel, 70f, 85f);
            gameObject.GetComponent<Renderer>().material.color = vecObjColor;
        }

	}

    public void PutIntensity(Vector3[] intensity, float[] intensityLv)
    {
        soundIntensity = intensity;
        intensityLevel = intensityLv;
    }

    public void CallIntensity()
    {
        Debug.Log("No." + i + "intensity is" + soundIntensity[i] + ",intensityLevel is" + intensityLevel[i]);
    i++;

    }

    void ParameterChange()
    {
        transform.localRotation = Quaternion.LookRotation(soundIntensity[i] * 10000000000);
        transform.localScale = new Vector3(1f, 1f, 4f);
        Color vecObjColor = ColorBar.DefineColor(1, intensityLevel[i], 65f, 105f);
        gameObject.GetComponent<Renderer>().material.color = vecObjColor;
    }
}
