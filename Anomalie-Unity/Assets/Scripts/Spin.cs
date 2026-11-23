using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] private Vector3 _axis = Vector3.up;
    [SerializeField] private float _degreesPerSecond = 12f;

    private void Update()
    {
        transform.Rotate(_axis, _degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
