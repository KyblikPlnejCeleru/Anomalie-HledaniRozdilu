using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Hud : MonoBehaviour
{
    public Text ScoreLabel;
    public Text FoundLabel;
    public Text TimeLabel;
    public Text FeedLabel;
    public Image Flash;

    private float _feedLeft;

    private void Update()
    {
        GameData data = GameData.Instance;
        if (data == null) return;

        data.Elapsed += Time.deltaTime;
        ScoreLabel.text = "SKÓRE " + data.Score.ToString("000");
        FoundLabel.text = "ANOMÁLIE " + data.Found + " / " + GameData.Total;
        int s = Mathf.FloorToInt(data.Elapsed);
        TimeLabel.text = "ČAS " + (s / 60).ToString("00") + ":" + (s % 60).ToString("00");

        if (_feedLeft > 0f)
        {
            _feedLeft -= Time.deltaTime;
            Color c = FeedLabel.color;
            c.a = Mathf.Clamp01(_feedLeft);
            FeedLabel.color = c;
            Color f = Flash.color;
            f.a = Mathf.Clamp01(_feedLeft) * 0.18f;
            Flash.color = f;
        }
    }

    public void RegisterHit(string label)
    {
        GameData data = GameData.Instance;
        data.Score++;
        data.Found++;
        Show("+1  " + label, new Color(0.3f, 1f, 0.75f));
        if (data.Found >= GameData.Total) StartCoroutine(Finish());
    }

    public void RegisterMiss()
    {
        GameData data = GameData.Instance;
        data.Mistakes++;
        data.Score = Mathf.Max(0, data.Score - 1);
        Show("CHYBA  -1", new Color(1f, 0.35f, 0.35f));
    }

    private void Show(string message, Color color)
    {
        FeedLabel.text = message;
        FeedLabel.color = color;
        Flash.color = new Color(color.r, color.g, color.b, 0.18f);
        _feedLeft = 1.1f;
    }

    private IEnumerator Finish()
    {
        yield return new WaitForSeconds(0.9f);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(2);
    }
}
