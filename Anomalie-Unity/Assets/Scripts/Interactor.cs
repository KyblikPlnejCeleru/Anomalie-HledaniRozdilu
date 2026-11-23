using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [SerializeField] private float _range = 14f;
    public Hud Hud;

    private void Update()
    {
        if (Time.timeScale < 0.01f) return;
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;

        GameData.Instance.Clicks++;
        Transform cam = Camera.main.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, _range))
        {
            Anomaly anomaly = hit.collider.GetComponentInParent<Anomaly>();
            if (anomaly != null && anomaly.Fix())
            {
                Hud.RegisterHit(anomaly.Label);
                return;
            }
        }
        Hud.RegisterMiss();
    }
}
