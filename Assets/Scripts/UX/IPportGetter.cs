using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IPportGetter : MonoBehaviour {

    [SerializeField]
    Dropdown samplerate;
    [SerializeField]
    InputField sampleLength;
    [SerializeField]
    InputField ip1,ip2,ip3;

    public static string IP1 = "192.168.1.21", IP2 = "192.168.1.23",IP3 = "192.168.1.26";
    public static int SampleRate = 44100;
    public static int SampleLength = 4096;
    SceneManage sceneManage;
	// Use this for initialization
	void Start () {
        Debug.Log("Check");
        sceneManage = GameObject.Find("SceneManager").GetComponent<SceneManage>();
        if (SampleRate == 44100)
        {
            samplerate.value = 0;
        }
        else
        {
            samplerate.value = 1;
        }
        sampleLength.text = SampleLength.ToString();
        ip1.text = IP1;
        ip2.text = IP2;
        ip3.text = IP3;
    }


    public void Reload()
    {
        if (ip1 != null)
        {
            IP1 = ip1.text;
            IP2 = ip2.text;
            IP3 = ip3.text;
        }
        if (samplerate != null)
        {
            if (samplerate.value == 0)
            {
                SampleRate = 44100;
            }
            else
            {
                SampleRate = 48000;
            }
            SampleLength = int.Parse(sampleLength.text);
        }
        //sceneManage.MoveScene("Main");
        sceneManage.MoveScene("SharingMeasurement");
    }

    public void Load4disp()
    {
        if (ip1 != null)
        {
            IP1 = ip1.text;
            IP2 = ip2.text;
            IP3 = ip3.text;
        }
        if (samplerate != null){
            if (samplerate.value == 0)
            {
                SampleRate = 44100;
            }
            else
            {
                SampleRate = 48000;
            }
            SampleLength = int.Parse(sampleLength.text);
        }
        sceneManage.MoveScene("SharinDisplay");
    }

    public void LoadCalibScene()
    {
        if (ip1 != null)
        {
            IP1 = ip1.text;
            IP2 = ip2.text;
            IP3 = ip3.text;
        }
        if (samplerate != null)
        {
            if (samplerate.value == 0)
            {
                SampleRate = 44100;
            }
            else
            {
                SampleRate = 48000;
            }
            SampleLength = int.Parse(sampleLength.text);
        }
        sceneManage.MoveScene("CalibScene");
    }
}
