using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalAnimation : MonoBehaviour
{
    public Transform animSkeletonRoot;
    public Transform physicsSkeletonRoot;

    public Transform[] physicsBones;
    public Transform[] animBones;
    public UnrealConstraint[] constraintsInParent;
    public Rigidbody[] rigidbodies;

    UnrealConstraint[] constraints;
    private void OnEnable()
    {
        constraints = GetComponentsInChildren<UnrealConstraint>();
        foreach (UnrealConstraint constraint in constraints) {
            ConfigurableJoint joint = constraint.Joint;
            Rigidbody childRB = joint.connectedBody;
            if (childRB)
            {
                Collider[] parentColliders = joint.GetComponents<Collider>();
                Collider[] childColliders = childRB.GetComponents<Collider>();
                foreach (Collider parentCollider in parentColliders)
                {
                    foreach (Collider chidCollider in childColliders)
                    {
                        Physics.IgnoreCollision(parentCollider, chidCollider);
                    }
                }
            }
        }
    }

    private void Start()
    {
        for (int i = 0; i < constraintsInParent.Length; i++) {
            UnrealConstraint constraint = constraintsInParent[i];
            if (constraint) {
                JointDrive angularDrive = new JointDrive();
                angularDrive.positionSpring = 100000;
                angularDrive.positionDamper = 5;
                angularDrive.maximumForce = 1000000;

                constraint.Joint.rotationDriveMode = RotationDriveMode.Slerp;

                constraint.Joint.angularXMotion = ConfigurableJointMotion.Free;
                constraint.Joint.angularYMotion = ConfigurableJointMotion.Free;
                constraint.Joint.angularZMotion = ConfigurableJointMotion.Free;

                constraint.Joint.angularXDrive = angularDrive;
                constraint.Joint.angularYZDrive = angularDrive;
                constraint.Joint.slerpDrive = angularDrive;
            }
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < animBones.Length; i++) {
            Transform animBone = animBones[i];
            if (constraintsInParent[i] && !rigidbodies[i].isKinematic)
            {
                Quaternion worldRotation = physicsBones[i].parent.rotation * animBone.localRotation;
                constraintsInParent[i].SetConnectedBodyWorldSpaceRotationTarget(worldRotation);
            }
            else if (rigidbodies[i])
            {
                Rigidbody rb = rigidbodies[i];
                rb.position = animBone.position;
                rb.rotation = animBone.rotation;
            }
            else {
                physicsBones[i].localPosition = animBone.localPosition;
                physicsBones[i].localRotation = animBone.localRotation;
            }
        }
    }
}
