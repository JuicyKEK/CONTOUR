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
        // Объект мог быть выключен ещё до Start (например, свет выключили из сохранения) - тогда Start не вызывался.
        if (!m_TargetObject)
        {
            m_TargetObject = this.gameObject;
        }

        if (m_TargetObject != null)
        {
            m_TargetObject.SetActive(!m_TargetObject.activeSelf);
        }
    }
}