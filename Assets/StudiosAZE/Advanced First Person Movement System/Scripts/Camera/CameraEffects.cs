using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace AZE.AdvancedFirstPerson
{
    public class CameraEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovementStateMachine playerMovement;
        [SerializeField] private CinemachineCamera cinemachineCamera;

        [Header("Bob Settings")]
        [SerializeField] private bool useHeadBob = true;
        [Range(0f, 5f)] [SerializeField] private float moveAmplitude = 1.5f;
        [Range(0f, 10f)] [SerializeField] private float moveFrequency = 2.5f;
        [Range(0.1f, 20f)] [SerializeField] private float bobSmoothing = 5f;
        private float idleAmplitude;
        private float idleFrequency;

        [Header("Tilt Settings")]
        [SerializeField] private bool useTilt = true;
        [Range(0f, 10f)] [SerializeField] private float maxTiltAngle = 1.5f;
        [Range(0.1f, 20f)] [SerializeField] private float tiltSmoothing = 8f;

        [Header("FOV Settings")]
        [SerializeField] private bool useFovKick = true;
        [Range(0f, 20f)] [SerializeField] private float fovBoostAmount = 10f;
        [Range(0.1f, 20f)] [SerializeField] private float fovSmoothing = 4f;

        [Header("Приближение по ПКМ")]
        [Tooltip("Пока зажата правая кнопка мыши, камера слегка приближается (сужается угол обзора), " +
                 "отпустили - возвращается. Не работает, пока виден курсор (открыта панель, осмотр предмета) " +
                 "или движение игрока запрещено.")]
        [SerializeField] private bool useRightMouseZoom = true;
        [Tooltip("Во сколько раз сужается угол обзора при приближении: 0.8 - на 20 % (60° -> 48°).")]
        [Range(0.3f, 1f)] [SerializeField] private float zoomFovMultiplier = 0.8f;
        [Tooltip("Скорость приближения и возврата камеры.")]
        [Range(0.1f, 30f)] [SerializeField] private float zoomSmoothing = 10f;

        private CinemachineBasicMultiChannelPerlin _cameraNoise;
        private float _baseFOV;
        // Текущий множитель приближения (1 - без приближения) и текущая прибавка угла обзора от бега.
        private float _zoomFactor = 1f;
        private float _fovKick;

        private void Awake()
        {
            if (cinemachineCamera == null)
                cinemachineCamera = GetComponent<CinemachineCamera>();

            if (cinemachineCamera != null)
            {
                _cameraNoise = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
                idleAmplitude = _cameraNoise.AmplitudeGain;
                idleFrequency = _cameraNoise.FrequencyGain;
                _baseFOV = cinemachineCamera.Lens.FieldOfView;
            }

            if (cinemachineCamera != null)
            {
                playerMovement = GetComponentInParent<PlayerMovementStateMachine>();
            }

            if (playerMovement == null)
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (playerMovement == null) return;

            float speedPercent = playerMovement.CurrentSpeedPercentage;
            float inputX = playerMovement.InputHandler.MoveInput.x;

            HandleBob(speedPercent);
            HandleTilt(speedPercent, inputX);
            HandleFOV(speedPercent);
        }

        private void HandleBob(float speedPercent)
        {
            if (!useHeadBob) return;
            if (_cameraNoise == null) return;

            float targetAmp = Mathf.Lerp(idleAmplitude, moveAmplitude, speedPercent);
            float targetFreq = Mathf.Lerp(idleFrequency, moveFrequency, speedPercent);

            _cameraNoise.AmplitudeGain = Mathf.Lerp(_cameraNoise.AmplitudeGain, targetAmp, bobSmoothing * Time.deltaTime);
            _cameraNoise.FrequencyGain = Mathf.Lerp(_cameraNoise.FrequencyGain, targetFreq, bobSmoothing * Time.deltaTime);
        }

        private void HandleTilt(float speedPercent, float inputX)
        {
            if (!useTilt) return;
            if (cinemachineCamera == null) return;

            float targetTilt = -inputX * maxTiltAngle * speedPercent;

            var lens = cinemachineCamera.Lens;

            lens.Dutch = Mathf.Lerp(lens.Dutch, targetTilt, tiltSmoothing * Time.deltaTime);

            cinemachineCamera.Lens = lens;
        }

        private void HandleFOV(float speedPercent)
        {
            if (!useFovKick && !useRightMouseZoom) return;
            if (cinemachineCamera == null) return;

            // Прибавка от бега и приближение сглаживаются каждое своей скоростью: приближение по ПКМ
            // должно срабатывать быстрее, чем плавное расширение обзора на бегу.
            float targetKick = useFovKick ? fovBoostAmount * speedPercent : 0f;
            _fovKick = Mathf.Lerp(_fovKick, targetKick, fovSmoothing * Time.deltaTime);

            float targetZoom = IsZoomHeld() ? zoomFovMultiplier : 1f;
            _zoomFactor = Mathf.Lerp(_zoomFactor, targetZoom, zoomSmoothing * Time.deltaTime);

            var lens = cinemachineCamera.Lens;

            lens.FieldOfView = _baseFOV * _zoomFactor + _fovKick;

            cinemachineCamera.Lens = lens;
        }

        private bool IsZoomHeld()
        {
            if (!useRightMouseZoom) return false;
            if (playerMovement.IsUseMouse || !playerMovement.IsCanMove) return false;

            var mouse = Mouse.current;
            return mouse != null && mouse.rightButton.isPressed;
        }
    }
}