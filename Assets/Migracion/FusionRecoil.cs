using UnityEngine;

public class FusionRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    public float recoilRecoverySpeed = 10f;

    Vector3 recoilOffsetPos = Vector3.zero;
    Vector3 recoilOffsetRot = Vector3.zero;

    Vector3 initialPosition;
    Vector3 initialRotation;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localEulerAngles;
    }

    void Update()
    {
        recoilOffsetPos = Vector3.Lerp(recoilOffsetPos, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);
        recoilOffsetRot = Vector3.Lerp(recoilOffsetRot, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

        transform.localPosition = initialPosition + recoilOffsetPos;
        transform.localEulerAngles = initialRotation + recoilOffsetRot;
    }

    public void ApplyRecoil(Vector3 recoilKick, Vector3 recoilRotation)
    {
        recoilOffsetPos += recoilKick;
        recoilOffsetRot += recoilRotation;
    }
}