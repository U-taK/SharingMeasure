using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AsioCSharpDll;
using System;
using System.Runtime.InteropServices;
using System.IO;

public class RecordAsioManager : MonoBehaviour {

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

    //ヘッダサイズ(wavファイル生成用)
    const int HEADER_SIZE = 44;
    const int rescaleFactor = 32767; //to convert float to Int16

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

    /// <summary>
    /// Asioから音圧信号(IntPtr型)を取得してfloat配列に変換して返す
    /// </summary>
    /// <param name="sampleLength">サンプル長</param>
    /// <returns>音圧信号の配列 hoge["サンプル"]</returns>
    public float[] GetAsioSoundSignals(int channel, int sampleLength)
    {

        //AsioからIntPtr型を取得
        asiocsharpdll.GetFourSoundSignal(ptrSoundSignals[0], ptrSoundSignals[1], ptrSoundSignals[2], ptrSoundSignals[3]);
       

        //一時的に保管するint型の音圧信号配列
        int[] tempSoundSignals = new int[sampleLength];
        //出力するdouble型の音圧信号配列
        float[] outSoundSignals = new float[sampleLength];

       
            //IntPtr -> int
            Marshal.Copy(ptrSoundSignals[channel], tempSoundSignals, 0, sampleLength);


        for (int sample = 0; sample < sampleLength; sample++)
        {
                //キャリブレーションを反映するとしたらここ
                //outputSoundSignals[micID][sample] = (double)calibValue[0] * (double)tempIntMic0[sample] / (double)Mathf.Pow(10, 8);
                outSoundSignals[sample] = tempSoundSignals[sample] / Mathf.Pow(10, 8);
        }

        return outSoundSignals;
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
            StartCoroutine(WavRecoding(1, 120, samplingFrequency, sampleNum, path));
        if (Input.GetKeyDown(KeyCode.Q))
            isRecording = false;
	}


    public IEnumerator WavRecoding(int micNumName, int MaxRecordingTime, int samplingFrequency, int samplingLength, string filePath)
    {

        yield return new WaitForSeconds(samplingLength / samplingFrequency);
        Debug.Log("WavFileManager Recording Start");

        //WavFile準備
        if (!filePath.ToLower().EndsWith(".wav"))
        {
            filePath += ".wav";
        }
        Debug.Log("File path: " + filePath);

        //ディレクトリ作成
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        //Fileストリーム
        fileStream = new FileStream(filePath, FileMode.Create);

        //Head領域を事前に確保
        byte headerByte = new byte();
        for(int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(headerByte);
        }


        //Buffer
        microphoneBuffers = new float[MaxRecordingTime * samplingFrequency];

        //Rcording開始
        isRecording = true;

        //録音開始
        rep = 0;

        do
        {
            Debug.Log(DateTime.Now);
            var tempSound = GetAsioSoundSignals(micNumName, sampleNum);
            yield return null;
            var startTime = DateTime.Now;
            Array.Copy(tempSound, 0, microphoneBuffers, sampleNum * rep, sampleNum);

            //yield return new WaitForSeconds(samplingLength / samplingFrequency);
            rep++;
            TimeSpan timeSpan = DateTime.Now - startTime;
            Debug.Log("Record time is :" + timeSpan.Milliseconds);
            yield return null;
        } while (isRecording && sampleNum*rep < samplingFrequency*MaxRecordingTime);

        for (int i = 0; i < samplingLength * rep-1; i++)
        {
            Byte[] _buffer = BitConverter.GetBytes((short)(microphoneBuffers[i] * rescaleFactor));
            fileStream.Write(_buffer, 0, 2);
        }

        WavHeaderWrite(fileStream, rep, samplingFrequency);

        yield return null;

        Debug.Log("Stop recording");

        yield return null;
    }

    private void WavHeaderWrite(FileStream _fileStream, int rep, int samplingFrequency)
    {

        //サンプリング数を計算
        var samples = ((int)_fileStream.Length - HEADER_SIZE) / 2;

        //おまじない
        _fileStream.Flush();
        _fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        _fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(_fileStream.Length - 8);
        _fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        _fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        _fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        _fileStream.Write(subChunk1, 0, 4);

        //UInt16 _two = 2;
        UInt16 _one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(_one);
        _fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(1);
        _fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(samplingFrequency);
        _fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(samplingFrequency * 1 * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        _fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(1 * 2);
        _fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        _fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        _fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(sampleNum * rep * 2);
        _fileStream.Write(subChunk2, 0, 4);

        //必ずクローズ
        _fileStream.Flush();
        _fileStream.Close();
    }
}
