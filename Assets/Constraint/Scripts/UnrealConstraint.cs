using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class UnrealConstraint : MonoBehaviour
{
    public string constraintName;
    [SerializeField]
    BakedConstraintData bakedConstraintData;

    /// <summary>
    /// 在child空间下的constraintRotation
    /// </summary>
    public Quaternion ChildAnchorRotation {
        get => bakedConstraintData.ChildAnchorRotation;
    }

    /// <summary>
    /// 在parent空间下的constraintRotation
    /// </summary>
    public Quaternion ParentAnchorRotation {
        get => bakedConstraintData.ParentAnchorRotation;
    }

    public ConfigurableJoint Joint
    {
        get {
            if (!bakedConstraintData.configurableJoint) {
                bakedConstraintData.configurableJoint = GetComponent<ConfigurableJoint>();
            }
            return bakedConstraintData.configurableJoint;
        }
    }

    public void InitConstraintData() {
        if (bakedConstraintData == null) bakedConstraintData = new BakedConstraintData();
    }

    private void Reset()
    {
        ConfigurableJoint configurableJoint = GetComponent<ConfigurableJoint>();
        if (bakedConstraintData == null) {
            bakedConstraintData = new BakedConstraintData();
        }
        if (configurableJoint)
        {
            bakedConstraintData.configurableJoint = configurableJoint;
        }
    }

    public void SetConnectedBodyWorldSpaceRotationTarget(Quaternion worldRotation) {
        bakedConstraintData.SetConnectedBodyWorldSpaceRotationTarget(worldRotation);
    }
}

[System.Serializable]
public class BakedConstraintData {
    public ConfigurableJoint configurableJoint;
    [SerializeField]
    Quaternion initialRotationOffset = Quaternion.identity;
    [SerializeField]
    Quaternion initalLocalRotation = Quaternion.identity;

    /// <summary>
    /// 在child空间下的constraintRotation
    /// </summary>
    public Quaternion ChildAnchorRotation
    {
        get => initialRotationOffset;
    }

    /// <summary>
    /// 在parent空间下的constraintRotation
    /// </summary>
    public Quaternion ParentAnchorRotation
    {
        get => initalLocalRotation;
    }

    //to-do:用rigidbody的velocity写一套motor控制，作为motor的另外一环
    public void SetConnectedBodyWorldSpaceRotationTarget(Quaternion worldRotation)
    {
        ConfigurableJoint joint = configurableJoint;
        Quaternion constraintWorldRotation = configurableJoint.transform.rotation * ParentAnchorRotation;
        Quaternion constraintSpaceSourceRotation = Quaternion.Inverse(constraintWorldRotation) * worldRotation;
        Quaternion rotationTarget = constraintSpaceSourceRotation * ChildAnchorRotation;
        joint.targetRotation = rotationTarget;
    }

    //Rotation Only KeyFramed Controller

    
}
