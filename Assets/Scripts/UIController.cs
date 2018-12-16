using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour {

    // Inventory variables
    public GameObject inventory;
    private Image sprite;

    // Health bar variables
    public GameObject healthbar, energybar;
    public Text healthText;
    private Vector2 healthbarSize, healthbarPos, energybarSize;
    private Color energybarColor;
    

    private MainCharController player;

	// Use this for initialization
	void Start () {
        // Initialize references
        sprite = inventory.GetComponent<Image>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<MainCharController>();
        healthbarSize = healthbar.GetComponent<RectTransform>().sizeDelta;
        healthbarPos = healthbar.GetComponent<RectTransform>().localPosition;
        energybarSize = energybar.GetComponent<RectTransform>().sizeDelta;
        energybarColor = energybar.GetComponent<Image>().color;

        // Hide image on start
        sprite.color = Color.clear;
    }
	
	// Update is called once per frame
	void Update () {

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

        Debug.Log(energybarSize.y);

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
}
