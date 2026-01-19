using UnityEngine;

public class Recoil : MonoBehaviour
{
    [Header("Recoil Settings")]
  
    public float recoilRecoverySpeed = 10f; // velocidad a la que vuelve

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
        // Suavemente reducimos el recoil hacia cero
        recoilOffsetPos = Vector3.Lerp(recoilOffsetPos, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);
        recoilOffsetRot = Vector3.Lerp(recoilOffsetRot, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

        // Aplicamos el recoil relativo a la posición original
        transform.localPosition = initialPosition + recoilOffsetPos;
        transform.localEulerAngles = initialRotation + recoilOffsetRot;
    }

    // Llamar en cada disparo
    public void ApplyRecoil( Vector3 recoilKick, Vector3 recoilRotation )
    {
        // Sumamos un nuevo recoil
        recoilOffsetPos += recoilKick;
        recoilOffsetRot += recoilRotation;
    }
}
