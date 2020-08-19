using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AsioCSharpDll;
using System.Runtime.InteropServices;
using System;

public class AsioManager : MonoBehaviour {


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

    /// <summary>
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

    /// <summary>
    /// Asioを終了させる処理
    /// </summary>
    public static void DeAsio()
    {
        asiocsharpdll.StopAsioMain();
    }


    /// <summary>
    /// Asioから音圧信号(IntPtr型)を取得してdouble配列に変換して返す
    /// </summary>
    /// <param name="sampleLength">サンプル長</param>
    /// <returns>音圧信号のジャグ配列 hoge["マイクのID番号"]["サンプル"]</returns>
    public static double[][] GetAsioSoundSignals(int sampleLength)
    {
        //Asioから取得してくるIntPtr型の音圧信号配列
        IntPtr[] ptrSoundSignals = new IntPtr[4];
        for (int micID = 0; micID < 4; micID++)
        {
            ptrSoundSignals[micID] = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int)) * sampleLength);
        }
        //AsioからIntPtr型を取得
        asiocsharpdll.GetFourSoundSignal(ptrSoundSignals[0], ptrSoundSignals[1], ptrSoundSignals[2], ptrSoundSignals[3]);

        //一時的に保管するint型の音圧信号配列
        int[][] tempSoundSignals = new int[4][];
        //出力するdouble型の音圧信号配列
        double[][] outSoundSignals = new double[4][];

        for (int micID = 0; micID < 4; micID++)
        {
            tempSoundSignals[micID] = new int[sampleLength];
            outSoundSignals[micID] = new double[sampleLength];

            //IntPtr -> int
            Marshal.Copy(ptrSoundSignals[micID], tempSoundSignals[micID], 0, sampleLength);

            for (int sample = 0; sample < sampleLength; sample++)
            {
                //キャリブレーションを反映するとしたらここ
                //outputSoundSignals[micID][sample] = (double)calibValue[0] * (double)tempIntMic0[sample] / (double)Mathf.Pow(10, 8);
                outSoundSignals[micID][sample] = tempSoundSignals[micID][sample] / (double)Mathf.Pow(10, 9)/ 2d/InstantCalibManager.calibValue[micID];
            }
        }
        return outSoundSignals;
    }

    #region ForDebugReasion
    private void Start()
    {
        string[] asioDrivers = GetAsioDriverNames();
        foreach (string asio in asioDrivers)
        {
            Debug.Log(asio);
        }
        //サンプリング周波数とサンプル数変更候補
        PrepareAsio(1, 44100, 512);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            try
            {
                double[][] test = GetAsioSoundSignals(512);
                Debug.Log(test[0][0]);
                Debug.Log("OK");
            }
            catch
            {
                Debug.Log("だめや");
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            asiocsharpdll.StopAsioMain();
        }
    }
    #endregion
}

//ToDo : AsioDll編集すればこのクラス自体必要なくなりそう
