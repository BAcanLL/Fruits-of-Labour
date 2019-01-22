using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour {

    // Inventory variables
    public GameObject currentItem, inventoryPanel;
    private GameObject itemHolder;
    private int selection = 0, invRows, invColumns;
    private List<GameObject> holders = new List<GameObject>();
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
        sprite = currentItem.GetComponent<Image>();
        sprite.color = Color.clear;

        // Initialize inventory
        itemHolder = (GameObject)Resources.Load("Item Holder");
        inventoryPanel.SetActive(false);
        invColumns = Mathf.FloorToInt(inventoryPanel.GetComponent<RectTransform>().sizeDelta.x / (inventoryPanel.GetComponent<GridLayoutGroup>().cellSize.x + inventoryPanel.GetComponent<GridLayoutGroup>().spacing.x));
        invRows = Mathf.FloorToInt(inventoryPanel.GetComponent<RectTransform>().sizeDelta.y / (inventoryPanel.GetComponent<GridLayoutGroup>().cellSize.y + inventoryPanel.GetComponent<GridLayoutGroup>().spacing.y));

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
                Pause(true, true);
            }
            else
            {
                Pause(false, false);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if(!inventoryPanel.activeSelf)
            {
                OpenInventory();
            }
            else
            {
                CloseInventory();
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

        // Update inventory
        InventoryUpdate();
	}

    //----------------
    // Button methods
    //----------------

    void ResumeBtnPress()
    {
        Pause(false, false);
    }

    void RestartBtnPress()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        Pause(false, false);
    }

    //---------------
    // Other methods
    //---------------

    private void Pause(bool pause, bool menu)
    {
        Time.timeScale = pause ? 0 : 1;
        paused = pause;
        pauseMenu.SetActive(menu);
    }

    private void OpenInventory()
    {
        Pause(true, false);

        // Populate the panel with images of the inventory items
        for (int i = 0; i < MainCharController.INVENTORY_SIZE; i++)
        {
            Item itm = (Item)player.Inventory[i, 0];

            if (itm != null)
            {
                GameObject invItem = Instantiate(itemHolder);
                invItem.transform.SetParent(inventoryPanel.transform);
                invItem.GetComponent<Image>().enabled = false;
                invItem.GetComponentsInChildren<Image>()[1].sprite = Resources.Load<Sprite>(itm.SpriteName);
                invItem.GetComponentInChildren<Text>().text = ((int)player.Inventory[i, 1]).ToString();

                holders.Add(invItem);

                //selection = 0;
            }
        }

        inventoryPanel.SetActive(true);
    }

    private void CloseInventory()
    {
        inventoryPanel.SetActive(false);

        foreach (GameObject holder in holders)
            Destroy(holder);

        holders.Clear();

        Pause(false, false);
    }

    private void EnableHolder(int index, bool on)
    {
        holders[index].GetComponent<Image>().enabled = on;
    }

    private void InventoryUpdate()
    {
        if(inventoryPanel.activeSelf && player.NumItems > 0)
        {
            // Highlight the selected item
            EnableHolder(selection, true);

            // Select through the inventory
            if(Input.GetKeyDown(KeyCode.RightArrow))
            {
                if((selection + 1) % invColumns != 0 && (selection + 1) < player.NumItems)
                {
                    EnableHolder(selection, false);
                    selection++;
                }
            }
            else if(Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if ((selection + 1) % invColumns != 1)
                {
                    EnableHolder(selection, false);
                    selection--;
                }
            }
            else if(Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (selection - 8 >= 0)
                {
                    EnableHolder(selection, false);
                    selection -= 8;
                }
            }
            else if(Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (selection + 8 < player.NumItems)
                {
                    EnableHolder(selection, false);
                    selection += 8;
                }
            }

            // Choose the selected item with <Q>
            if (Input.GetKeyDown(KeyCode.Q))
            {
                player.CurrentWeapon = (Weapon)player.Inventory[selection, 0];
                player.audioSource[1].PlayOneShot(player.pickupSFX, 1f);

                CloseInventory();
            }
        }
    }
}
