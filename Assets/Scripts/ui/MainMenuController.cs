using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems; 

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject panelMainMenu;
    public GameObject panelNewGameDifficulty; 
    public GameObject panelRaceSetup;
    public GameObject panelGarage;
    public GameObject panelSettings;          
    public GameObject panelLoading;
    public GameObject panelLoadGame; 

    [Header("Controller Navigation (First Buttons)")]
    public GameObject mainFirstButton;
    public GameObject newGameFirstButton;
    public GameObject raceSetupFirstButton;
    public GameObject garageFirstButton; 
    public GameObject settingsFirstButton;
    public GameObject loadGameFirstButton;

    [Header("UI Elements")]
    public TextMeshProUGUI moneyText;
    public Button btnCircuit;
    
    [Header("New Game Setup")]
    public TMP_InputField saveNameInput; 

    [Header("Load Screen Elements")] 
    public Button btnSlot1;
    public TextMeshProUGUI txtSlot1;
    public Button btnSlot2;
    public TextMeshProUGUI txtSlot2;
    public Button btnSlot3;
    public TextMeshProUGUI txtSlot3;

    [Header("Settings Sliders")]
    public Slider lookSlider;
    public Slider handlingSlider;

    private float garageInputCooldown = 0f;

    void Start()
    {
        QualitySettings.vSyncCount = 0;    
        Application.targetFrameRate = 300; 

        ShowPanel(panelMainMenu, mainFirstButton);
    }

    private void Update()
    {
        if (garageInputCooldown > 0f) garageInputCooldown -= Time.deltaTime;

        // 1. 'B' BUTTON / BACKSPACE: Universal "Back" button
        if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Backspace))
        {
            if (panelNewGameDifficulty != null && panelNewGameDifficulty.activeSelf) 
                ShowPanel(panelMainMenu, mainFirstButton);
            
            else if (panelLoadGame != null && panelLoadGame.activeSelf) 
                ShowPanel(panelMainMenu, mainFirstButton);
            
            else if (panelRaceSetup != null && panelRaceSetup.activeSelf) 
                ShowPanel(panelMainMenu, mainFirstButton);
            
            else if (panelSettings != null && panelSettings.activeSelf) 
                BackToMainMenuFromSettings();
            
            else if (panelGarage != null && panelGarage.activeSelf) 
                ShowPanel(panelRaceSetup, raceSetupFirstButton);
        }

        // 2. GARAGE CAR SWITCHING (Strictly D-Pad and Bumpers ONLY)
        if (panelGarage != null && panelGarage.activeSelf && garageInputCooldown <= 0f)
        {
            float dPadInput = 0f;

            // Safely read ONLY the D-Pad
            try { dPadInput = Input.GetAxisRaw("DPad_Horizontal"); } catch { } 

            // Swiped Right OR Pressed Right Bumper (R1/RB) -> Next Car
            if (dPadInput > 0.5f || Input.GetKeyDown(KeyCode.JoystickButton5)) 
            {
                GarageManager gm = GetComponent<GarageManager>();
                if (gm != null) gm.NextCar(); 
                
                garageInputCooldown = 0.25f; // Wait a quarter-second
            }
            // Swiped Left OR Pressed Left Bumper (L1/LB) -> Previous Car
            else if (dPadInput < -0.5f || Input.GetKeyDown(KeyCode.JoystickButton4))
            {
                GarageManager gm = GetComponent<GarageManager>();
                if (gm != null) gm.PreviousCar(); 
                
                garageInputCooldown = 0.25f;
            }
        }
    }

    // --- PANEL MANAGER WITH CONTROLLER SUPPORT ---
    private void ShowPanel(GameObject panelToShow, GameObject buttonToHighlight = null)
    {
        if(panelMainMenu) panelMainMenu.SetActive(false);
        if(panelNewGameDifficulty) panelNewGameDifficulty.SetActive(false);
        if(panelRaceSetup) panelRaceSetup.SetActive(false);
        if(panelGarage) panelGarage.SetActive(false);
        if(panelSettings) panelSettings.SetActive(false);
        if(panelLoading) panelLoading.SetActive(false);
        if(panelLoadGame) panelLoadGame.SetActive(false); 

        if(panelToShow) panelToShow.SetActive(true);

        EventSystem.current.SetSelectedGameObject(null);
        if (buttonToHighlight != null)
        {
            EventSystem.current.SetSelectedGameObject(buttonToHighlight);
        }
    }

    public void OnClick_NewGame()
    {
        if (saveNameInput != null) saveNameInput.text = ""; 
        ShowPanel(panelNewGameDifficulty, newGameFirstButton);
    }

    public void OnClick_LoadGame()
    {
        RefreshLoadSlot(1, btnSlot1, txtSlot1);
        RefreshLoadSlot(2, btnSlot2, txtSlot2);
        RefreshLoadSlot(3, btnSlot3, txtSlot3);
        
        ShowPanel(panelLoadGame, loadGameFirstButton); 
    }

    private void RefreshLoadSlot(int slotID, Button btn, TextMeshProUGUI txt)
    {
        if (btn == null || txt == null) return;

        if (SaveManager.DoesSaveExist(slotID))
        {
            btn.interactable = true;
            PlayerData data = SaveManager.LoadGame(slotID);
            string displayName = string.IsNullOrEmpty(data.saveProfileName) ? "SAVE SLOT " + slotID : data.saveProfileName;
            txt.text = $"{displayName.ToUpper()}\n<size=80%>CASH: ${data.totalMoney:N0}</size>";
        }
        else
        {
            btn.interactable = false;
            txt.text = $"SAVE SLOT {slotID}\n<size=80%>- EMPTY -</size>";
        }
    }

    public void OnClick_LoadSpecificSlot(int slotID)
    {
        GameManager.Instance.currentSaveSlot = slotID;
        GameManager.Instance.currentPlayerProfile = SaveManager.LoadGame(slotID);
        OpenRaceSetup();
    }

    public void OnClick_DeleteSlot(int slotID)
    {
        SaveManager.DeleteGame(slotID);
        OnClick_LoadGame(); 
    }

    public void FinalizeNewGameWithDifficulty(int selectedDifficulty)
    {
        int emptySlot = 1;
        if (!SaveManager.DoesSaveExist(1)) emptySlot = 1;
        else if (!SaveManager.DoesSaveExist(2)) emptySlot = 2;
        else if (!SaveManager.DoesSaveExist(3)) emptySlot = 3;
        else 
        {
            Debug.LogWarning("All slots full! Overwriting Slot 1 as failsafe.");
            emptySlot = 1; 
        }

        GameManager.Instance.currentSaveSlot = emptySlot;
        GameManager.Instance.currentPlayerProfile = new PlayerData();
        GameManager.Instance.currentPlayerProfile.difficulty = selectedDifficulty;

        if (saveNameInput != null && !string.IsNullOrEmpty(saveNameInput.text))
        {
            GameManager.Instance.currentPlayerProfile.saveProfileName = saveNameInput.text;
        }
        else
        {
            GameManager.Instance.currentPlayerProfile.saveProfileName = "Racer_" + Random.Range(100, 999); 
        }

        GameManager.Instance.SaveCurrentProfile(); 
        OpenRaceSetup();
    }

    public void OnClick_Settings()
    {
        if (GameManager.Instance.currentPlayerProfile != null)
        {
            lookSlider.value = GameManager.Instance.currentPlayerProfile.lookSensitivity;
            handlingSlider.value = GameManager.Instance.currentPlayerProfile.handlingSensitivity;
        }
        ShowPanel(panelSettings, settingsFirstButton);
    }

    public void OnClick_SplitScreen()
    {
        GameManager.Instance.isSplitScreen = true;
        OpenRaceSetup();
    }

    public void OnClick_Quit()
    {
        Application.Quit();
    }

    public void OnClick_BackToMainMenu() 
    {
        ShowPanel(panelMainMenu, mainFirstButton);
    }

    public void ChangeDifficultyInSettings(int newDifficulty)
    {
        if (GameManager.Instance.currentPlayerProfile != null)
            GameManager.Instance.currentPlayerProfile.difficulty = newDifficulty;
    }

    public void OnLookSliderChanged(float value)
    {
        GameManager.Instance.currentPlayerProfile.lookSensitivity = value;
    }

    public void OnHandlingSliderChanged(float value)
    {
        GameManager.Instance.currentPlayerProfile.handlingSensitivity = value;
    }

    public void BackToMainMenuFromSettings()
    {
        GameManager.Instance.SaveCurrentProfile();
        ShowPanel(panelMainMenu, mainFirstButton);
    }

    private void OpenRaceSetup()
    {
        ShowPanel(panelRaceSetup, raceSetupFirstButton);
        if(btnCircuit != null && GameManager.Instance.currentPlayerProfile != null)
        {
            btnCircuit.interactable = GameManager.Instance.currentPlayerProfile.hasCompletedSprint;
        }
    }

    public void SelectRaceMode(string mode) 
    {
        GameManager.Instance.selectedRaceMode = mode;
        OpenGarage();
    }

    private void OpenGarage()
    {
        ShowPanel(panelGarage, garageFirstButton);
        if(moneyText != null && GameManager.Instance.currentPlayerProfile != null)
        {
            moneyText.text = "Cash: $" + GameManager.Instance.currentPlayerProfile.totalMoney.ToString("N0");
        }
        
        GarageManager gm = GetComponent<GarageManager>();
        if (gm != null) gm.InitializeGarage();
    }

    public void StartRace() 
    {
        StartCoroutine(LoadSceneSequence());
    }

    private IEnumerator LoadSceneSequence()
    {
        ShowPanel(panelLoading, null);
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(1); 
    }
}