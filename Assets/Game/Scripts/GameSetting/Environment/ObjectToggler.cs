using System;
using UnityEngine;

public class ObjectToggler : MonoBehaviour
{
    [SerializeField] private GameObject m_TargetObject;

    private void Start()
    {
        if (!m_TargetObject)
        {
            m_TargetObject = this.gameObject;
        }
    }

    public void ToggleActive()
    {
        if (m_TargetObject != null)
        {
            m_TargetObject.SetActive(!m_TargetObject.activeSelf);
        }
    }
}