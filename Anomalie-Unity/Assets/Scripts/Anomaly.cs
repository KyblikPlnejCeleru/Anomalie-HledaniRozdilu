using UnityEngine;

public enum AnomalyKind { Missing, Moved, Rotated, WrongColor, WrongScale, Extra }

public class Anomaly : MonoBehaviour
{
    [SerializeField] private AnomalyKind _kind;
    [SerializeField] private string _label;
    [SerializeField] private Vector3 _correctPosition;
    [SerializeField] private Quaternion _correctRotation = Quaternion.identity;
    [SerializeField] private Vector3 _correctScale = Vector3.one;
    [SerializeField] private Renderer _tinted;
    [SerializeField] private Color _correctColor = Color.cyan;
    [SerializeField] private bool _emissive;
    [SerializeField] private GameObject _reveal;

    public string Label => _label;
    public bool Fixed { get; private set; }

    public bool Fix()
    {
        if (Fixed) return false;
        Fixed = true;

        switch (_kind)
        {
            case AnomalyKind.Missing:
                if (_reveal != null) _reveal.SetActive(true);
                gameObject.SetActive(false);
                break;
            case AnomalyKind.Moved:
                transform.position = _correctPosition;
                break;
            case AnomalyKind.Rotated:
                transform.rotation = _correctRotation;
                break;
            case AnomalyKind.WrongColor:
                if (_tinted != null)
                {
                    if (_emissive) _tinted.material.SetColor("_EmissionColor", _correctColor);
                    else _tinted.material.color = _correctColor;
                }
                break;
            case AnomalyKind.WrongScale:
                transform.localScale = _correctScale;
                break;
            case AnomalyKind.Extra:
                gameObject.SetActive(false);
                break;
        }
        enabled = false;
        return true;
    }
}
