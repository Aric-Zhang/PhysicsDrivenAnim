using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[CustomEditor(typeof(UnrealConstraint))]
public class UnrealConstraintEditor : Editor
{
    const int CONE_EDGE_COUNT = 36;

    private static Mesh coneMesh;
    private static Material material;
    private static Material backMaterial;
    private static Material fanMaterial;
    private static Material fanBackMaterial;

    EditingConstraintMode editingConstraintMode;

    /// <summary>
    /// 锥体Mesh
    /// </summary>
    public static Mesh ConeMesh
    {
        get {
            if (!coneMesh) {
                Mesh mesh = new Mesh();
                mesh.name = "cone";

                Vector3[] verts = new Vector3[CONE_EDGE_COUNT+1];
                for (int i = 0; i < CONE_EDGE_COUNT; i++) {
                    verts[i] = new Vector3(0, Mathf.Sin(Mathf.Deg2Rad * i * 360/CONE_EDGE_COUNT), Mathf.Cos(Mathf.Deg2Rad * i * 360/CONE_EDGE_COUNT));
                }
                verts[36] = Vector3.zero;
                int[] tri_indices = new int[CONE_EDGE_COUNT * 3];
                for (int i = 0; i < CONE_EDGE_COUNT-1; i++) {
                    tri_indices[i * 3] = i;
                    tri_indices[i * 3 + 1] = i + 1;
                    tri_indices[i * 3 + 2] = CONE_EDGE_COUNT;
                }
                tri_indices[(CONE_EDGE_COUNT-1) * 3] = CONE_EDGE_COUNT-1;
                tri_indices[(CONE_EDGE_COUNT-1) * 3 + 1] = 0;
                tri_indices[(CONE_EDGE_COUNT-1) * 3 + 2] = CONE_EDGE_COUNT;
                mesh.vertices = verts;
                mesh.triangles = tri_indices;
                coneMesh = mesh;
            }
            return coneMesh;
        }
    }

    /// <summary>
    /// 红色半透明材质正面
    /// </summary>
    public static Material ConeMaterial
    {
        get
        {
            if (!material)
            {
                //Shader shader = (Shader)EditorGUIUtility.LoadRequired("Standard.shader");
                Shader shader = Shader.Find("ZXT/ConeUnlitFront");
                material = new Material(shader);
                material.renderQueue = 3000;
                material.hideFlags = HideFlags.DontSaveInEditor;
                material.enableInstancing = true;
            }
            material.color = new Color(1f, 0, 0, 0.2f);
            material.SetColor("_Color", new Color(1f, 0, 0, 0.2f));
            return material;
        }
    }

    /// <summary>
    /// 红色半透明材质背面
    /// </summary>
    public static Material ConeBackMaterial
    {
        get
        {
            if (!backMaterial)
            {
                Shader shader = Shader.Find("ZXT/ConeUnlitBack");
                backMaterial = new Material(shader);
                //backMaterial.renderQueue = 3000;
                backMaterial.hideFlags = HideFlags.DontSaveInEditor;
                backMaterial.enableInstancing = true;
            }
            backMaterial.color = new Color(1f, 0, 0, 0.2f);
            backMaterial.SetColor("_Color", new Color(1f, 0, 0, 0.2f));
            return backMaterial;
        }
    }

    /// <summary>
    /// 绿色半透明材质正面
    /// </summary>

    public static Material FanMaterial
    {
        get {
            if (!fanMaterial) {
                Shader shader = Shader.Find("ZXT/ConeUnlitFront");
                fanMaterial = new Material(shader);
                //fanMaterial.renderQueue = 2000;
                fanMaterial.hideFlags = HideFlags.DontSaveInEditor;
                fanMaterial.enableInstancing = true;
            }
            fanMaterial.color = new Color(0, 1f, 0, 0.2f);
            fanMaterial.SetColor("_Color", new Color(0, 1f, 0, 0.2f));
            return fanMaterial;
        }
    }

