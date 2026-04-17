using UnityEngine;
using VRM;

public class AutoBlink : MonoBehaviour
{
    private VRMBlendShapeProxy _proxy;
    private float _timer;
    private float _nextBlink;
    private bool _isBlinking;
    private float _blinkTimer;

    [SerializeField] float blinkDuration = 0.15f;
    [SerializeField] float blinkIntervalMin = 2f;
    [SerializeField] float blinkIntervalMax = 6f;

    void Start()
    {
        _proxy = GetComponent<VRMBlendShapeProxy>();
        ScheduleNextBlink();
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (!_isBlinking && _timer >= _nextBlink)
        {
            _isBlinking = true;
            _blinkTimer = 0f;
        }

        if (_isBlinking)
        {
            _blinkTimer += Time.deltaTime;
            float t = _blinkTimer / blinkDuration;

            // 前半で閉じる、後半で開く
            float value = t < 0.5f
                ? Mathf.SmoothStep(0, 1, t / 0.5f)
                : Mathf.SmoothStep(1, 0, (t - 0.5f) / 0.5f);

            _proxy.SetValue(BlendShapePreset.Blink, value);

            if (_blinkTimer >= blinkDuration)
            {
                _proxy.SetValue(BlendShapePreset.Blink, 0f);
                _isBlinking = false;
                _timer = 0f;
                ScheduleNextBlink();
            }
        }
    }

    void ScheduleNextBlink()
    {
        _nextBlink = Random.Range(blinkIntervalMin, blinkIntervalMax);
    }
}