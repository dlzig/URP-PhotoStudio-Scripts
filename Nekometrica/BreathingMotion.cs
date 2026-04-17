using UnityEngine;
using VRM;

public class BreathingMotion : MonoBehaviour
{
    [SerializeField] float breathSpeed = 0.3f;      // 1サイクル = 1/breathSpeed 秒
    [SerializeField] float chestAmount = 1.5f;      // 胸の回転量（度）
    [SerializeField] float spineAmount = 0.8f;      // 背骨の回転量（度）
    [SerializeField] float shoulderAmount = 0.3f;   // 肩の上下量

    private Animator _animator;
    private Transform _chest;
    private Transform _spine;
    private Transform _leftShoulder;
    private Transform _rightShoulder;

    private Quaternion _chestBase;
    private Quaternion _spineBase;
    private Quaternion _lShoulderBase;
    private Quaternion _rShoulderBase;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _chest        = _animator.GetBoneTransform(HumanBodyBones.Chest);
        _spine        = _animator.GetBoneTransform(HumanBodyBones.Spine);
        _leftShoulder = _animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        _rightShoulder= _animator.GetBoneTransform(HumanBodyBones.RightShoulder);

        // 初期姿勢を記録
        if (_chest)         _chestBase      = _chest.localRotation;
        if (_spine)         _spineBase      = _spine.localRotation;
        if (_leftShoulder)  _lShoulderBase  = _leftShoulder.localRotation;
        if (_rightShoulder) _rShoulderBase  = _rightShoulder.localRotation;
    }

    void LateUpdate()  // ← Animatorの後に上書きするためLateUpdate
    {
        float breath = Mathf.Sin(Time.time * breathSpeed * Mathf.PI * 2f);

        if (_chest)
            _chest.localRotation = _chestBase *
                Quaternion.Euler(breath * chestAmount, 0, 0);

        if (_spine)
            _spine.localRotation = _spineBase *
                Quaternion.Euler(breath * spineAmount, 0, 0);

        if (_leftShoulder)
            _leftShoulder.localRotation = _lShoulderBase *
                Quaternion.Euler(-breath * shoulderAmount, 0, 0);

        if (_rightShoulder)
            _rightShoulder.localRotation = _rShoulderBase *
                Quaternion.Euler(-breath * shoulderAmount, 0, 0);
    }
}