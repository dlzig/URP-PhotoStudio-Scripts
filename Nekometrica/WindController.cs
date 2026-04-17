using UnityEngine;
using VRM;

public class WindController : MonoBehaviour
{
    [Header("風向き")]
    public WindZone windZone;
    public Vector3 windBaseOrientation = new Vector3(1f, 0f, 0f);

    [Header("1/f ゆらぎ設定")]
    public float baseStrength = 0.1f;
    public float strengthFactor = 1.0f;

    [Header("Gizmo設定")]
    public float arrowLength = 2.0f;
    public Color arrowColor = Color.cyan;

    private readonly float[] frequencies = { 0.05f, 0.13f, 0.27f, 0.51f, 1.1f, 2.3f };
    private readonly float[] amplitudes  = { 1.0f,  0.7f,  0.5f,  0.35f, 0.25f, 0.18f };
    private float[] phaseOffsets;

    private VRMSpringBone[] springBones;

    void Start()
    {
        springBones = FindObjectsOfType<VRMSpringBone>();

        phaseOffsets = new float[frequencies.Length];
        for (int i = 0; i < phaseOffsets.Length; i++)
        {
            phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void Update()
    {
        // 1/fゆらぎで風の強さを生成
        float noise = 0f;
        float totalAmplitude = 0f;

        for (int i = 0; i < frequencies.Length; i++)
        {
            noise += Mathf.Sin(Time.time * frequencies[i] * Mathf.PI * 2f + phaseOffsets[i]) * amplitudes[i];
            totalAmplitude += amplitudes[i];
        }

        float windStrength = Mathf.Max(0f, (noise / totalAmplitude) * baseStrength * strengthFactor + baseStrength * 0.5f);

        // 重力ベクトル（0, -1, 0）に風ベクトルを加算してから正規化
        // 参考コードの applyWindForce の設計を踏襲
        Vector3 gravity = new Vector3(0f, -1f, 0f);
        Vector3 direction = (windZone != null) ? windZone.transform.forward : windBaseOrientation.normalized;
        Vector3 wind = direction * windStrength;
        Vector3 combinedDir = (gravity + wind).normalized;

        foreach (var bone in springBones)
        {
            bone.m_gravityDir = combinedDir;
            bone.m_gravityPower = windStrength;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = arrowColor;
        Vector3 origin = transform.position;
        Vector3 direction = windBaseOrientation.normalized * arrowLength;
        Vector3 end = origin + direction;

        Gizmos.DrawLine(origin, end);

        Vector3 right = Vector3.Cross(direction.normalized, Vector3.up).normalized * arrowLength * 0.2f;
        Vector3 up = Vector3.up * arrowLength * 0.2f;
        Gizmos.DrawLine(end, end - direction.normalized * arrowLength * 0.3f + right);
        Gizmos.DrawLine(end, end - direction.normalized * arrowLength * 0.3f - right);
        Gizmos.DrawLine(end, end - direction.normalized * arrowLength * 0.3f + up);
        Gizmos.DrawLine(end, end - direction.normalized * arrowLength * 0.3f - up);
    }
}