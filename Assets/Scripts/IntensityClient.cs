using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace uOSC
{

    [RequireComponent(typeof(uOscClient))]
    public class IntensityClient : MonoBehaviour {

        //マイクロホン間隔
        [SerializeField]
        float interval = 0.05f;
        [SerializeField]
        static int sampleLength = 4096;

        [SerializeField]//周波数帯域の下限
        float freq_range_min;
        [SerializeField]//周波数帯域の上限
        float freq_range_max;
        [SerializeField]//気温[℃]
        float tempreture;
        [SerializeField]//気圧[hPa]
        float atm;
        [SerializeField]//計算間隔
        float measureSpan;

        int sampleRate;//サンプリング周波数

        [SerializeField]
        KeyCode recTrigger = KeyCode.Mouse0;

        //大気密度[kg/m^3]
        float atmDensity;

        //録音された音圧信号[マイクID][サンプル]
        double[][] soundSignals;

        // サンプル数の2の乗数
        int length_bit;

        uOscClient client;

        //ログ表示
        public Text log;
        List<string> logList = new List<string>();
        int i = 0;
        string instLog;
        public Text logIntensity;

        bool beMeasure = true;

        void Awake()
        {
            // サンプル数の2の乗数を計算
            length_bit = (int)(Mathf.Log(sampleLength, 2f));

            //ドライバー名デバッグ
            string[] asioDriverIDNames = AsioManager.GetAsioDriverNames();
            foreach (string asioDriverIDName in asioDriverIDNames)
            {
                Debug.Log(asioDriverIDName);
                Writelog(asioDriverIDName);
            }

            //ASIOスタート //localは1 ドライバー選択可能に
            sampleRate = IPportGetter.SampleRate;
            instLog = AsioManager.PrepareAsio(2,sampleRate, sampleLength);
            Writelog(instLog);


        }
        public void InitSetting(string micInterval, string freqMax, string freqMin, string temp, string airPressure, string span)
        {
            client = GetComponent<uOscClient>();
            if (!float.TryParse(micInterval, out interval))
                interval = 0.05f;
            if (!float.TryParse(freqMax, out freq_range_max))
                freq_range_max = 1414f;
            if (!float.TryParse(freqMin, out freq_range_min))
                freq_range_min = 707f;
            tempreture = float.Parse(temp);
            atm = float.Parse(airPressure);
            if (!float.TryParse(span, out measureSpan))
                measureSpan = 0.5f;
            CalculateAtmDensity();

            StartCoroutine(UpdateData());
        }



        IEnumerator UpdateData()
        {
            yield return new WaitForSeconds(1f);
            Writelog("Start record");
            while (beMeasure)
            {
                soundSignals = AsioManager.GetAsioSoundSignals(sampleLength);

                Vector3 IntensityDirection = AcousticMathNew.CrossSpectrumMethod(soundSignals, sampleRate, length_bit, freq_range_min, freq_range_max, atmDensity, interval);
                float intensityLevel_dB = AcousticMathNew.CalcuIntensityLevel(IntensityDirection);
                //  Debug.Log(soundSignals[0][i]);
                //  Debug.Log(soundSignals[1][i]);
                //  Debug.Log(soundSignals[2][i]);
                //  Debug.Log(soundSignals[3][i]);

                Debug.Log(intensityLevel_dB);
                Debug.Log(IntensityDirection.x);
                Debug.Log(IntensityDirection.y);
                Debug.Log(IntensityDirection.z);
                WriteConsole(IntensityDirection.x, IntensityDirection.y, IntensityDirection.z, intensityLevel_dB);
                client.Send("", IntensityDirection.x, IntensityDirection.y, IntensityDirection.z, intensityLevel_dB);
                yield return new WaitForSeconds(measureSpan);
            }
        }

        public void StopMeasure()
        {
            if (beMeasure)
                beMeasure = false;
            else
            {
                beMeasure = true;
                StartCoroutine(UpdateData());
            }
        }

        void CalculateAtmDensity()
        {
            //大気密度の計算法:ρ=P/{R(t+273.15)} ただしRは乾燥空気の気体定数2.87としている
            atmDensity = atm / (2.87f * (tempreture + 273.15f));
        }

        public void Writelog(string logElem)
        {
            if (logElem != null)
            {
                logList.Add(logElem);
                switch (logList.Count())
                {
                    case 1:
                        log.text = logList[0];
                        break;
                    case 2:
                        log.text = logList[1] + "\n" + logList[0];
                        break;
                    default:
                        log.text = logList[logList.Count() - 1] + "\n" + logList[logList.Count() - 2] + "\n" + logList[logList.Count() - 3];
                        break;

                }
            }
        }

        public void Writelog()
        {
            switch (logList.Count() - i)
            {
                case 1:
                    log.text = logList[0];
                    break;
                case 2:
                    log.text = logList[1] + "\n" + logList[0];
                    break;
                default:
                    log.text = logList[logList.Count() - i - 1] + "\n" + logList[logList.Count() - i - 2] + "\n" + logList[logList.Count() - i - 3];
                    break;

            }
        }

        public void DownLog()
        {
            if (logList.Count() - i > 3)
                i += 3;
            Writelog();
        }

        public void UpLog()
        {
            if (i > 0)
                i -= 3;
            Writelog();
        }


        void WriteConsole(float intensityX, float intensityY, float intensityZ, float intensityLevel)
        {
            logIntensity.text = "Intensity Data is \n x: " + intensityX.ToString("F12") +
                                "\n y: " + intensityY.ToString("F12") +
                                "\n z: " + intensityZ.ToString("F12") +
                                "\n Intensity level: " + intensityLevel.ToString("f6") + "[dB]";
        }
    }
}
