using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Bind("BtnStart", StartGame);
        Bind("BtnCredits", () => SceneManager.LoadScene(3));
        Bind("BtnQuit", Application.Quit);
        Bind("BtnBack", () => SceneManager.LoadScene(0));
    }

    private void Bind(string name, UnityEngine.Events.UnityAction action)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) go.GetComponent<Button>().onClick.AddListener(action);
    }

    private void StartGame()
    {
        if (GameData.Instance != null) GameData.Instance.ResetRound();
        SceneManager.LoadScene(1);
    }
}
