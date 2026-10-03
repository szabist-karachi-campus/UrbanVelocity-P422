using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class GarageManager : MonoBehaviour
{
    [Header("Camera Control")]
    public SmoothFollowCamera garageCamera;
    
    [Header("Camera Angles")]
    public Transform[] cameraAnchors; 
    public float transitionSpeed = 5f; 
    private int currentCamIndex = 0;

    [Header("Data")]
    public CarData[] allAvailableCars; 
    private int currentIndex = 0;

    [Header("3D Showroom Platform")]
    public Transform showroomCenter; 
    private GameObject currentCarModel;

    [Header("UI Elements")]
    public TextMeshProUGUI carNameText;
    public TextMeshProUGUI carPriceText;
    public TextMeshProUGUI totalCashText;
    
    public Button btnBuy;
    public Button btnSelect;
    public Button btnPaintJob; 
    public Button btnNextCar;
    public Button btnPrevCar;

    [Header("Color Palette UI Matrix (5x2 Grid)")]
    public GameObject panelColorPalette;
    public GameObject firstColorButton;

    [Header("The 10 Paintjob Slots")]
    public Button[] paletteButtons = new Button[10];
    public Color[] paletteColors = new Color[10];

    [Header("The Shade Section UI Layout")]
    public GameObject panelShadeSelection;
    public Button btnGlossy;
    public Button btnMetallic;
    public Button btnMatte;

    [Header("Stat Bars")]
    public Slider speedBar;
    public Slider handlingBar;
    public Slider accelerationBar;

    [Header("Connection")]
    public MainMenuController menuController; 

    private Color originalCarColor;
    private string originalCarFinish = "Glossy";
    private Color temporarilySelectedColor;
    private GameObject lastSelectedUIObject;
    private int paintCost = 1000;

    private void Awake()
    {
        if (panelShadeSelection != null) panelShadeSelection.SetActive(false);
        if (panelColorPalette != null) panelColorPalette.SetActive(false);
    }

    private void Start()
    {
        if (speedBar != null) speedBar.interactable = false;
        if (handlingBar != null) handlingBar.interactable = false;
        if (accelerationBar != null) accelerationBar.interactable = false;

        if (btnPaintJob != null)
        {
            btnPaintJob.onClick.RemoveAllListeners();
            btnPaintJob.onClick.AddListener(OpenColorPalette);
        }

        for (int i = 0; i < paletteButtons.Length; i++)
        {
            if (paletteButtons[i] != null)
            {
                int index = i; 
                paletteButtons[i].onClick.RemoveAllListeners();
                paletteButtons[i].onClick.AddListener(() => OnColorSelected(index));
            }
        }

        if (btnGlossy != null) { btnGlossy.onClick.RemoveAllListeners(); btnGlossy.onClick.AddListener(() => ConfirmAndBuyPaintJob("Glossy")); }
        if (btnMetallic != null) { btnMetallic.onClick.RemoveAllListeners(); btnMetallic.onClick.AddListener(() => ConfirmAndBuyPaintJob("Metallic")); }
        if (btnMatte != null) { btnMatte.onClick.RemoveAllListeners(); btnMatte.onClick.AddListener(() => ConfirmAndBuyPaintJob("Matte")); }
    }

    private void Update()
    {
        if (panelColorPalette == null || !panelColorPalette.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.C)) ChangeCamera();
        }

        if (cameraAnchors.Length > 0 && garageCamera != null && showroomCenter != null)
        {
            Vector3 targetPosition = cameraAnchors[currentCamIndex].position;
            garageCamera.transform.position = Vector3.Lerp(garageCamera.transform.position, targetPosition, transitionSpeed * Time.deltaTime);
            Vector3 lookTarget = showroomCenter.position + new Vector3(0, 1f, 0);
            garageCamera.transform.LookAt(lookTarget);
        }

        if (panelColorPalette != null && panelColorPalette.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                CancelPaintJob();
                return;
            }

            if (EventSystem.current != null)
            {
                GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
                if (currentSelection != lastSelectedUIObject && currentSelection != null)
                {
                    lastSelectedUIObject = currentSelection;

                    for (int i = 0; i < paletteButtons.Length; i++)
                    {
                        if (paletteButtons[i] != null && paletteButtons[i].gameObject == currentSelection)
                        {
                            PreviewColorOnCar(paletteColors[i]);
                            break;
                        }
                    }

                    if (currentSelection == btnGlossy.gameObject) ApplyFinishProperties("Glossy");
                    if (currentSelection == btnMetallic.gameObject) ApplyFinishProperties("Metallic");
                    if (currentSelection == btnMatte.gameObject) ApplyFinishProperties("Matte");
                }
            }
        }
    }

    public void InitializeGarage()
    {
        if(garageCamera != null) garageCamera.enabled = false; 
        if (cameraAnchors.Length > 0 && garageCamera != null && showroomCenter != null)
        {
            garageCamera.transform.position = cameraAnchors[currentCamIndex].position;
            garageCamera.transform.LookAt(showroomCenter.position + new Vector3(0, 1f, 0));
        }
        
        if (panelColorPalette != null) panelColorPalette.SetActive(false);
        if (panelShadeSelection != null) panelShadeSelection.SetActive(false);
        UpdateUIAndModel();
    }

    public void ChangeCamera()
    {
        if (cameraAnchors == null || cameraAnchors.Length == 0) return;
        currentCamIndex++;
        if (currentCamIndex >= cameraAnchors.Length) currentCamIndex = 0; 
    }

    public void NextCar()
{
    if (panelColorPalette != null && panelColorPalette.activeSelf) return;
    currentIndex++;
    if (currentIndex >= allAvailableCars.Length) currentIndex = 0;
    UpdateUIAndModel();
    
    // THE FIX: Re-grab focus so the D-pad doesn't die
    ReassignFocusAfterCarSwap();
}

