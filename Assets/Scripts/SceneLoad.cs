using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour {


    Scene scene;
	// Use this for initialization
	void Start () {
        scene = SceneManager.GetActiveScene();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void Reload()
    {
        SceneManager.LoadScene(scene.name);
    }
}
