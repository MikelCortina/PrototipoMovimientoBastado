using Fusion;
using UnityEngine;

public class ManualBoneIK : NetworkBehaviour
{
    private Animator animator;

    [Header("General")]
    public bool ikActive = true;
    public Transform objTarget;

    [Header("Body Aim Weights")]
    [Range(0f, 1f)] public float spineWeight = 0.15f;
    [Range(0f, 1f)] public float chestWeight = 0.25f;
    [Range(0f, 1f)] public float shoulderWeight = 0.35f;

    public float rotationSpeed = 7f;

    [Header("Vertical Limits (per bone)")]
    public float maxUpAngle = 25f;
    public float maxDownAngle = -15f;

    // Bones
    private Transform spine;
    private Transform chest;
    private Transform rightUpperArm;
    private Transform leftUpperArm;

    // --- NETWORK ---
    // En Fusion, [Networked] reemplaza a OnPhotonSerializeView. 
    // Fusion sincroniza esto automáticamente por la red.
    [Networked] private Vector3 NetworkAimPosition { get; set; }

    public enum VerticalAxis { X, Y, Z }

    [Header("Rig Axis")]
    public VerticalAxis verticalAxis = VerticalAxis.Z; // Mixamo suele ser Z


    void Start()
    {
        animator = GetComponentInChildren<Animator>();

        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
    }

    // En Fusion, los cambios de estado (como actualizar una variable [Networked]) 
    // deben hacerse en FixedUpdateNetwork para asegurar la precisión de los ticks y el rollback.
    public override void FixedUpdateNetwork()
    {
        if (objTarget == null) return;

        bool isMine = Object.HasInputAuthority || (Runner.Topology == Topologies.Shared && Object.HasStateAuthority);

        if (isMine)
        {
            NetworkAimPosition = objTarget.position;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        // Validamos que el objeto exista en la red de Fusion antes de calcular IK
        if (animator == null || !Object || !Object.IsValid) return;

        bool isMine = Object.HasInputAuthority || (Runner.Topology == Topologies.Shared && Object.HasStateAuthority);

        // El dueño usa el target real al instante. Los demás clientes usan la variable sincronizada.
        Vector3 aimPos = (isMine && objTarget != null) ? objTarget.position : NetworkAimPosition;

        /* ---------- HEAD LOOK IK ---------- */
        if (ikActive)
        {
            // Parámetros: (peso global, peso del CUERPO, peso de la cabeza, peso de los ojos, clamp)
            // ¡Poniendo el peso del cuerpo a 0f evitamos que gire todo el modelo!
            animator.SetLookAtWeight(1f, 0f, 0.7f, 1f, 0.8f);
            animator.SetLookAtPosition(aimPos);
        }
        else
        {
            animator.SetLookAtWeight(0f);
        }

        if (!ikActive) return;

        /* ---------- BODY AIM DISTRIBUTION ---------- */
        RotateBone(spine, aimPos, spineWeight);
        RotateBone(chest, aimPos, chestWeight);
        RotateBone(rightUpperArm, aimPos, shoulderWeight);
        RotateBone(leftUpperArm, aimPos, shoulderWeight * 0.5f);
    }

    void RotateBone(Transform bone, Vector3 targetPos, float weight)
    {
        if (bone == null || weight <= 0f) return;

        Vector3 dirWorld = (targetPos - bone.position).normalized;

        Quaternion targetWorldRot = Quaternion.LookRotation(dirWorld, animator.transform.up);

        Quaternion localTargetRot =
            Quaternion.Inverse(bone.parent.rotation) * targetWorldRot;

        Vector3 euler = localTargetRot.eulerAngles;

        // Normalizamos todos
        euler.x = NormalizeAngle(euler.x);
        euler.y = NormalizeAngle(euler.y);
        euler.z = NormalizeAngle(euler.z);

        // 🔥 CLAMP SOLO EN EL EJE CORRECTO
        switch (verticalAxis)
        {
            case VerticalAxis.X:
                euler.x = Mathf.Clamp(euler.x, maxDownAngle, maxUpAngle);
                break;

            case VerticalAxis.Y:
                euler.y = Mathf.Clamp(euler.y, maxDownAngle, maxUpAngle);
                break;

            case VerticalAxis.Z:
                euler.z = Mathf.Clamp(euler.z, maxDownAngle, maxUpAngle);
                break;
        }

        Quaternion clampedLocalRot = Quaternion.Euler(euler);
        Quaternion finalWorldRot = bone.parent.rotation * clampedLocalRot;

        // Nota: Al usar Time.deltaTime dentro de OnAnimatorIK (que corre en el Update de Unity),
        // la suavidad del "proxy" (los otros jugadores) dependerá de su framerate local, 
        // lo cual está bien para la rotación de los huesos visuales.
        bone.rotation = Quaternion.Slerp(
            bone.rotation,
            finalWorldRot,
            Time.deltaTime * rotationSpeed * weight
        );
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}