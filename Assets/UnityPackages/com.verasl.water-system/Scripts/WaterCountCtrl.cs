using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UT.Rendering.Runtime;

[RequireComponent(typeof(RendererCount))]
[DisallowMultipleComponent]
[ExecuteAlways]
public class WaterCountCtrl : MonoBehaviour
{
    private RendererCount _rendererCount;

    private int _passId = -1;

    private void Awake()
    {
        _rendererCount = GetComponent<RendererCount>();
    }

    private void OnEnable()
    {
        if (_rendererCount)
        {
            _passId = _rendererCount.AddPass("Water", false);
        }
        else
        {
            _passId = -1;
        }
    }

    private void OnDisable()
    {
        _rendererCount?.RemovePassById(_passId);
    }
}
