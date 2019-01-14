using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitlescreenController : MonoBehaviour {


    public Button startBtn, creditsBtn;

	// Use this for initialization
	void Start () {
        startBtn.onClick.AddListener(StartBtnClick);
        creditsBtn.onClick.AddListener(CreditsBtnClick);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void StartBtnClick()
    {
        SceneManager.LoadScene("Level 1", LoadSceneMode.Single);
    }

    void CreditsBtnClick()
    {

    }
}
