using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour {

    // Inventory variables
    public GameObject inventory;
    private Image sprite;

    // Health bar variables
    public GameObject healthbar, energybar;
    public Text healthText;
    private Vector2 healthbarSize, healthbarPos, energybarSize;
    private Color energybarColor;

    // Pause variables
    public static bool paused = false;
    public GameObject pauseMenu;
    public Button resumeBtn, settingsBtn, restartBtn;

    // Player
    private MainCharController player;

	// Use this for initialization
	void Start () {
        // Initialize sprite references
        sprite = inventory.GetComponent<Image>();
        sprite.color = Color.clear;

        // Initialize player reference
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<MainCharController>();

        // Initialize healthbar
        healthbarSize = healthbar.GetComponent<RectTransform>().sizeDelta;
        healthbarPos = healthbar.GetComponent<RectTransform>().localPosition;
        energybarSize = energybar.GetComponent<RectTransform>().sizeDelta;
        energybarColor = energybar.GetComponent<Image>().color;

        // Initialize buttons
        resumeBtn.onClick.AddListener(ResumeBtnPress);
        restartBtn.onClick.AddListener(RestartBtnPress);

        // Set up pausing
        pauseMenu.SetActive(false);

    }
	
	// Update is called once per frame
	void Update () {

        // Pause the game with Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!paused)
            {
                Pause(true);
            }
            else
            {
                Pause(false);
            }

        }

        // Updaye HP bar
        float percentHP = player.Health / player.BaseHealth;
        Vector2 newSize = new Vector2(healthbarSize.x * percentHP, healthbarSize.y);
        Vector2 newPosition = healthbarPos - new Vector2(healthbarSize.x * (1 - percentHP)/2, 0);
        healthbar.GetComponent<RectTransform>().sizeDelta = newSize;
        healthbar.GetComponent<RectTransform>().localPosition = newPosition;

        // Update HP text
        healthText.text = player.Health.ToString() + "/" + player.BaseHealth.ToString();

        // Update Energy bar
        float percentEnergy = player.Energy / player.BaseEnergy;
        Vector2 newEnergySize = new Vector2(energybarSize.x * percentEnergy, energybarSize.y);
        energybar.GetComponent<RectTransform>().sizeDelta = newEnergySize;

        // Update Energy bar color
        if (player.Energy < player.DASH_COST)
            energybar.GetComponent<Image>().color = Color.red;
        else
            energybar.GetComponent<Image>().color = energybarColor;

        // Update weapon icon
        if (player.CurrentWeapon == player.DefaultWeapon)
        {
            sprite.color = Color.clear;
        }
        else
        {
            sprite.sprite = Resources.Load<Sprite>(player.CurrentWeapon.SpriteName);
            sprite.color = Color.white;
        }
	}

    //----------------
    // Button methods
    //----------------

    void ResumeBtnPress()
    {
        Pause(false);
    }

    void RestartBtnPress()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        Pause(false);
    }

    //---------------
    // Other methods
    //---------------

    private void Pause(bool pause)
    {
        Time.timeScale = pause ? 0 : 1;
        paused = pause;
        pauseMenu.SetActive(pause);
    }
}
