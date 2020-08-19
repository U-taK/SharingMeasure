using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AsioCSharpDll;
using System;
using System.Runtime.InteropServices;
using System.IO;

public class RecordAsioManager2 : MonoBehaviour {

    //From c++ Dll
    [DllImport("AsioCppDll")]
    public static extern void StartAsioRecord(string fileName, int time);
    [DllImport("AsioCppDll")]
    public static extern void StopAsioRecord();
    int rep = 0;
    private float[] microphoneBuffers;
    private FileStream fileStream;
    private int head;
    private int position;
    public bool isRecording;
    private bool isFinish;
    private IntPtr[] ptrSoundSignals = new IntPtr[4];

    //保存先
    public string path = @"C:\Users\acoust\Desktop\test\rec1.wav";
    //計測時間[s]
    public int recordTime = 1800;

    //サンプリング周波数
    public int samplingFrequency = 44100;

    //サンプル数
    public int sampleNum = 4096;


    /// <summary>
    /// 使えるASIOドライバー名を取得(IntPtr型) -> IDと一緒にString型に変換して返す
    /// 使用可能なドライバー名をメモする(New)
    /// </summary>
    /// <param name="asioDriverSum">ドライバーの総数</param>
    /// <returns></returns>
    public static string[] GetAsioDriverNames()
    {
        //ASIOドライバーの数を取得
        int asioDriverSum = asiocsharpdll.GetAsioDriverSum();
        //ドライバー数の分だけ名前を格納する配列を用意
        string[] outputAsioDrivers = new string[asioDriverSum];

        for (int asioDriverID = 0; asioDriverID < asioDriverSum; asioDriverID++)
        {
            IntPtr ptrAsioName = asiocsharpdll.GetAsioDriverNames(asioDriverID);
            string tempAsioDriverName = Marshal.PtrToStringAnsi(ptrAsioName);
            outputAsioDrivers[asioDriverID] = asioDriverID.ToString() + ": " + tempAsioDriverName;
        }
        return outputAsioDrivers;
    }

    // <summary>
    /// ASIOを起動する関数
    /// </summary>
    public static string PrepareAsio(int asioDriverID, int sampleRate, int sampleLength)
    {
        asiocsharpdll.SelectAsioDriver(asioDriverID);
        asiocsharpdll.ConfigSampleRateLength(sampleRate, sampleLength);
        if (asiocsharpdll.StartAsioMain())
        {
            Debug.Log("Asio start");
            return "Asio start";
        }
        else Debug.Log("Asio Startできなかった");
        return "Asio Startできなかった";
    }



    // Use this for initialization
    void Awake () {
        string[] asioDrivers = GetAsioDriverNames();
        foreach (string asio in asioDrivers)
        {
            Debug.Log(asio);
        }
        //テスト
        //サンプリング周波数とサンプル数変更候補
        PrepareAsio(2, samplingFrequency, sampleNum);
        //Asioから取得してくるIntPtr型の音圧信号配列
        
        for (int micID = 0; micID < 4; micID++)
        {
            ptrSoundSignals[micID] = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int)) * sampleNum);
        }

    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.S))
        {
            StartAsioRecord(path, recordTime);
            Debug.Log("Record Start");
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            StopAsioRecord();
            Debug.Log("Record End");
        }
	}

    private void OnDestroy()
    {
        Debug.Log("Application finished");
        asiocsharpdll.StopAsioMain();
    }

}
