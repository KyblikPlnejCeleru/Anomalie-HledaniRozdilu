using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{
    public GameObject Panel;
    public Button ResumeButton;
    public Button RestartButton;
    public Button MenuButton;

    private void Awake()
    {
        ResumeButton.onClick.AddListener(Toggle);
        RestartButton.onClick.AddListener(() => { Time.timeScale = 1f; GameData.Instance.ResetRound(); SceneManager.LoadScene(1); });
        MenuButton.onClick.AddListener(() => { Time.timeScale = 1f; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; SceneManager.LoadScene(0); });
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) Toggle();
    }

    private void Toggle()
    {
        bool show = !Panel.activeSelf;
        Panel.SetActive(show);
        Time.timeScale = show ? 0f : 1f;
        if (show)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            FindFirstObjectByType<StationPlayer>()?.LockCursor();
        }
    }
}
