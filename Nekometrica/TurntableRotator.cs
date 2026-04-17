// TurntableRotator.cs
using UnityEngine;

namespace Nekometrica
{

public class TurntableRotator : MonoBehaviour
{
    [Header("回転設定")]
    [Tooltip("1周にかかる秒数")]
    public float duration = 6f;

    [Tooltip("回転軸")]
    public Vector3 axis = Vector3.up;

    [Tooltip("開始角度")]
    public float startAngle = 0f;

    [Header("制御")]
    public bool isPlaying = true;

    private float elapsed = 0f;

    void OnEnable()
    {
        elapsed = 0f;
        ApplyRotation(startAngle);
    }

    void Update()
    {
        if (!isPlaying) return;

        elapsed += Time.deltaTime;
        float angle = startAngle + (elapsed / duration) * 360f;
        ApplyRotation(angle);
    }

    void ApplyRotation(float angle)
    {
        transform.rotation = Quaternion.AngleAxis(angle, axis);
    }

    // Timelineや外部から直接フレーム指定したい場合
    public void SetNormalizedTime(float t)
    {
        float angle = startAngle + t * 360f;
        ApplyRotation(angle);
    }
}

}