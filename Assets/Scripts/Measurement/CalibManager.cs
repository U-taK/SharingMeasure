using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CalibManager : MonoBehaviour  {

    //録音時間
    [SerializeField]
    InputField SampleNum;

    //録音ボタン
    [SerializeField]
    Button rButton;

    // 各マイクの音圧と音圧レベルをメモするボタン
    [SerializeField]
    private KeyCode[] noteButton = new KeyCode[4]
    {
        KeyCode.Alpha0, KeyCode.Alpha1 , KeyCode.Alpha2 , KeyCode.Alpha3
    };

    // noteした4つの値をバイナリとして記録するボタン
    [SerializeField]
    private KeyCode saveBytesButton = KeyCode.S;


    // Hierarcy内のオブジェクトを当てはめる
    //[SerializeField]
    //private LineRenderer[] lines = new LineRenderer[4];
    //[SerializeField]
    //private Text[] recTexts = new Text[4];
    //[SerializeField]
    //private Text[] noteTexts = new Text[4];

    // 設定値
    int sampleRate = 16000;
    int sampleLength = 4096;

    // 更新される音圧、音圧レベル
    float[] recSP = new float[4];
    float[] recSPL = new float[4];
    //記録される音圧、音圧レベル
    float[] noteSP = new float[4];
    float[] noteSPL = new float[4];

    //録音された音圧信号 [マイクID][サンプル]
    double[][] soundSignals;

    bool onRec = false;

    private void Start()
    {
        rButton.interactable = false;
    }
    public void CalibReady()
    {
        //ドライバー名デバッグ
        string[] asioDriverIDNames = AsioManager.GetAsioDriverNames();
        foreach(string asioDriverIDName in asioDriverIDNames)
        {
            Debug.Log(asioDriverIDName);
        }

        sampleLength = int.Parse(SampleNum.text);
        //ASIOスタート
        //string beStart = AsioManager.PrepareAsio(3,sampleRate,sampleLength);
        string beStart = AsioManager.PrepareAsio(0, sampleRate, sampleLength);
        //string beStart = AsioManager.PrepareAsio(1, sampleRate, sampleLength);
        if (beStart == "Asio start")
        {
            rButton.interactable = true;
            onRec = true;
        }
    }

    private void OnDestroy()
    {
        
        Debug.Log("Return home scene");
        if (onRec)
            AsioManager.DeAsio();
    }

    public void Record()
    {
        rButton.interactable = false;
        soundSignals = AsioManager.GetAsioSoundSignals(sampleLength);

        Debug.Log("Save Bytes Data");
        SaveBinaryData(soundSignals, Application.streamingAssetsPath);
        rButton.interactable = true;
    }


    public void Record2()
    {
            soundSignals = AsioManager.GetAsioSoundSignals(sampleLength);
            //Vector3 IntensityDirection = AcousticMathNew.CrossSpectrumMethod(soundSignals,sampleRate, 12,353f,707f,1000.4f,0.05f);
            //float intensityLevel_dB = AcousticMathNew.CalcuIntensityLevel(IntensityDirection);
            //Debug.Log(intensityLevel_dB);
            for (int micID = 0; micID < 4; micID++)
            {
                //グラフ書く
               // DrowLineGraph(soundSignals[micID], lines[micID]);
                //音圧レベルを出す
                recSP[micID] = GetSoundPressure(soundSignals[micID]);
                recSPL[micID] = GetSoundPressureLevel(recSP[micID]);
                // recTexts[micID].text = string.Format("SP: {0}\nSPL: {1}", recSP[micID], recSPL[micID]);
                Debug.Log(string.Format("SP: {0}\nSPL: {1}", recSP[micID], recSPL[micID]));
            }
        

        //recSPLをメモ
        for(int micId = 0; micId < 4; micId++)
        {
            if (Input.GetKeyDown(noteButton[micId]))
            {
                SetRecSPSPL(micId);
            }
        }

        // バイナリ保存
        if (Input.GetKeyDown(saveBytesButton))
        {
            Debug.Log("Save Bytes Data");
            SaveBinaryData(soundSignals, @"C:\Users\acoust\Desktop");
        }
    }

    /// <summary>
    /// LineRendererにグラフを書く
    /// </summary>
    /// <param name="inputArray">入力配列</param>
    /// <param name="ioLine">変更するLineRenderer</param>
    void DrowLineGraph(double[] inputArray, LineRenderer ioLine)
    {
        int arrLength = inputArray.Length;
        ioLine.SetVertexCount(arrLength - 1);
        float initialX = -arrLength / 2.0f;
        for (int arrIndex = 1; arrIndex < arrLength; arrIndex++)
        {
            ioLine.SetPosition(arrIndex - 1, new Vector3(initialX, 500.0f * (float)inputArray[arrIndex], 0));
            initialX += 1.0f;
        }
    }

    /// <summary>
    /// 音圧信号から音圧(実効値 = √1/N * sigma(o, N-1)n^2))を計算
    /// </summary>
    /// <param name="soundSignal">入力信号</param>
    float GetSoundPressure(double[] soundSignal)
    {
        float sumSquaredSp = 0;
        foreach(double sp in soundSignal)
        {
            sumSquaredSp += Mathf.Pow((float)sp, 2);
        }
        float effectiveSP = Mathf.Sqrt( sumSquaredSp / soundSignal.Length );

        return effectiveSP;
    }

    /// <summary>
    /// 音圧から音圧レベルを計算
    /// </summary>
    float GetSoundPressureLevel(float soundPressure)
    {
        float spl = 20 * Mathf.Log10(soundPressure / 0.00002f);
        return spl;
    }

    /// <summary>
    /// realtimeSPとSPLの値をrecSPとSPLにコピー
    /// </summary>
    void SetRecSPSPL(int micID)
    {
        noteSP[micID] = recSP[micID];
        noteSPL[micID] = recSPL[micID];
       // noteTexts[micID].text = string.Format("SP: {0}\nSPL: {1}", noteSP[micID], noteSPL[micID]);
    }

    /// <summary>
    /// バイナリデータセーブ
    /// </summary>
    void SaveBinaryData(double[][] soundSignals, string saveDirPath)
    {
        //ディレクトリなかったら作成
        SafeCreateDirectory(saveDirPath);

        //録音＆マイク位置バイナリファイル保存
        string Filteredname = saveDirPath + @"\CalibValues.bytes";
        FileStream fs = new FileStream(Filteredname, FileMode.Create);
        BinaryWriter bw = new BinaryWriter(fs);
        for (int micId = 0; micId < 4; micId++)
        {
            for (int sample = 0; sample < sampleLength; sample++)
            {
                bw.Write(soundSignals[micId][sample]);
            }
        }
        bw.Close();
        fs.Close();
    }

    /// <summary>
    /// 指定したパスにディレクトリが存在しない場合
    /// すべてのディレクトリとサブディレクトリを作成
    /// </summary>
    public static DirectoryInfo SafeCreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            return null;
        }
        return Directory.CreateDirectory(path);
    }
}
