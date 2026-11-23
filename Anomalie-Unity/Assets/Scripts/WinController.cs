using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinController : MonoBehaviour
{
    public Text ScoreLabel;
    public Text DetailLabel;
    public Text RankLabel;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameData data = GameData.Instance;
        int score = data != null ? data.Score : 0;
        int clicks = data != null ? data.Clicks : 0;
        int mistakes = data != null ? data.Mistakes : 0;
        float time = data != null ? data.Elapsed : 0f;
        int s = Mathf.FloorToInt(time);

        ScoreLabel.text = "VÝSLEDNÉ SKÓRE   " + score;
        DetailLabel.text = "Nalezeno " + GameData.Total + "/" + GameData.Total +
                           "     Kliknutí " + clicks +
                           "     Chyby " + mistakes +
                           "\nČas " + (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        RankLabel.text = "HODNOCENÍ   " + (mistakes == 0 ? "S" : mistakes < 3 ? "A" : mistakes < 6 ? "B" : "C");

        GameObject back = GameObject.Find("BtnBack");
        if (back != null) back.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(0));
    }
}
