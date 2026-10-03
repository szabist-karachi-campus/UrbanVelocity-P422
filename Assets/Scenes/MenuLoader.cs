using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems; 

public class MenuLoader : MonoBehaviour
{
    [Header("Pause Menu Elements")]
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel;
    public AudioSource cameraShutterAudio; 
    
    [Header("Controller Navigation")]
    public GameObject pauseFirstButton;    // Drag your "Resume" button here
    public GameObject settingsFirstButton; // Drag the first button/slider in your Settings panel here
    
    private bool isPaused = false;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;    
        Application.targetFrameRate = 300; 
    }

    private void Update()
    {
        // 1. START / ESCAPE BUTTON: Pauses or Unpauses the game
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                UI_CloseSettings(); // If in settings, pressing Start backs out to Pause Menu
            }
            else 
            {
                TogglePauseMenu();
            }
        }

        // 2. 'B' BUTTON / BACKSPACE: The dedicated "Back" button
        if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Backspace))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                UI_CloseSettings(); // In Settings -> Go back to Pause Menu
            }
            else if (isPaused)
            {
                UI_ResumeGame(); // In Pause Menu -> Resume the game
            }
        }
    }

    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            if (settingsPanel != null) settingsPanel.SetActive(false); 
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            if (cameraShutterAudio != null) cameraShutterAudio.Pause();
            
            SetSelectedButton(pauseFirstButton);
        }
        else
        {
            Time.timeScale = 1f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (cameraShutterAudio != null) cameraShutterAudio.UnPause();
        }
    }

    // Helper function to force the controller to highlight a specific button
    private void SetSelectedButton(GameObject buttonToSelect)
    {
        EventSystem.current.SetSelectedGameObject(null); 
        if (buttonToSelect != null) 
        {
            EventSystem.current.SetSelectedGameObject(buttonToSelect);
        }
    }

    // --- UI BUTTON FUNCTIONS ---
    public void UI_ResumeGame() 
    { 
        if (isPaused) TogglePauseMenu(); 
    }
    
    public void UI_OpenSettings() 
    { 
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false); // Hide Pause Menu
        if (settingsPanel != null) settingsPanel.SetActive(true);    // Show Settings Menu
        SetSelectedButton(settingsFirstButton);                      // Highlight Settings Button
    }
    
    public void UI_CloseSettings() 
    { 
        if (settingsPanel != null) settingsPanel.SetActive(false);   // Hide Settings Menu
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);  // Show Pause Menu
        SetSelectedButton(pauseFirstButton);                         // Highlight Resume Button
    }

    public void UI_QuitGame() { Application.Quit(); }

    public void LoadGarage()
    {
        Time.timeScale = 1f; 
        StartCoroutine(LoadSceneCoroutine(0)); 
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f; 
        StartCoroutine(LoadSceneCoroutine(SceneManager.GetActiveScene().buildIndex));
    }

    private IEnumerator LoadSceneCoroutine(int sceneIndex)
    {
        yield return new WaitForSecondsRealtime(0.5f); 
        SceneManager.LoadScene(sceneIndex);
    }
}