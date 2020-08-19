using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IIntensityTester : MonoBehaviour {

    // プレハブ
    public GameObject prefab;
    // サンプル数の2の乗数
    int length_bit;

    static int sampleLength = 4096;
    int sampleRate = 44100;

    //マイクロホン間隔
    public float micInterval = 0.05f;

    //大気密度[kg/m^3]
    public float atmDensity = 1.2f;

    float[] sendData;


    private void Awake()
    {
        // サンプル数の2の乗数を計算
        length_bit = (int)(Mathf.Log(sampleLength, 2f));

        //ドライバー名デバッグ
        string[] asioDriverIDNames = AsioManager.GetAsioDriverNames();
        foreach (string asioDriverIDName in asioDriverIDNames)
        {
            Debug.Log(asioDriverIDName);
        }

        //ASIOスタート //localは1 ドライバー選択可能に
        string instLog = AsioManager.PrepareAsio(2, sampleRate, sampleLength);
    }
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.P))
            RecordIntensity();
	}

    void RecordIntensity()
    {
        var soundSignal = AsioManager.GetAsioSoundSignals(sampleLength);

        var intensityDirection = AcousticSI.DirectMethod(soundSignal, atmDensity, micInterval);
        float[] intensityLv = new float[intensityDirection.Length];
        for(int i = 0; i < intensityDirection.Length; i++)
        {
            intensityLv[i] = AcousticMathNew.CalcuIntensityLevel(intensityDirection[i]);
        }
        var intensityObj = Instantiate(prefab); ;
        var parameter = intensityObj.AddComponent<ParameterStorage>();
        parameter.PutIntensity(intensityDirection, intensityLv);
    }
}
