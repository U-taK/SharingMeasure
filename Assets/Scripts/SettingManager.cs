using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.IO;

namespace uOSC
{
    public class SettingManager : MonoBehaviour
    {

        uOscClient oscClient;
        IntensityClient intensityClient;

        //Setting
        //IPアドレス
        // public InputField iPaddress;
        //ポート番号
        // public InputField port;
        //マイクロホン間隔
        public InputField MicInterval;
        int samplingRate;
        //分析周波数
        public InputField FreqMax;
        public InputField FreqMin;
        //気温
        public InputField Temperature;
        //大気圧
        public InputField Atm;
        //計算スパン
        public InputField Span;
        //保存ディレクトリ
        public InputField SaveDirectory;

        //インテンシティレベル表記
        public InputField LVMax, LVMin;

        public static float micInterval;
        public static float freqMin;
        public static float freqMax;
        public static float temperature;
        public static float atm;
        public static float objInterval;
        public static string saveDirPath;
        public static int colormapID = 2;//0がgrayscale,1がparula,2がjet
        public static float lvMin = 60;
        public static float lvMax = 105;
        public static float objSize = 0.05f;
        public static float atomDensity = 1.02f;
        //保存用、計測点数の総数をdataファイルに入れられるようにする
        public static int plotNumber;

        // Use this for initialization
        void Start()
        {
           
        }

        public void InitParam()
        {
            oscClient = gameObject.GetComponent<uOscClient>();
            intensityClient = gameObject.GetComponent<IntensityClient>();
            //IPアドレスとポート番号指定
           // oscClient.GetIP(iPaddress.text);
           // oscClient.GetPort(port.text);
           
            intensityClient.InitSetting(MicInterval.text, FreqMax.text, FreqMin.text, Temperature.text, Atm.text, Span.text);
            return;
        }

        public void InitParam4Sharing(out float mInterval, out float fMax, out float fMin, out float atmDensity)
        {
            saveDirPath = SaveDirectory.text;
            if(!float.TryParse(MicInterval.text, out micInterval))
                micInterval = 0.05f;
            if (!float.TryParse(FreqMax.text, out freqMax))
                freqMax = 1414f;
            if (!float.TryParse(FreqMin.text, out freqMin))
                freqMin = 707f;
            temperature = float.Parse(Temperature.text);
            atm = float.Parse(Atm.text);
            atmDensity = CalculateAtmDensity(atm, temperature);
            //atmDensity = CalculateAtmDensity(temperature, atm);
            mInterval = micInterval;
            fMax = freqMax;
            fMin = freqMin;
            return;
        }

        public void InitParam4recalc()
        {
            saveDirPath = SaveDirectory.text;
            if (!float.TryParse(MicInterval.text, out micInterval))
                micInterval = 0.05f;
            if (!float.TryParse(FreqMax.text, out freqMax))
                freqMax = 1414f;
            if (!float.TryParse(FreqMin.text, out freqMin))
                freqMin = 707f;
            float.TryParse(LVMax.text, out lvMax);
            float.TryParse(LVMin.text, out lvMin);
            temperature = float.Parse(Temperature.text);
            atm = float.Parse(Atm.text);
            atomDensity = CalculateAtmDensity(atm, temperature);
            
            return;
        }

        public void InitParam4repro()
        {
            saveDirPath = SaveDirectory.text;
            if (!float.TryParse(MicInterval.text, out micInterval))
                micInterval = 0.05f;
            if (!float.TryParse(FreqMax.text, out freqMax))
                freqMax = 1414f;
            if (!float.TryParse(FreqMin.text, out freqMin))
                freqMin = 707f;
            float.TryParse(LVMax.text, out lvMax);
            float.TryParse(LVMin.text, out lvMin);
            temperature = float.Parse(Temperature.text);
            atm = float.Parse(Atm.text);
            atomDensity = CalculateAtmDensity(atm, temperature);

            return;
        }

        public void ServerSetting(out float oInterval)
        {
            if (!float.TryParse(Span.text, out objInterval))
                objInterval = 0.5f;
            oInterval = objInterval;
        }
        float CalculateAtmDensity(float atm, float temp)
        {
            //大気密度の計算法:ρ=P/{R(t+273.15)} ただしRは乾燥空気の気体定数2.87としている
            return atm / (2.87f * (temp + 273.15f));
        }

        public void SettingSave()
        {
            //設定値メモ保存
                string settingTxtPath = saveDirPath + @"\setting.txt";
                StreamWriter settingSW = new StreamWriter(settingTxtPath, false, System.Text.Encoding.GetEncoding("shift_jis"));
            //    settingSW.WriteLine("MeasurePointNum : " + dataStorages.Count.ToString());
                settingSW.WriteLine("sampleRate : " + IPportGetter.SampleRate);
                settingSW.WriteLine("sampleLength : " + IPportGetter.SampleLength);
                settingSW.WriteLine("freqRange : " + FreqMin.text + " - " + FreqMax.text);
                settingSW.WriteLine("Mic size : " +MicInterval.text);
                settingSW.WriteLine("atmPressure : " + Atm.text);
                settingSW.WriteLine("temperature : " + Temperature.text);
                settingSW.WriteLine("Measure point interval : " + objInterval.ToString());
                settingSW.WriteLine("Color ID :" + colormapID.ToString());
                settingSW.WriteLine("Level gain :" + LVMin.text + " - " + LVMax.text);
                settingSW.WriteLine("The number of plot :" + plotNumber);
                settingSW.Close();
            return;
        }
    }
}
