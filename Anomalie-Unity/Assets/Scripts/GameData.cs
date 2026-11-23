using UnityEngine;

public class GameData : MonoBehaviour
{
    public const int Total = 8;
    public static GameData Instance { get; private set; }

    public int Score;
    public int Found;
    public int Clicks;
    public int Mistakes;
    public float Elapsed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResetRound();
    }

    public void ResetRound()
    {
        Score = Found = Clicks = Mistakes = 0;
        Elapsed = 0f;
    }
}
