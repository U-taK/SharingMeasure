using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace uOSC
{

    [RequireComponent(typeof(uOscServer))]
    public class Server : MonoBehaviour
    {

        //計測時間サンプル数
        int sNum = 0;

        //計測データ保存用
        List<string> transformData = new List<string>();

        //PC版生成用プレハブ
        [SerializeField]
        GameObject Cone;
        //空間基準マーカレプリカ
        [SerializeField]
        GameObject copyStandard;
        [SerializeField]
        SettingManager setting;
        [SerializeField]
        GameObject uiManager;

        //仮想オブジェクト間隔
        float objectInterbal;
        //格子点計測精度
        public int accuracy = 100;

        //座標送信時間,送信された座標情報
        string sendTime;
        DateTime pastTime = DateTime.Now;
        Vector3 pastPos = Vector3.zero;
        Vector3 sendPos, Speed;
        float speedf;
        Quaternion sendRot;
        //受信データの管理番号
        int i;
        //送信データの管理番号
        int j = 0;
        List<DataStorage> dataStorages = new List<DataStorage>();

        IntensityClientVer2 ver2;
        LogPanelManager logPanelManager;


        Vector3 intensity;
        double[][] soundSignals;

        List<float> speeds = new List<float>();

        //遅延時間の計測
        Dictionary<int, DateTime> delaytime = new Dictionary<int, DateTime>();
        public List<float> delays = new List<float>();


        //送信用floatリスト
        List<object> sendStorage = new List<object>();

        //開始ボタンで計測開始
        public void InitServer()
        {
            // ログマネージャー取得
            logPanelManager = uiManager.GetComponent<LogPanelManager>();

            //サーバ立ち上げ
            setting.ServerSetting(out objectInterbal);
            var server = GetComponent<uOscServer>();
            server.onDataReceived.AddListener(OnDataReceived);
            ver2 = GetComponent<IntensityClientVer2>();
            Debug.Log("Init setting");
            logPanelManager.Writelog("Init setting");
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                SaveDelay();
            }
        }
        void OnDataReceived(Message message)
        {
            //計測結果取得
            // address
            var msg = message.address;
            Debug.Log("catch address: " + msg);
            // timestamp
            //msg += "(" + message.timestamp.ToLocalTime() + ") ";
            if (msg == "PositionSender")
            {
                // values
                foreach (var value in message.values)
                {
                    transformData.Add(value.ToString());
                }
                sendTime = transformData[i];
                sendPos = new Vector3(float.Parse(transformData[i + 1]), float.Parse(transformData[i + 2]), float.Parse(transformData[i + 3]));
                sendRot = new Quaternion(float.Parse(transformData[i + 4]), float.Parse(transformData[i + 5]), float.Parse(transformData[i + 6]), float.Parse(transformData[i + 7]));
                //送信受信時間確認
                //  Debug.Log("Send Time is: " + sendTime + "Receive Time is:" + DateTime.Now.ToString("MM/ss/HH/mm/ss.fff") +
                //      " ,(" + sendPos.x.ToString() + ", " + sendPos.y.ToString() + ", " + sendPos.z.ToString() + ")");

                //速度保存
                //CheckSpeed(sendPos, DateTime.Parse(sendTime));

                //１フレーム毎の座標と音圧情報を取得
                //SaveInstanceBinaryData(sendPos, sendRot);
                Vector3 fPos = sendPos * accuracy / 100f;
                if (CheckPlotDistance(fPos, dataStorages, objectInterbal))
                {
                    //計測結果＋データ送信
                    GameObject micPoint = new GameObject("measurementPoint" + j);
                    micPoint.transform.parent = copyStandard.transform;
                    micPoint.transform.localPosition = sendPos;
                    micPoint.transform.localRotation = sendRot;
                    ver2.SendData(sendPos, sendRot, out soundSignals, out intensity);
                    DataStorage data = new DataStorage(j, sendPos, sendRot, soundSignals, intensity);
                    dataStorages.Add(data);
                   // delaytime.Add(j, DateTime.Parse(sendTime));
                    j++;
                }
                i += 8;
            }
            else if (msg == "SettingSender")
            {
                Debug.Log("Setting end in Holo");
                logPanelManager.Writelog("Setting end in Holo");
                SettingManager.colormapID = int.Parse(message.values[0].ToString());
                SettingManager.lvMin = float.Parse(message.values[1].ToString());
                SettingManager.lvMax = float.Parse(message.values[2].ToString());
                SettingManager.objSize = float.Parse(message.values[3].ToString());
                ver2.SharingStarter();
            }
            else if (msg == "DelaySend")
            {
                var postTime = delaytime[int.Parse(message.values[0].ToString())];
                //遅延時間計算
                var sec = (float)(DateTime.Parse(message.values[1].ToString()) - postTime).TotalSeconds/2f;
                delays.Add(sec);
            }

        }

        public void DeleteDataStorage()
        {
            dataStorages.RemoveAt(j-1);
            ver2.DeleteNum(j-1);
            j--;
        }
        public void recalc()
        {
            setting.InitParam4recalc();
            ver2.SharingStarter();
            StartCoroutine("RecalcuData");
        }

        private IEnumerator RecalcuData()
        {
            int length_bit = (int)(Mathf.Log(IPportGetter.SampleLength, 2f));
            //送信番号
            int sendNum = 0;
            //1回の送信量を減らすために64個ずつ送信
            sendStorage = new List<object>();
            //1オブジェクトずつデータを送信
            foreach (DataStorage storage in dataStorages)
            {
                
                Vector3 intensity = AcousticMathNew.CrossSpectrumMethod(storage.soundSignal, IPportGetter.SampleRate,length_bit , SettingManager.freqMin, SettingManager.freqMax, SettingManager.atomDensity, SettingManager.micInterval);
                if (sendNum % 64 == 0 && sendNum != 0)
                {
                    object[] sender = sendStorage.ToArray();
                    ver2.SendFrequencyChange(sender);
                    sendStorage.Clear();
                    logPanelManager.Writelog("Send data number:" + sendNum);
                }

                sendStorage.Add(intensity.x);
                sendStorage.Add(intensity.y);
                sendStorage.Add(intensity.z);

                //sendStorage.Add((float)sendNum);
                sendStorage.Add((float)sendNum);
                sendNum++;
                Debug.Log("store date");
                yield return null;
            }
            //最後に、LIST内の中途半端に残ったデータを送信
            object[] sender2 = sendStorage.ToArray();
            ver2.SendFrequencyChange(sender2);
            sendStorage.Clear();
            logPanelManager.Writelog("Send data number:" + sendNum);
            yield return null;
        }



        //ほかのプロットと距離が離れているかチェック
        //当均等に配置するために精度要素を追加
        bool CheckPlotDistance(Vector3 realtimeLocalPosition, List<DataStorage> dataList, float settingDistance)
        {
            float accDistance = settingDistance;
            int plotNum = dataList.Count;
            //一個目だったらtrue
            if (plotNum == 0)
            {
                return true;
            }
            else
            {
                for (int index = 0; index < plotNum; index++)
                {
                    if (!dataList[index].CheckPlotDistance(realtimeLocalPosition, settingDistance))
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// バイナリデータセーブ
        /// </summary>
        public void SaveBinaryData()
        {
            StartCoroutine("Save");
           
        }
        private IEnumerator Save()
        {
            Debug.Log(SettingManager.saveDirPath);
            //ディレクトリなかったら作成
            SafeCreateDirectory(SettingManager.saveDirPath);

            //録音＆マイク位置バイナリファイル保存
            for (int dataIndex = 0; dataIndex < dataStorages.Count; dataIndex++)
            {
                string Filteredname = SettingManager.saveDirPath + @"\measurepoint_" + (dataIndex + 1).ToString() + ".bytes";
                FileStream fs = new FileStream(Filteredname, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);

                for (int micID = 0; micID < 4; micID++)
                {
                    for (int sample = 0; sample < dataStorages[dataIndex].soundSignal[micID].Length; sample++)
                    {
                        bw.Write(dataStorages[dataIndex].soundSignal[micID][sample]);
                    }
                }

                bw.Write((double)dataStorages[dataIndex].micLocalPos.x);
                bw.Write((double)dataStorages[dataIndex].micLocalPos.y);
                bw.Write((double)dataStorages[dataIndex].micLocalPos.z);

                bw.Write((double)dataStorages[dataIndex].micLocalRot.x);
                bw.Write((double)dataStorages[dataIndex].micLocalRot.y);
                bw.Write((double)dataStorages[dataIndex].micLocalRot.z);
                bw.Write((double)dataStorages[dataIndex].micLocalRot.w);

                bw.Close();
                fs.Close();
                if((dataIndex+1 % 10) == 0)
                    logPanelManager.Writelog("Save data number:" + dataIndex + 1);
                yield return null;
            }
            SettingManager.plotNumber = dataStorages.Count;
            setting.SettingSave();
            yield return null;
        }

        /// <summary>
        /// 1サンプルごとのデータをバイナリファイルに取得していく
        /// </summary>
        public void SaveInstanceBinaryData(Vector3 sendPos, Quaternion sendRot)
        {
            //ディレクトリなかったら作成
            SafeCreateDirectory(SettingManager.saveDirPath);
            //もう一つディレクトリを作成
            var savePath = SettingManager.saveDirPath + @"\instance";
            SafeCreateDirectory(savePath);

            //録音＆マイク位置バイナリファイル保存
                string Filteredname = savePath + @"\instancepoint_" + (sNum++).ToString() + ".bytes";
                FileStream fs = new FileStream(Filteredname, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);


            var soundSignal = AsioManager.GetAsioSoundSignals(256);

            for (int micID = 0; micID < 4; micID++)
                {
                    for (int sample = 0; sample < 4096; sample++)
                    {
                        bw.Write(soundSignal[micID][sample]);
                    }
                }

                bw.Write((double)sendPos.x);
                bw.Write((double)sendPos.y);
                bw.Write((double)sendPos.z);

                bw.Write((double)sendRot.x);
                bw.Write((double)sendRot.y);
                bw.Write((double)sendRot.z);
                bw.Write((double)sendRot.w);

                bw.Close();
                fs.Close();
        }
        


        /// <summary>
        /// 指定したパスにディレクトリが存在しない場合
        /// すべてのディレクトリとサブディレクトリを作成します
        /// </summary>
        public static DirectoryInfo SafeCreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return null;
            }
            return Directory.CreateDirectory(path);
        }

        ///<summary>
        ///速度調査
        /// </summary>
        public void CheckSpeed(Vector3 PrePos, DateTime now)
        {
            var sec = (float)(now - pastTime).TotalSeconds;
            Speed = ((PrePos - pastPos) / sec);
            speedf = Speed.magnitude * 3.6f;
            Debug.Log("Mic Speed is" + speedf + "km/h" + sec);
            pastPos = PrePos;
            pastTime = now;
            if (speedf != 0)
            {
                speeds.Add(speedf);
            }
        }

        ///<summary>
        //速度リストをCSVファイルで保存
        /// </summary>
        public void SaveSpeed()
        {
            //ディレクトリなかったら作成
            SafeCreateDirectory(SettingManager.saveDirPath);
            string Filteredname = SettingManager.saveDirPath + @"\speed.csv";
            FileStream fs = new FileStream(Filteredname, FileMode.Create);
            StreamWriter bw = new StreamWriter(fs);
            foreach (var Speeds in speeds)
            {
                bw.WriteLine(Speeds.ToString());
            }
            bw.Close();
            fs.Close();
            Debug.Log("speed saving finish");
        }

        ///<summary>
        //遅延時間リストをCSVファイルで保存
        /// </summary>
        public void SaveDelay()
        {
            //ディレクトリなかったら作成
            SafeCreateDirectory(SettingManager.saveDirPath);
            string Filteredname = SettingManager.saveDirPath + @"\delay.csv";
            FileStream fs = new FileStream(Filteredname, FileMode.Create);
            StreamWriter bw = new StreamWriter(fs);
            foreach (var Delays in delays)
            {
                bw.WriteLine(Delays.ToString());
            }
            bw.Close();
            fs.Close();
            Debug.Log("speed saving finish");
        }
    }
}