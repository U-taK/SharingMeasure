using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace uOSC {
    public class IntensityClientVer2 : MonoBehaviour
    {
        // サンプル数の2の乗数
        int length_bit;
        int sampleRate;
        int sampleLength;

        //マイクロホン間隔
        float micInterval = 0.05f;
        //周波数帯域の下限
        float freq_range_min;
        //周波数帯域の上限
        float freq_range_max;
        //大気密度[kg/m^3]
        float atmDensity;

        [SerializeField]
        GameObject uiManager;

        SettingManager setting;
        LogPanelManager logMan;
        Vector3 pastInt;
        float pastLv;
        uOscClient client, client1, client2;
        float[] sendData;

        //送信側の管理番号
        int j = 0;
        //録音された音圧信号[マイクID][サンプル]
        double[][] soundSignals;
        public double[][] SoundSignals
        {
            get { return this.soundSignals; }
        }

        void Awake()
        {
            sampleLength = IPportGetter.SampleLength;
            setting = uiManager.GetComponent<SettingManager>();
            logMan = uiManager.GetComponent<LogPanelManager>();
            // サンプル数の2の乗数を計算
            length_bit = (int)(Mathf.Log(sampleLength, 2f));

            //ドライバー名デバッグ
            string[] asioDriverIDNames = AsioManager.GetAsioDriverNames();
            foreach (string asioDriverIDName in asioDriverIDNames)
            {
                Debug.Log(asioDriverIDName);
                logMan.Writelog(asioDriverIDName);
            }

            //ASIOスタート //localは1 ドライバー選択可能に
            sampleRate = IPportGetter.SampleRate;
            string instLog = AsioManager.PrepareAsio(2, sampleRate, sampleLength);
            //string instLog = AsioManager.PrepareAsio(3, sampleRate, sampleLength);
            logMan.Writelog(sampleRate.ToString());
            logMan.Writelog(sampleLength.ToString());
            logMan.Writelog(instLog);
        }

        //終了時処理(Asioを切る)
        private void OnDestroy()
        {
            AsioManager.DeAsio();
        }


        //計測準備完了(開始ボタンに付与)
        public void InitSetting()
        {
            setting.InitParam4Sharing(out micInterval, out freq_range_max, out freq_range_min, out atmDensity);
            client = this.gameObject.AddComponent<uOscClient>();
            client.Address();
            client1 = this.gameObject.AddComponent<uOscClient>();
            client1.Address1();
            client2 = this.gameObject.AddComponent<uOscClient>();
            client2.Address2();
        }

        public void SendData(Vector3 SendPos, Quaternion SendRot,out double[][] soundSignal, out Vector3 IntensityDirection)
        {
            if(client == null)
            {
                Debug.Log("client is not set!!");
                soundSignal = soundSignals;
                IntensityDirection = Vector3.zero;
                return;
            }
            else
            {
                logMan.Writelog("Start record");
                soundSignal = AsioManager.GetAsioSoundSignals(sampleLength);

                IntensityDirection = AcousticMathNew.CrossSpectrumMethod(soundSignal, sampleRate, length_bit, SettingManager.freqMin, SettingManager.freqMax, atmDensity, micInterval);
                float intensityLevel_dB = AcousticMathNew.CalcuIntensityLevel(IntensityDirection);
                client.Send("ResultSend", SendPos.x, SendPos.y, SendPos.z, SendRot.x, SendRot.y, SendRot.z, SendRot.w, IntensityDirection.x, IntensityDirection.y, IntensityDirection.z,j);
                client1.Send("ResultSend", SendPos.x, SendPos.y, SendPos.z, SendRot.x, SendRot.y, SendRot.z, SendRot.w, IntensityDirection.x, IntensityDirection.y, IntensityDirection.z, j);
                client2.Send("ResultSend", SendPos.x, SendPos.y, SendPos.z, SendRot.x, SendRot.y, SendRot.z, SendRot.w, IntensityDirection.x, IntensityDirection.y, IntensityDirection.z, j);

                logMan.WriteConsole(j,SendPos, IntensityDirection, intensityLevel_dB);
                pastInt = IntensityDirection;
                pastLv = intensityLevel_dB;
                j++;
                
    
            }
        }

        //計測データの再計算
        public void SendFrequencyChange(object[] vs)
        {
            client.Send("ColorChange", vs);
            client1.Send("ColorChange", vs);
            client2.Send("ColorChange", vs);
        }

       

        public void SharingStarter()
        {
            //計測用HoloからユニキャストでデータをもらうとマルチキャストでHolo(観測側)に基本設定を送信し計測を始める
            client.Send("SharingStart", SettingManager.colormapID, SettingManager.lvMin, SettingManager.lvMax, SettingManager.objSize);
            client1.Send("SharingStart", SettingManager.colormapID, SettingManager.lvMin, SettingManager.lvMax, SettingManager.objSize);
            client2.Send("SharingStart", SettingManager.colormapID, SettingManager.lvMin, SettingManager.lvMax, SettingManager.objSize);


        }

        public void DeleteNum(int deleteNum)
        { 
            client.Send("Delete", deleteNum);
            client1.Send("Delete", deleteNum);
            client2.Send("Delete", deleteNum);
            j--;
        }
        //デバック用
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.M))
            {
                SharingStarter();
            }
        }
    }
}
