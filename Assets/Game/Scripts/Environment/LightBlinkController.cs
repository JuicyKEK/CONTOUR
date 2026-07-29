using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class LightBlinkController : MonoBehaviour
{
    [SerializeField] private List<Light> _lightObjects;

    [SerializeField] private float _maxIntensity = 2f;
    [SerializeField] private float _blinkDuration = 0.08f;
    [SerializeField] private int _blinkCount = 3;

    private Sequence _sequence;

    [ContextMenu("TurnOn")]
    public void TurnOn()
    {
        StartCoroutine(BlinkRoutine());

        // _sequence?.Kill();
        //
        // _sequence = DOTween.Sequence();

        // foreach (var light in _lightObjects)
        // {
        //     light.enabled = true;
        // }


        // for (int i = 0; i < _blinkCount; i++)
        // {
        //     _sequence.AppendCallback(() =>
        //     {
        //         foreach (var light in _lightObjects)
        //             light.enabled = true;
        //     });
        //
        //     _sequence.AppendInterval(_blinkDuration);
        //
        //     _sequence.AppendCallback(() =>
        //     {
        //         foreach (var light in _lightObjects)
        //             light.enabled = false;
        //     });
        //
        //     _sequence.AppendInterval(_blinkDuration);
        // }
        //
        // _sequence.AppendCallback(() =>
        // {
        //     foreach (var light in _lightObjects)
        //         light.intensity = _maxIntensity;
        // });
    }
    
    private IEnumerator BlinkRoutine()
    {
        foreach (var light in _lightObjects)
        {
            light.gameObject.SetActive(true);
            light.enabled = true;
        }

        for (int i = 0; i < _blinkCount; i++)
        {
            foreach (var light in _lightObjects)
                light.enabled = true;

            yield return new WaitForSeconds(_blinkDuration);

            foreach (var light in _lightObjects)
                light.enabled = false;

            yield return new WaitForSeconds(_blinkDuration);
        }

        foreach (var light in _lightObjects)
            light.enabled = true;
    }
    
    [ContextMenu("TurnOff")]
    public void TurnOff()
    {
        foreach (var light in _lightObjects)
        {
            light.enabled = false;
        }
    }
}