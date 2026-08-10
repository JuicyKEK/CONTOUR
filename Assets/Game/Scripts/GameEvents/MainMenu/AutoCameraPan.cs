using UnityEngine;

public class AutoCameraPanGlobal : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float m_Angle = 45f;
    [SerializeField] private float m_Speed = 1f;
    [SerializeField] private Vector3 m_GlobalAxis = Vector3.up;

    private Quaternion m_InitialWorldRotation;

    private void Awake()
    {
        m_InitialWorldRotation = transform.rotation;
    }

    private void Update()
    {
        float sinValue = Mathf.Sin(Time.time * m_Speed);
        float currentAngle = sinValue * m_Angle;

        Quaternion worldOffset =
            Quaternion.AngleAxis(currentAngle, m_GlobalAxis.normalized);

        transform.rotation = worldOffset * m_InitialWorldRotation;
    }
}