    /// <summary>
    /// 绿色半透明材质背面
    /// </summary>
    public static Material FanBackMaterial
    {
        get
        {
            if (!fanBackMaterial)
            {
                Shader shader = Shader.Find("ZXT/ConeUnlitBack");
                fanBackMaterial = new Material(shader);
                //fanMaterial.renderQueue = 2000;
                fanBackMaterial.hideFlags = HideFlags.DontSaveInEditor;
                fanBackMaterial.enableInstancing = true;
            }
            fanBackMaterial.color = new Color(0, 1f, 0, 0.2f);
            fanBackMaterial.SetColor("_Color", new Color(0, 1f, 0, 0.2f));
            return fanBackMaterial;
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Color defaultBgColor = GUI.backgroundColor;
        editingConstraintMode = (EditingConstraintMode)EditorGUILayout.EnumPopup("Editing Constraint Space", editingConstraintMode);
    }

    private void OnSceneGUI()
    {
        //ConfigurableJoint configurableJoint = (target as UnrealConstraint).Joint;
        SerializedProperty s_p_bakedConstraintData = serializedObject.FindProperty("bakedConstraintData");
        ConfigurableJoint configurableJoint = s_p_bakedConstraintData.FindPropertyRelative("configurableJoint").objectReferenceValue as ConfigurableJoint;
        if (!configurableJoint) {
            s_p_bakedConstraintData.FindPropertyRelative("configurableJoint").objectReferenceValue = (target as UnrealConstraint).GetComponent<ConfigurableJoint>();
        }
        DrawConstraint(serializedObject.FindProperty("bakedConstraintData"), 0.5f, editingConstraintMode);
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 绘制UE4风格的Constraint
    /// </summary>
    /// <param name="s_p_bakedConstraintData">被烘焙信息，包括“约束空间”在父物体下的旋转和“被约束空间”在子物体下的旋转</param>
    /// <param name="unifiedScale">显示大小</param>
    /// <param name="editingConstraintMode">编辑模式</param>
    public static void DrawConstraint(SerializedProperty s_p_bakedConstraintData, float unifiedScale = 1,EditingConstraintMode editingConstraintMode = EditingConstraintMode.none) {
        
        ConfigurableJoint configurableJoint = s_p_bakedConstraintData.FindPropertyRelative("configurableJoint").objectReferenceValue as ConfigurableJoint;

        Transform parentTransform = configurableJoint.transform;
        Transform childTransform = null;

        Vector3 anchor = configurableJoint.anchor;
        Vector3 connectedAnchor = configurableJoint.connectedAnchor;
        Vector3 connectedWorldPosition = parentTransform.TransformPoint(anchor);

        Vector3 constraintWorldPosition = parentTransform.TransformPoint(anchor);

        if (configurableJoint.connectedBody)
        {
            childTransform = configurableJoint.connectedBody.transform;
            if (configurableJoint.autoConfigureConnectedAnchor && !Application.isPlaying) {
                connectedWorldPosition = constraintWorldPosition;
            }
            else
            {
                connectedWorldPosition = childTransform.TransformPoint(connectedAnchor);
            }
            Handles.color = Color.blue;
            Handles.DrawLine(connectedWorldPosition, childTransform.position);
        }

        Handles.color = Color.red;
        Handles.DrawLine(constraintWorldPosition, parentTransform.position);
        //计算真正有效的轴向
        Vector3 axis = configurableJoint.axis;
        if (axis.sqrMagnitude < 0.001) axis = Vector3.right;

        Vector3 secondaryAxis = configurableJoint.secondaryAxis;
        secondaryAxis = Vector3.ProjectOnPlane(secondaryAxis, axis.normalized);
        if (secondaryAxis.sqrMagnitude < 0.001) { 
            secondaryAxis = Vector3.up;
            secondaryAxis = Vector3.ProjectOnPlane(secondaryAxis, axis.normalized);
            if (secondaryAxis.sqrMagnitude < 0.001) secondaryAxis = Vector3.left;
        }

        Vector3 localXAxis = axis.normalized;
        Vector3 localYAxis = secondaryAxis.normalized;
        Vector3 localZAxis = Vector3.Cross(localXAxis, localYAxis).normalized;
        //计算约束空间在世界空间下的轴向
        Vector3 worldXAxis;
        Vector3 worldYAxis;
        Vector3 worldZAxis;

        if (configurableJoint.configuredInWorldSpace)
        {

            if (Application.isPlaying)
            {
                Quaternion initParentSpaceRotation = s_p_bakedConstraintData.FindPropertyRelative("initalLocalRotation").quaternionValue;
                worldXAxis = parentTransform.rotation * initParentSpaceRotation * Vector3.right;
                worldYAxis = parentTransform.rotation * initParentSpaceRotation * Vector3.up;
                worldZAxis = parentTransform.rotation * initParentSpaceRotation * Vector3.forward;
            }
            else {
                worldXAxis = localXAxis;
                worldYAxis = localYAxis;
                worldZAxis = localZAxis;
            }
        }
        else
        {
            worldXAxis = parentTransform.TransformDirection(axis).normalized;
            worldYAxis = parentTransform.TransformDirection(secondaryAxis).normalized;
            worldZAxis = Vector3.Cross(worldXAxis, worldYAxis);
        }

        float motionLimit = configurableJoint.linearLimit.limit;
        //渲染线性约束
        Handles.color = Color.green;
        if (configurableJoint.xMotion == ConfigurableJointMotion.Limited)
        {
            Handles.DrawLine(constraintWorldPosition + worldXAxis * motionLimit, constraintWorldPosition - worldXAxis * motionLimit);
        }
        if (configurableJoint.yMotion == ConfigurableJointMotion.Limited)
        {
            Handles.DrawLine(constraintWorldPosition + worldYAxis * motionLimit, constraintWorldPosition - worldYAxis * motionLimit);
        }
        if (configurableJoint.zMotion == ConfigurableJointMotion.Limited)
        {
            Handles.DrawLine(constraintWorldPosition + worldZAxis * motionLimit, constraintWorldPosition - worldZAxis * motionLimit);
        }

        //如果使用适应屏幕的Handles大小，则传入的unifiedScale被覆盖
        unifiedScale = HandleUtility.GetHandleSize(constraintWorldPosition) * unifiedScale;
        //渲染swing范围锥形（塔可饼）
        #region render yz cone
        Vector3 angularHandlePosition = childTransform ? connectedWorldPosition : constraintWorldPosition;
        CommandBuffer cb = new CommandBuffer();
        cb.Clear();
        Matrix4x4 matrix = Matrix4x4.TRS(angularHandlePosition, Quaternion.identity, Vector3.one * unifiedScale);
        Mesh mesh = ConeMesh;

        float angle_z_limit;
        switch (configurableJoint.angularZMotion) {
            case ConfigurableJointMotion.Free:
                angle_z_limit = 180;
                break;
            case ConfigurableJointMotion.Locked:
                angle_z_limit = 0;
                break;
            default:
                angle_z_limit = configurableJoint.angularZLimit.limit;
                break;
        }

        float angle_y_limit;
        switch (configurableJoint.angularYMotion)
        {
            case ConfigurableJointMotion.Free:
                angle_y_limit = 180;
                break;
            case ConfigurableJointMotion.Locked:
                angle_y_limit = 0;
                break;
            default:
                angle_y_limit = configurableJoint.angularYLimit.limit;
                break;
        }

        if (configurableJoint.angularYMotion != ConfigurableJointMotion.Free || configurableJoint.angularZMotion != ConfigurableJointMotion.Free)
        {
            Vector3 z_positive = worldXAxis * Mathf.Cos(angle_z_limit * Mathf.Deg2Rad) + worldYAxis * Mathf.Sin(angle_z_limit * Mathf.Deg2Rad);
            Vector3 z_negative = worldXAxis * Mathf.Cos(angle_z_limit * Mathf.Deg2Rad) - worldYAxis * Mathf.Sin(angle_z_limit * Mathf.Deg2Rad);

            Vector3 y_positive = worldXAxis * Mathf.Cos(angle_y_limit * Mathf.Deg2Rad) + worldZAxis * Mathf.Sin(angle_y_limit * Mathf.Deg2Rad);
            Vector3 y_negative = worldXAxis * Mathf.Cos(angle_y_limit * Mathf.Deg2Rad) - worldZAxis * Mathf.Sin(angle_y_limit * Mathf.Deg2Rad);

            mesh.vertices = CalculateConeVertPositions(y_positive, y_negative, z_positive, z_negative);

            cb.DrawMesh(ConeMesh, matrix, ConeMaterial);
            cb.DrawMesh(ConeMesh, matrix, ConeBackMaterial);
            Graphics.ExecuteCommandBuffer(cb);
        }

        cb.Clear();

        Mesh fan_mesh = ConeMesh;

        if (configurableJoint.angularXMotion == ConfigurableJointMotion.Limited)
        {
            float angle_x_upper_limit = configurableJoint.highAngularXLimit.limit;
            float angle_x_lower_limit = configurableJoint.lowAngularXLimit.limit;
            float angle_x_mid = (angle_x_upper_limit + angle_x_lower_limit) * 0.5f;

            Vector3 upperVec = worldYAxis * Mathf.Cos(angle_x_upper_limit * Mathf.Deg2Rad) + worldZAxis * Mathf.Sin(angle_x_upper_limit * Mathf.Deg2Rad);
            Vector3 lowerVec = worldYAxis * Mathf.Cos(angle_x_lower_limit * Mathf.Deg2Rad) + worldZAxis * Mathf.Sin(angle_x_lower_limit * Mathf.Deg2Rad);
            Vector3 midVec = worldYAxis * Mathf.Cos(angle_x_mid * Mathf.Deg2Rad) + worldZAxis * Mathf.Sin(angle_x_mid * Mathf.Deg2Rad);

            fan_mesh.vertices = CalculateConeVertPositions(upperVec, lowerVec, midVec, midVec);
            cb.DrawMesh(fan_mesh, matrix, FanMaterial);

            Graphics.ExecuteCommandBuffer(cb);
        }
        else if (configurableJoint.angularXMotion == ConfigurableJointMotion.Free) {
            fan_mesh.vertices = CalculateConeVertPositions(worldYAxis, -worldYAxis, worldZAxis, -worldZAxis);
            cb.DrawMesh(fan_mesh, matrix, FanMaterial);
            cb.DrawMesh(fan_mesh, matrix, FanBackMaterial);
            Graphics.ExecuteCommandBuffer(cb);
        }
        #endregion

        //SerializedObject serializedObject = new SerializedObject(unrealConstraint);
        //约束空间在世界空间下的旋转
        Quaternion constraintWorldRotation = Quaternion.LookRotation(worldZAxis, worldYAxis);

        if (Application.isPlaying) {
            //
        }
        else{  //更新被烘焙的旋转

            Quaternion constraintRotationInConnectedSpace;
            //SerializedProperty s_p_bakedConstraintData = serializedObject.FindProperty("bakedConstraintData");

            if (childTransform)
            {
                constraintRotationInConnectedSpace = Quaternion.Inverse(childTransform.rotation) * constraintWorldRotation;
                s_p_bakedConstraintData.FindPropertyRelative("initialRotationOffset").quaternionValue = constraintRotationInConnectedSpace;
            }
            Quaternion constraintRotationInParentSpace = Quaternion.Inverse(parentTransform.rotation) * constraintWorldRotation;
            s_p_bakedConstraintData.FindPropertyRelative("initalLocalRotation").quaternionValue = constraintRotationInParentSpace;
            //serializedObject.ApplyModifiedProperties();
        }

        if (childTransform) {  //渲染“被约束空间”的轴向
            Quaternion childAnchorRotation = s_p_bakedConstraintData.FindPropertyRelative("initialRotationOffset").quaternionValue;
            Vector3 childAnchorYAxis = (childTransform.rotation * childAnchorRotation) * Vector3.up;
            Vector3 childAnchorXAxis = (childTransform.rotation * childAnchorRotation) * Vector3.right;
            float lineWidth = 8 * unifiedScale;

            Vector3 twistWorldDisplayDirection = Quaternion.FromToRotation(childAnchorXAxis, worldXAxis) * childAnchorYAxis;

            Handles.DrawAAPolyLine(lineWidth, new Vector3[] { connectedWorldPosition, connectedWorldPosition + twistWorldDisplayDirection * unifiedScale });
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(lineWidth, new Vector3[] { connectedWorldPosition, connectedWorldPosition + childAnchorXAxis * unifiedScale });
            Handles.DrawAAPolyLine(lineWidth, new Vector3[] { connectedWorldPosition, connectedWorldPosition + worldXAxis * unifiedScale });
            Handles.ConeHandleCap(0, connectedWorldPosition + worldXAxis * unifiedScale, Quaternion.LookRotation(worldXAxis), 0.05f * unifiedScale,EventType.Repaint);
        }

        //交互式编辑anchor和axis
        if (!Application.isPlaying)
        {
            switch (editingConstraintMode)
            {
                case EditingConstraintMode.position:
                    Vector3 newWorldPosition = Handles.PositionHandle(constraintWorldPosition, constraintWorldRotation);
                    Vector3 newLocalPosition = parentTransform.InverseTransformPoint(newWorldPosition);
                    configurableJoint.anchor = newLocalPosition;
                    if (configurableJoint.autoConfigureConnectedAnchor && childTransform) {
                        Vector3 newChildSpacePosition = childTransform.InverseTransformPoint(newWorldPosition);
                        configurableJoint.connectedAnchor = newChildSpacePosition;
                    }
                    //HandleUtility.Repaint();
                    //SceneView.RepaintAll();
                    break;
                case EditingConstraintMode.rotation:
                    Quaternion newWorldRotation = Handles.RotationHandle(constraintWorldRotation, constraintWorldPosition);
                    Quaternion newLocalRotation;
                    if (configurableJoint.configuredInWorldSpace)
                    {
                        newLocalRotation = newWorldRotation;
                    }
                    else
                    {
                        newLocalRotation = Quaternion.Inverse(parentTransform.rotation) * newWorldRotation;
                    }
                    Vector3 newAxis = newLocalRotation * Vector3.right;
                    Vector3 newSecondaryAxis = newLocalRotation * Vector3.up;
                    configurableJoint.axis = newAxis;
                    configurableJoint.secondaryAxis = newSecondaryAxis;

                    //HandleUtility.Repaint();
                    //SceneView.RepaintAll();
                    break;
                case EditingConstraintMode.connectedPosition:
                    Vector3 newConnectedAnchorWorldPosition = Handles.PositionHandle(connectedWorldPosition, constraintWorldRotation);
                    Vector3 newConnectedAnchorLocalPosition = childTransform.InverseTransformPoint(newConnectedAnchorWorldPosition);
                    configurableJoint.connectedAnchor = newConnectedAnchorLocalPosition;
                    if (configurableJoint.autoConfigureConnectedAnchor && childTransform)
                    {
                        Vector3 newParentSpacePosition = parentTransform.InverseTransformPoint(newConnectedAnchorWorldPosition);
                        configurableJoint.anchor = newParentSpacePosition;
                    }
                    //HandleUtility.Repaint();
                    //SceneView.RepaintAll();
                    break;
            }
        }
    }

    static Vector3[] CalculateConeVertPositions(Vector3 y_positive, Vector3 y_negative, Vector3 z_positive, Vector3 z_negative) {
        Vector3 z_center = Vector3.Lerp(z_positive, z_negative, 0.5f);
        Vector3 y_center = Vector3.Lerp(y_positive, y_negative, 0.5f);
        Vector3[] verts = new Vector3[CONE_EDGE_COUNT + 1];
        for (int i = 0; i < CONE_EDGE_COUNT; i++)
        {
            float rad_around_x = (float)i * 360 / CONE_EDGE_COUNT * Mathf.Deg2Rad;

            float cos = Mathf.Cos(rad_around_x);
            float sin = Mathf.Sin(rad_around_x);

            Vector3 offset_y = (y_positive - y_center) * cos - y_center * sin * sin;
            Vector3 vec_limit_y = y_center + offset_y;
            Vector3 offset_z = (z_positive - z_center) * sin - z_center * cos * cos;
            Vector3 vec_limit_z = z_center + offset_z;

            Vector3 all = (vec_limit_y + vec_limit_z).normalized;
            verts[i] = all;
        }
        return verts;
    }
}

public enum EditingConstraintMode { 
    none,
    position,
    rotation,
    connectedPosition
}