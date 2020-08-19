using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeleteGetter : MonoBehaviour {
    InputField delete;

    public static int DeleteNum = 0;

	// Use this for initialization
	void Start () {
        delete = GetComponent<InputField>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void CatchNum()
    {
        DeleteNum = int.Parse(delete.text.ToString());
    }
}
