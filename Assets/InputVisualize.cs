using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputVisualize : MonoBehaviour
{
    public Transform rotationSource;
    //public Rigidbody constraintedChild;

    public UnrealConstraint[] constraints;

    private void Start()
    {
        //ConfigurableJoint joint = constraint.Joint;
        //joint.targetRotation = Quaternion.LookRotation(worldSpaceForward);
        //joint.rotationDriveMode = RotationDriveMode.Slerp;
    }

    private void Update()
    {
        //constraint.SetConnectedBodyWorldSpaceRotationTarget(rotationSource.rotation);
        //constraintedChild.rotation = rotationSource.rotation;
    }

    private void FixedUpdate()
    {
        //
        //constraint.SetConnectedBodyPositionAndRotationDirectly(rotationSource.position, rotationSource.rotation,20);
        foreach (UnrealConstraint constraint in constraints)
        {
            //constraint.SetConnectedBodyWorldSpaceRotation(rotationSource.rotation, 20);
            constraint.SetConnectedBodyWorldSpaceRotationTarget(rotationSource.rotation);
        }
        //constraintedChild.rotation = rotationSource.rotation;
        //constraintedChild.position = rotationSource.position;
    }

    private void OnDrawGizmos()
    {


    }
}