public void PreviousCar()
{
    if (panelColorPalette != null && panelColorPalette.activeSelf) return;
    currentIndex--;
    if (currentIndex < 0) currentIndex = allAvailableCars.Length - 1;
    UpdateUIAndModel();
    
    // THE FIX: Re-grab focus so the D-pad doesn't die
    ReassignFocusAfterCarSwap();
}

// Add this quick helper function right below them:
private void ReassignFocusAfterCarSwap()
{
    // If the new car is owned, highlight Select. If it's locked, highlight Buy!
    if (btnSelect != null && btnSelect.gameObject.activeInHierarchy)
    {
        HighlightButtonForController(btnSelect.gameObject);
    }
    else if (btnBuy != null && btnBuy.gameObject.activeInHierarchy)
    {
        HighlightButtonForController(btnBuy.gameObject);
    }
}

    // ----------------------------------------------------------------
    // NEW: Save File Fixer - Prevents NullReferenceExceptions from old saves!
    // ----------------------------------------------------------------
    private void ValidateProfileData(PlayerData profile)
    {
        if (profile.savedCarColors == null) profile.savedCarColors = new List<string>();
        if (profile.savedCarFinishes == null) profile.savedCarFinishes = new List<string>();
        if (profile.unlockedCarIDs == null) profile.unlockedCarIDs = new List<string>();
    }

    private void UpdateUIAndModel()
    {
        CarData currentData = allAvailableCars[currentIndex];
        
        // Ensure old saves don't crash the UI script
        ValidateProfileData(GameManager.Instance.currentPlayerProfile);

        carNameText.text = currentData.displayName;
        totalCashText.text = "Cash: $" + GameManager.Instance.currentPlayerProfile.totalMoney.ToString("N0");
        speedBar.value = currentData.topSpeed;
        handlingBar.value = currentData.handling;
        accelerationBar.value = currentData.acceleration;

        if (currentCarModel != null) Destroy(currentCarModel); 
        
        Vector3 finalSpawnPosition = showroomCenter.position + new Vector3(0, currentData.garageHeightOffset, 0);
        currentCarModel = Instantiate(currentData.carPrefab, finalSpawnPosition, showroomCenter.rotation);
        
        Rigidbody rb = currentCarModel.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        MonoBehaviour[] allScripts = currentCarModel.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in allScripts) script.enabled = false; 

        WheelCollider[] wheelColliders = currentCarModel.GetComponentsInChildren<WheelCollider>();
        foreach (WheelCollider wc in wheelColliders) { wc.suspensionDistance = 0f; wc.motorTorque = 0f; wc.brakeTorque = 1000000f; }

        LoadCarCustomizationFromActiveProfile(currentData.carID);

        bool isUnlocked = GameManager.Instance.currentPlayerProfile.unlockedCarIDs.Contains(currentData.carID);
        if (isUnlocked || currentData.isUnlockedByDefault)
        {
            carPriceText.text = "OWNED";
            btnBuy.gameObject.SetActive(false);
            btnSelect.gameObject.SetActive(true);
            btnPaintJob.interactable = true; 
        }
        else
        {
            carPriceText.text = "$" + currentData.price.ToString("N0");
            btnBuy.gameObject.SetActive(true);
            btnSelect.gameObject.SetActive(false);
            btnPaintJob.interactable = false;
        }
    }

    private void LoadCarCustomizationFromActiveProfile(string carID)
    {
        var profile = GameManager.Instance.currentPlayerProfile;
        
        int colorIdx = profile.savedCarColors.FindIndex(s => s.StartsWith(carID + ":"));
        int finishIdx = profile.savedCarFinishes.FindIndex(s => s.StartsWith(carID + ":"));

        if (colorIdx != -1)
        {
            string hex = profile.savedCarColors[colorIdx].Split(':')[1];
            if (ColorUtility.TryParseHtmlString(hex, out Color loadedColor)) PreviewColorOnCar(loadedColor);
        }
        if (finishIdx != -1)
        {
            string finishType = profile.savedCarFinishes[finishIdx].Split(':')[1];
            ApplyFinishProperties(finishType);
        }
    }

    public void BuyCurrentCar()
{
    CarData currentData = allAvailableCars[currentIndex];
    if (GameManager.Instance.currentPlayerProfile.totalMoney >= currentData.price)
    {
        // 1. Process the purchase
        GameManager.Instance.currentPlayerProfile.totalMoney -= currentData.price;
        GameManager.Instance.currentPlayerProfile.unlockedCarIDs.Add(currentData.carID);
        GameManager.Instance.SaveCurrentProfile();
        UpdateUIAndModel();

        // 2. THE FIX: Catch the falling UI focus!
        // Because "Buy" just disappeared, snap the cursor instantly to the "Select" button
        if (btnSelect != null && btnSelect.gameObject.activeInHierarchy)
        {
            HighlightButtonForController(btnSelect.gameObject);
        }
    }
}

    public void SelectCarAndRace()
    {
        CarData currentData = allAvailableCars[currentIndex];
        var profile = GameManager.Instance.currentPlayerProfile;

        ValidateProfileData(profile);

        string colorPrefix = currentData.carID + ":";
        int colorIdx = profile.savedCarColors.FindIndex(s => s.StartsWith(colorPrefix));
        int finishIdx = profile.savedCarFinishes.FindIndex(s => s.StartsWith(colorPrefix));

        if (colorIdx != -1) PlayerPrefs.SetString("RacePaintHex", profile.savedCarColors[colorIdx].Split(':')[1]);
        else PlayerPrefs.DeleteKey("RacePaintHex");

        if (finishIdx != -1) PlayerPrefs.SetString("RacePaintFinish", profile.savedCarFinishes[finishIdx].Split(':')[1]);
        else PlayerPrefs.DeleteKey("RacePaintFinish");

        PlayerPrefs.Save();

        GameManager.Instance.selectedCarPrefab = currentData.carPrefab;
        menuController.StartRace(); 
    }

    public void OpenColorPalette()
    {
        if (panelColorPalette == null || currentCarModel == null) return;

        MeshRenderer[] renderers = currentCarModel.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            if (mr.CompareTag("carpaint") && mr.sharedMaterials.Length > 0)
            {
                Material mat = mr.sharedMaterials[0];
                string activeProperty = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                originalCarColor = mat.GetColor(activeProperty);
                break; // Just need to grab it once
            }
        }

        lastSelectedUIObject = null;
        panelColorPalette.SetActive(true);
        if (panelShadeSelection != null) panelShadeSelection.SetActive(false); 
        
        SetBackgroundButtonsInteractable(false);
        if (firstColorButton != null) HighlightButtonForController(firstColorButton);
    }

    public void OnColorSelected(int index)
    {
        temporarilySelectedColor = paletteColors[index];
        PreviewColorOnCar(temporarilySelectedColor);

        if (panelShadeSelection != null) panelShadeSelection.SetActive(true);
        if (btnGlossy != null) HighlightButtonForController(btnGlossy.gameObject);
    }

    public void ConfirmAndBuyPaintJob(string chosenFinish)
    {
        CarData currentData = allAvailableCars[currentIndex];
        var profile = GameManager.Instance.currentPlayerProfile;

        ValidateProfileData(profile);

        if (profile.totalMoney >= paintCost && currentCarModel != null)
        {
            profile.totalMoney -= paintCost;

            PreviewColorOnCar(temporarilySelectedColor);
            ApplyFinishProperties(chosenFinish);

            originalCarColor = temporarilySelectedColor;
            originalCarFinish = chosenFinish;

            string colorPrefix = currentData.carID + ":";
            profile.savedCarColors.RemoveAll(s => s.StartsWith(colorPrefix));
            profile.savedCarColors.Add(colorPrefix + "#" + ColorUtility.ToHtmlStringRGBA(temporarilySelectedColor));

            profile.savedCarFinishes.RemoveAll(s => s.StartsWith(colorPrefix));
            profile.savedCarFinishes.Add(colorPrefix + chosenFinish);

            GameManager.Instance.SaveCurrentProfile();

            totalCashText.text = "Cash: $" + profile.totalMoney.ToString("N0");
            CloseColorPalette();
        }
    }

    public void CloseColorPalette()
    {
        if (panelColorPalette != null) panelColorPalette.SetActive(false);
        if (panelShadeSelection != null) panelShadeSelection.SetActive(false);
        
        SetBackgroundButtonsInteractable(true);
        if (btnPaintJob != null) HighlightButtonForController(btnPaintJob.gameObject);
    }

    public void CancelPaintJob()
    {
        PreviewColorOnCar(originalCarColor);
        ApplyFinishProperties(originalCarFinish);
        CloseColorPalette();
    }

    private void PreviewColorOnCar(Color previewColor)
    {
        if (currentCarModel == null) return;

        MeshRenderer[] renderers = currentCarModel.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            if (!mr.CompareTag("carpaint")) continue;

            Material[] mats = mr.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                string activeProperty = mats[i].HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                mats[i].SetColor(activeProperty, previewColor);
            }
            mr.materials = mats; 
        }
    }

    private void ApplyFinishProperties(string finishType)
    {
        if (currentCarModel == null) return;

        float metallicValue = 0f;
        float smoothnessValue = 0.5f;

        if (finishType == "Glossy") { metallicValue = 0.0f; smoothnessValue = 0.9f; }
        else if (finishType == "Metallic") { metallicValue = 1.0f; smoothnessValue = 0.82f; }
        else if (finishType == "Matte") { metallicValue = 0.0f; smoothnessValue = 0.12f; }

        MeshRenderer[] renderers = currentCarModel.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            if (!mr.CompareTag("carpaint")) continue;

            Material[] mats = mr.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                string metalProp = mat.HasProperty("_Metallic") ? "_Metallic" : "_MetallicGlossMap";
                string smoothProp = mat.HasProperty("_Glossiness") ? "_Glossiness" : "_GlossMapScale";
                if (mat.HasProperty("_Smoothness")) smoothProp = "_Smoothness";

                mat.SetFloat(metalProp, metallicValue);
                mat.SetFloat(smoothProp, smoothnessValue);
            }
            mr.materials = mats;
        }
    }

    private void SetBackgroundButtonsInteractable(bool state)
    {
        if (btnBuy != null) btnBuy.interactable = state;
        if (btnSelect != null) btnSelect.interactable = state;
        if (btnPaintJob != null) btnPaintJob.interactable = state;
        if (btnNextCar != null) btnNextCar.interactable = state;
        if (btnPrevCar != null) btnPrevCar.interactable = state;
    }

    private void HighlightButtonForController(GameObject buttonToHighlight)
    {
        if (gameObject.activeInHierarchy) StartCoroutine(DelayHighlightRoutine(buttonToHighlight));
    }

    private IEnumerator DelayHighlightRoutine(GameObject targetButton)
    {
        yield return null; 
        if (EventSystem.current != null && targetButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null); 
            EventSystem.current.SetSelectedGameObject(targetButton); 
        }
    }
}