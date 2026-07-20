using UnityEngine;

[ExecuteInEditMode]
public class ObjectActivatorInEditor : MonoBehaviour
{
    [SerializeField] private ObjectsActivatorInEditor[] Objects;
    
    private void OnValidate()
    {
        UpdateActiveObjects();
    }
    
    private void UpdateActiveObjects()
    {
        if (Objects != null && Objects.Length != 0)
        {
            for (int i = 0; i < Objects.Length; i++)
            {
                if (Objects[i].Objects != null && Objects[i].Objects.Length != 0 
                                               && Objects[i].Objects[0].activeSelf != Objects[i].IsActive)
                {
                    for (int j = 0; j < Objects[i].Objects.Length; j++)
                    {
                        Objects[i].Objects[j].gameObject.SetActive(Objects[i].IsActive);
                    }
                }
            }
        }
    }
}