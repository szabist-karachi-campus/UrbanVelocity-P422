using UnityEngine;

public class CoreTester : MonoBehaviour
{
    void Update()
    {
        // PRESS SPACEBAR: Add $500 to the profile
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameManager.Instance.currentPlayerProfile.totalMoney += 500;
            Debug.Log("Added Cash! Total Money: $" + GameManager.Instance.currentPlayerProfile.totalMoney);
        }

        // PRESS S: Save the game
        if (Input.GetKeyDown(KeyCode.S))
        {
            GameManager.Instance.SaveCurrentProfile();
            Debug.Log("Game Saved manually via Tester.");
        }

        // PRESS L: Load the game manually (GameManager already does this on Awake)
        if (Input.GetKeyDown(KeyCode.L))
        {
            GameManager.Instance.currentPlayerProfile = SaveManager.LoadGame();
            Debug.Log("Loaded manually! Total Money: $" + GameManager.Instance.currentPlayerProfile.totalMoney);
        }
    }
}