using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace uOSC
{
    public class ReproduceClient : MonoBehaviour
    {
        [SerializeField]
        GameObject uiManager;

        [SerializeField]
        Text ObjSize;
        [SerializeField]
        Text plotNums;

        SettingManager setting;
        LogPanelManager logPanelManager;

        uOscClient client, client1, client2;

        List<DataStorage> dataStorages = new List<DataStorage>();

        //データ読み込みは1度だけにする
        bool intial = true;
        //送信用floatリスト
        List<object> sendStorage = new List<object>();

        int length_bit;
        private void Awake()
        {

        }

        //計測準備+データ読み出し
        public void InitSetting()
        {
            // ログマネージャー取得
            logPanelManager = uiManager.GetComponent<LogPanelManager>();

            setting = uiManager.GetComponent<SettingManager>();
            setting.InitParam4repro();
            client = this.gameObject.AddComponent<uOscClient>();
            client.Address();
            client1 = this.gameObject.AddComponent<uOscClient>();
            client1.Address1();
            client2 = this.gameObject.AddComponent<uOscClient>();
            client2.Address2();
            SettingManager.objSize = float.Parse(ObjSize.text);
            SettingManager.plotNumber = int.Parse(plotNums.text);
            length_bit = (int)(Mathf.Log(IPportGetter.SampleLength, 2f));

            StartCoroutine("DataRead");
        }
        public void SharingStarter()
        {
            //計測用HoloからユニキャストでデータをもらうとマルチキャストでHolo(観測側)に基本設定を送信し計測を始める
                        StartCoroutine("DataSend");
        }
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private IEnumerator DataRead()
        {
            logPanelManager.Writelog(SettingManager.saveDirPath);

            if (intial)
            {
                //録音＆マイク位置バイナリファイル保存
                for (int dataIndex = 0; dataIndex < SettingManager.plotNumber; dataIndex++)
                {
                    string Filteredname = SettingManager.saveDirPath + @"\measurepoint_" + (dataIndex + 1).ToString() + ".bytes";
                    DataStorage data = new DataStorage();
                    //FileStream fs = new FileStream(Filteredname, FileMode.Open);
                    if (File.Exists(Filteredname))
                    {
                        using (BinaryReader br = new BinaryReader(File.Open(Filteredname, FileMode.Open)))
                        {

                            data.soundSignal = new double[4][];
                            for (int micID = 0; micID < 4; micID++)
                            {
                                data.soundSignal[micID] = new double[IPportGetter.SampleLength];
                                for (int sample = 0; sample < IPportGetter.SampleLength; sample++)
                                {
                                    data.soundSignal[micID][sample] = br.ReadDouble();
                                }
                            }
                            float vx = (float)br.ReadDouble();
                            float vy = (float)br.ReadDouble();
                            float vz = (float)br.ReadDouble();
                            data.micLocalPos = new Vector3(vx, vy, vz);
                            float rx = (float)br.ReadDouble();
                            float ry = (float)br.ReadDouble();
                            float rz = (float)br.ReadDouble();
                            float rw = (float)br.ReadDouble();
                            data.micLocalRot = new Quaternion(rx, ry, rz, rw);
                            br.Close();
                            //  fs.Close();
                        }
                    }
                    data.measureNo = dataIndex;
                    data.intensityDir = AcousticMathNew.CrossSpectrumMethod(data.soundSignal, IPportGetter.SampleRate, length_bit, SettingManager.freqMin, SettingManager.freqMax, SettingManager.atomDensity, SettingManager.micInterval);
                    var level = AcousticMathNew.CalcuIntensityLevel(data.intensityDir);
                    dataStorages.Add(data);
                    Debug.Log("read data:" + dataIndex);
                    if((dataIndex + 1)%10 == 0)
                        logPanelManager.Writelog("read data:" + dataIndex);
                    yield return null;
                }
                logPanelManager.Writelog("Data reading is finished!");
                intial = false;
                yield return null;
            }
            else
            {
                logPanelManager.Writelog("already read data");
            }
            client.Send("SharingStart", SettingManager.colormapID, SettingManager.lvMin, SettingManager.lvMax, SettingManager.objSize);
            client1.Send("SharingStart", SettingManager.colormapID, SettingManager.lvMin, SettingManager.lvMax, SettingManager.objSize);
            client2.Send("SharingStart", SettingManager.colormapID, SettingManager.lvMin, SettingManager.lvMax, SettingManager.objSize);

            yield return null;
        }

        private IEnumerator DataSend()
        {
            int sendNum = 0;
            //1回の送信量を減らすために24個ずつ送信
            sendStorage = new List<object>();
            //1オブジェクトずつデータを送信
            foreach (DataStorage storage in dataStorages)
            {
                if (sendNum % 24 == 0 && sendNum != 0)
                {
                    object[] sender = sendStorage.ToArray();
                    SendData("Reproduct",sender);
                    sendStorage.Clear();
                    logPanelManager.Writelog("Send data number:" + sendNum);
                }
                sendStorage.Add(storage.micLocalPos.x);
                sendStorage.Add(storage.micLocalPos.y);
                sendStorage.Add(storage.micLocalPos.z);
                sendStorage.Add(storage.micLocalRot.x);
                sendStorage.Add(storage.micLocalRot.y);
                sendStorage.Add(storage.micLocalRot.z);
                sendStorage.Add(storage.micLocalRot.w);
                sendStorage.Add(storage.intensityDir.x);
                sendStorage.Add(storage.intensityDir.y);
                sendStorage.Add(storage.intensityDir.z);
                sendStorage.Add(storage.measureNo);

                sendNum++;
                Debug.Log("store date:"+storage.measureNo);
                yield return null;
            }
            //最後に、LIST内の中途半端に残ったデータを送信
            object[] sender2 = sendStorage.ToArray();
            SendData("Reproduct", sender2);
            sendStorage.Clear();
            logPanelManager.Writelog("Send data number:" + sendNum);
            yield return null;
        }

        void SendData(string address, object[] vs)
        {
            client.Send(address, vs);
            client1.Send(address, vs);
            client2.Send(address, vs);
        }
        public void recalc()
        {
            setting.InitParam4recalc();
            StartCoroutine("RecalcuData");
        }
        private IEnumerator RecalcuData()
        {

            //送信番号
            int sendNum = 0;
            //1回の送信量を減らすために64個ずつ送信
            sendStorage = new List<object>();
            client.Send("SharingStart", SettingManager.colormapID, SettingManager.lvMin, SettingManager.lvMax, SettingManager.objSize);
            client1.Send("SharingStart", SettingManager.colormapID, SettingManager.lvMin, SettingManager.lvMax, SettingManager.objSize);
            client2.Send("SharingStart", SettingManager.colormapID, SettingManager.lvMin, SettingManager.lvMax, SettingManager.objSize);

            //1オブジェクトずつデータを送信
            foreach (DataStorage storage in dataStorages)
            {

                Vector3 intensity = AcousticMathNew.CrossSpectrumMethod(storage.soundSignal, IPportGetter.SampleRate, length_bit, SettingManager.freqMin, SettingManager.freqMax, SettingManager.atomDensity, SettingManager.micInterval);
                if (sendNum % 64 == 0 && sendNum != 0)
                {
                    object[] sender = sendStorage.ToArray();
                    SendData("ColorChange",sender);
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
            SendData("ColorChange", sender2);
            sendStorage.Clear();
            logPanelManager.Writelog("Send data number:" + sendNum);
            yield return null;
        }
    }
}