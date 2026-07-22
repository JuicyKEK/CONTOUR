using System.Collections.Generic;
using JuicyDI.Attributes;
using JuicyDI.Demo.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuicyDI.Demo.Scripts
{
    [JDIMonoController]
    public class GameStarter: MonoBehaviour
    {
        [Inject] private List<ISequence> m_TestStarter;

        [SerializeField] private MainJDIController m_MainJDIController;
        [SerializeField] private Button m_Button;

        private GameSequenceController m_GameSequenceController;

        private void Start()
        {
            Debug.Log("~~~~Before Injection~~~~");
            m_MainJDIController.Init();

            Debug.Log("~~~~After Injection~~~~");
            m_GameSequenceController = new GameSequenceController(m_TestStarter);
            
            Debug.Log("~~~~After Run~~~~");
            
            m_Button.onClick.AddListener(OpenNextScene);
        }

        private void OpenNextScene()
        {
            SceneManager.LoadScene("DemoScene2");
        }
    }
}