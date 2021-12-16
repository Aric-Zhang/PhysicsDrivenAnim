using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PhysicalAnimation))]
public class PhysicalAnimationEditor : Editor
{
    private int deepestIndent = 0;
    PhysicalBoneEditorData[] boneData;
    private int[] parentBoneStack;

    private void OnEnable()
    {
        RegenerateInspectorSkeletonHierarchy();
    }
    void SettingUpAnimPhysicsMap (){
        PhysicalAnimation physicalAnimation = target as PhysicalAnimation;
        if (physicalAnimation.animSkeletonRoot && physicalAnimation.physicsSkeletonRoot) {
            Transform animRoot = physicalAnimation.animSkeletonRoot;
            Transform physicsRoot = physicalAnimation.physicsSkeletonRoot;

            physicalAnimation.animBones = animRoot.GetComponentsInChildren<Transform>();
            physicalAnimation.physicsBones = new Transform[physicalAnimation.animBones.Length];
            physicalAnimation.rigidbodies = new Rigidbody[physicalAnimation.animBones.Length];
            physicalAnimation.constraintsInParent = new UnrealConstraint[physicalAnimation.animBones.Length];

            Transform[] temp_physicsBones = physicsRoot.GetComponentsInChildren<Transform>();
            for (int i = 0; i < physicalAnimation.animBones.Length; i++) {
                Transform animBone = physicalAnimation.animBones[i];
                foreach (Transform physicsBone in temp_physicsBones) {
                    //按名称匹配，除了root以外
                    if (physicsBone.name == animBone.name) {
                        physicalAnimation.physicsBones[i] = physicsBone;
                        break;
                    }
                }
            }
            physicalAnimation.physicsBones[0] = physicalAnimation.physicsSkeletonRoot;
            
            for (int i = 0; i < physicalAnimation.physicsBones.Length; i++) {
                Rigidbody rb = physicalAnimation.physicsBones[i].GetComponent<Rigidbody>();
                physicalAnimation.rigidbodies[i] = rb;
            }
            List<UnrealConstraint> constraintList = new List<UnrealConstraint>(physicalAnimation.physicsSkeletonRoot.GetComponentsInChildren<UnrealConstraint>());
            for (int i = 0; i < physicalAnimation.rigidbodies.Length; i++) {
                Rigidbody rb = physicalAnimation.rigidbodies[i];
                if (rb) {
                    UnrealConstraint constraint = constraintList.Find((c) => { return c.Joint.connectedBody == rb; });
                    if (constraint) {
                        physicalAnimation.constraintsInParent[i] = constraint;
                        constraintList.Remove(constraint);
                    }
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        if(GUILayout.Button("Setting Up Maps")){
            SettingUpAnimPhysicsMap();
        }
        //EditorGUILayout.PropertyField(serializedObject.FindProperty("constraintsInParent"));
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animSkeletonRoot"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("physicsSkeletonRoot"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            RegenerateInspectorSkeletonHierarchy();
            SettingUpAnimPhysicsMap();
        }
        InspectorSkeletonHierarchy();

        SerializedProperty s_p_constraints = serializedObject.FindProperty("constraints");
        //EditorGUILayout.PropertyField(s_p_constraints);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Bodies")) {
            GenerateBodies(0.1f);
        }
        if (GUILayout.Button("Generate Constraints"))
        {
            GenerateConstraints();
        }
        if (GUILayout.Button("Generate Colliders"))
        {
            GenerateColliders();
        }
        GUILayout.EndHorizontal();
    }

    void InspectorSkeletonHierarchy()
    {
        if (boneData.Length > 0)
        {
            parentBoneStack[0] = -1;
            int new_deepest_indent = 0;
            int indent = 0;

            for (int i = 0; i < boneData.Length; i++)
            {
                PhysicalBoneEditorData currentBoneData = boneData[i];
                //骨骼被删除
                if (!currentBoneData.BoneTransform) {
                    RegenerateInspectorSkeletonHierarchy();
                    return;
                }
                //reset to top
                if (currentBoneData.parentIndex == -1)
                    indent = 0;
                //骨骼层级结构改变
                else if (boneData[currentBoneData.parentIndex].BoneTransform != currentBoneData.BoneTransform.parent) {
                    RegenerateInspectorSkeletonHierarchy();
                    return;
                }
                //dive
                else if (currentBoneData.parentIndex == i - 1)
                {
                    parentBoneStack[++indent] = i - 1;
                }
                //pop up
                else if (currentBoneData.parentIndex != parentBoneStack[indent])
                {
                    int parentIndex = boneData[i].parentIndex;
                    while (indent > 0)
                    {
                        if (parentIndex == parentBoneStack[--indent]) break;
                    }
                }
                if (indent > 0)
                {
                    PhysicalBoneEditorData parentBoneData = boneData[boneData[i].parentIndex];
                    currentBoneData.ancestorsAllExpand = parentBoneData.ancestorsAllExpand && parentBoneData.expand;
                }
                else
                {
                    currentBoneData.ancestorsAllExpand = true;
                }
                if (currentBoneData.ancestorsAllExpand)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(15 * indent);
                    GUILayout.BeginVertical();

                    currentBoneData.expand = EditorGUILayout.Foldout(currentBoneData.expand, boneData[i].BoneTransform.name, true);

                    if (currentBoneData.expand)
                    {
                        SerializedObject s_o = new SerializedObject(boneData[i].constraints);
                        SerializedProperty s_p_constraints = s_o.FindProperty("constraints");

                        SerializedProperty s_p_rigidbody = s_o.FindProperty("rigidbody");
                        if (s_p_rigidbody.objectReferenceValue != null)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(s_p_rigidbody, new GUIContent(" Rigidbody"), new GUILayoutOption[] { GUILayout.ExpandWidth(true)});
                            GUILayout.Space(15 * (deepestIndent - indent));
                            bool clear = GUILayout.Button("x", new GUILayoutOption[] { GUILayout.ExpandWidth(false) });
                            if (clear)
                            {
                                Component component = s_p_rigidbody.objectReferenceValue as Rigidbody;
                                GameObject go = component.gameObject;
                                Joint[] joints = go.GetComponents<Joint>();
                                UnrealConstraint[] constraints = go.GetComponents<UnrealConstraint>();
                                for (int j = 0; j < constraints.Length; j++) {
                                    DestroyImmediate(constraints[j]);
                                }
                                for (int j = 0; j < joints.Length; j++)
                                {
                                    DestroyImmediate(joints[j]);
                                }
                                DestroyImmediate(component);
                                currentBoneData.UpdateConstraints();
                                return;
                            }
                            GUILayout.EndHorizontal();
                        }
                        for (int j = 0; j < s_p_constraints.arraySize; j++)
                        {
                            SerializedProperty s_p_unrealConstraint = s_p_constraints.GetArrayElementAtIndex(j);
                            UnrealConstraint constraint = s_p_unrealConstraint.objectReferenceValue as UnrealConstraint;
                            if (constraint.constraintName == "")
                            {
                                name = constraint.transform.name;
                                if (constraint.Joint.connectedBody)
                                {
                                    name = name + " to " + constraint.Joint.connectedBody.name;
                                }
                                constraint.constraintName = name;
                            }
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(s_p_unrealConstraint, new GUIContent(" Constraint " + j.ToString()), new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
                            GUILayout.Space(15 * (deepestIndent - indent));
                            bool clear = GUILayout.Button("x", new GUILayoutOption[] { GUILayout.ExpandWidth(false) });
                            if (clear)
                            {
                                Component component = s_p_unrealConstraint.objectReferenceValue as UnrealConstraint;
                                Component joint = (component as UnrealConstraint).Joint;
                                DestroyImmediate(component);
                                DestroyImmediate(joint);
                                currentBoneData.UpdateConstraints();
                                return;
                            }
                            GUILayout.EndHorizontal();
                        }
                        SerializedProperty s_p_colliders = s_o.FindProperty("colliders");
                        for (int j = 0; j < s_p_colliders.arraySize; j++)
                        {
                            SerializedProperty s_p_collider = s_p_colliders.GetArrayElementAtIndex(j);
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(s_p_collider, new GUIContent(" Collider " + j.ToString()), new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
                            GUILayout.Space(15 * (deepestIndent - indent));
                            bool clear = GUILayout.Button("x", new GUILayoutOption[] { GUILayout.ExpandWidth(false)});
                            if (clear) {
                                Component component = s_p_collider.objectReferenceValue as Collider;
                                DestroyImmediate(component);
                                currentBoneData.UpdateConstraints();
                                return;
                            }
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.EndVertical();
                    //GUILayout.Space(15 * (deepestIndent - indent));
                    GUILayout.EndHorizontal();
                    if (indent > new_deepest_indent) new_deepest_indent = indent;
                }
            }
            //next frame
            deepestIndent = new_deepest_indent;
        }


    }

    void RegenerateInspectorSkeletonHierarchy() {
        Transform rootBone = (target as PhysicalAnimation).physicsSkeletonRoot;
        if (!rootBone) {
            boneData = new PhysicalBoneEditorData[0];
            return;
        }
        Transform[] childBones = rootBone.GetComponentsInChildren<Transform>();
        boneData = new PhysicalBoneEditorData[childBones.Length];
        for (int i = 0; i < childBones.Length; i++)
        {
            Transform bone_transform = childBones[i];
            boneData[i] = new PhysicalBoneEditorData(bone_transform);
            if (bone_transform.parent)
            {
                for (int j = 0; j < i; j++)
                {
                    if (childBones[j] == bone_transform.parent)
                    {
                        boneData[i].parentIndex = j;
                        break;
                    }
                }
            }
            boneData[i].constraints.constraints = bone_transform.GetComponents<UnrealConstraint>();
            boneData[i].constraints.rigidbody = bone_transform.GetComponent<Rigidbody>();
            boneData[i].constraints.colliders = bone_transform.GetComponents<Collider>();
        }
        boneData[0].ancestorsAllExpand = true;
        parentBoneStack = new int[childBones.Length];
    }

    void GenerateBodies(float minBoneScale) {
        if (boneData.Length == 0) return;
        List<int> indiecs_need_to_add_rb = new List<int>();
        for (int i = 0; i < boneData.Length; i++) {
            PhysicalBoneEditorData data = boneData[i];
            if (data.parentIndex == -1) continue;
            Transform bone = data.BoneTransform;
            Transform parentBone = boneData[data.parentIndex].BoneTransform;
            float boneSacle = (bone.position - parentBone.position).magnitude;
            if (boneSacle < minBoneScale) continue;
            else{
                indiecs_need_to_add_rb.Add(i);
                int parentIndex = data.parentIndex;
                while (parentIndex > -1) {
                    if (indiecs_need_to_add_rb.Contains(parentIndex)) break;
                    indiecs_need_to_add_rb.Add(parentIndex);
                    PhysicalBoneEditorData parentBoneData = boneData[parentIndex];
                    parentIndex = parentBoneData.parentIndex;
                }
            }
        }
        foreach (int i in indiecs_need_to_add_rb) {
            PhysicalBoneEditorData data = boneData[i];
            data.BoneTransform.gameObject.AddComponent<Rigidbody>();
        }
        RegenerateInspectorSkeletonHierarchy();
    }

    void GenerateConstraints() {
        if (boneData.Length == 0) return;
        Transform rootBone = (target as PhysicalAnimation).physicsSkeletonRoot;
        if (!rootBone) return;

        for (int i = 0; i < boneData.Length; i++) {
            PhysicalBoneEditorData currentBoneData = boneData[i];
            if (currentBoneData.parentIndex == -1) continue;
            PhysicalBoneEditorData parentBoneData = boneData[currentBoneData.parentIndex];
            if (currentBoneData.constraints.rigidbody && parentBoneData.constraints.rigidbody) {
                UnrealConstraint[] parentConstraints = parentBoneData.constraints.constraints;
                bool constraint_found = false;
                for (int j = 0; j < parentConstraints.Length; j++) {
                    ConfigurableJoint joint = parentConstraints[i].Joint;
                    if (joint.connectedBody == currentBoneData.constraints.rigidbody) {
                        constraint_found = true;
                        break;
                    }
                }
                if (!constraint_found) {
                    GenerateConstraintOnParent(parentBoneData.constraints.rigidbody, currentBoneData.constraints.rigidbody);
                }
            }
        }
    }

    void GenerateConstraintOnParent(Rigidbody parentRB, Rigidbody childRB, ConfigurableJointMotion defaultAngularMotion = ConfigurableJointMotion.Limited, float defaultAngularLimit = 45) {
        GameObject parent = parentRB.gameObject;
        ConfigurableJoint joint = parent.AddComponent<ConfigurableJoint>();
        joint.connectedBody = childRB;
        joint.anchor = childRB.transform.localPosition;
        joint.autoConfigureConnectedAnchor = true;
        joint.connectedAnchor = Vector3.zero;
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        joint.angularXMotion = defaultAngularMotion;
        joint.angularYMotion = defaultAngularMotion;
        joint.angularZMotion = defaultAngularMotion;

        joint.secondaryAxis = joint.transform.InverseTransformDirection(childRB.transform.right).normalized;
        Vector3 zAxis = Vector3.ProjectOnPlane(joint.transform.InverseTransformDirection(parent.transform.position - childRB.transform.position), joint.secondaryAxis);
        if (zAxis.sqrMagnitude > 0.001f)
        {
            Vector3 xAxis = Vector3.Cross(joint.secondaryAxis, zAxis).normalized;
            joint.axis = xAxis;
        }

        SoftJointLimit highAngularXLimit = new SoftJointLimit();
        highAngularXLimit.limit = defaultAngularLimit;
        joint.highAngularXLimit = highAngularXLimit;

        SoftJointLimit lowAngularXLimit = new SoftJointLimit();
        lowAngularXLimit.limit = -defaultAngularLimit;
        joint.lowAngularXLimit = lowAngularXLimit;

        SoftJointLimit angularYLimit = new SoftJointLimit();
        angularYLimit.limit = defaultAngularLimit;
        joint.angularYLimit = angularYLimit;

        SoftJointLimit angularZLimit = new SoftJointLimit();
        angularZLimit.limit = defaultAngularLimit;
        joint.angularZLimit = angularZLimit;

        UnrealConstraint constraint = parent.AddComponent<UnrealConstraint>();
        constraint.InitConstraintData();

        SerializedObject s_o_constraint = new SerializedObject(constraint);
        SerializedProperty s_p_bakedConstraintData = s_o_constraint.FindProperty("bakedConstraintData");
        s_p_bakedConstraintData.FindPropertyRelative("configurableJoint").objectReferenceValue = joint;

        Transform childTransform = childRB.transform;
        Transform parentTransform = childRB.transform;
        Quaternion constraintRotationInConnectedSpace;
        Quaternion constraintWorldRotation = parentRB.transform.rotation;

        if (childTransform)
        {
            constraintRotationInConnectedSpace = Quaternion.Inverse(childTransform.rotation) * constraintWorldRotation;
            s_p_bakedConstraintData.FindPropertyRelative("initialRotationOffset").quaternionValue = constraintRotationInConnectedSpace;
        }
        Quaternion constraintRotationInParentSpace = Quaternion.Inverse(parentTransform.rotation) * constraintWorldRotation;
        s_p_bakedConstraintData.FindPropertyRelative("initalLocalRotation").quaternionValue = constraintRotationInParentSpace;

        s_o_constraint.ApplyModifiedProperties();
    }

    void GenerateColliders() {
        if (boneData.Length == 0) return;
        Transform rootBone = (target as PhysicalAnimation).transform;
        //获取所有的SkinnedMeshRenderer
        SkinnedMeshRenderer[] smrs = rootBone.GetComponentsInChildren<SkinnedMeshRenderer>();
        //用来存储bone和它对应的顶点在骨骼空间的位置
        Dictionary<Transform, List<Vector3>> bone_vertex_pos_dict = new Dictionary<Transform, List<Vector3>>();

        foreach (SkinnedMeshRenderer smr in smrs)
        {
            Dictionary<Transform, List<int>> bone_vertex_dict = new Dictionary<Transform, List<int>>();
            Transform[] bones = smr.bones;
            //sharedMesh用来获取蒙皮权重信息
            Mesh sharedMesh = smr.sharedMesh;
            //bakedMesh用莱获取顶点位置
            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);
            BoneWeight[] boneWeights = sharedMesh.boneWeights;
            for (int i = 0; i < boneWeights.Length; i++)
            {
                Transform bone1 = bones[boneWeights[i].boneIndex0];
                Transform bone2 = bones[boneWeights[i].boneIndex1];
                Transform bone3 = bones[boneWeights[i].boneIndex2];
                Transform bone4 = bones[boneWeights[i].boneIndex3];
                Transform[] vertex_bones = new Transform[] { bone1, bone2, bone3, bone4 };
                foreach (Transform bone in vertex_bones)
                {
                    if (bone)
                    {
                        if (!bone_vertex_dict.ContainsKey(bone))
                        {
                            bone_vertex_dict.Add(bone, new List<int>());
                        }
                        bone_vertex_dict[bone].Add(i);
                    }
                }
            }
            //模型空间转世界空间的矩阵
            Matrix4x4 modelTransformationMatrix = smr.transform.localToWorldMatrix;
            Vector3[] vertWorldPos = new Vector3[bakedMesh.vertexCount];
            Vector3[] vertModelPos = bakedMesh.vertices;
            for (int i = 0; i < bakedMesh.vertexCount; i++)
            {
                Vector4 vert4 = vertModelPos[i];
                vert4.w = 1;  //齐次矩阵乘法带上位移的必要步骤
                vertWorldPos[i] = modelTransformationMatrix * vert4;  //自动取前三位
            }
            foreach (KeyValuePair<Transform, List<int>> kvp in bone_vertex_dict)
            {
                Transform bone = kvp.Key;
                if (!bone_vertex_pos_dict.ContainsKey(kvp.Key))
                {
                    bone_vertex_pos_dict.Add(bone, new List<Vector3>());
                }
                foreach (int bone_index in kvp.Value)
                {
                    bone_vertex_pos_dict[bone].Add(bone.InverseTransformPoint(vertWorldPos[bone_index])); //世界空间转骨骼空间
                }
            }
        }

        for (int i = 0; i < boneData.Length; i++) {
            PhysicalBoneEditorData currentBoneData = boneData[i];
            if (!currentBoneData.constraints.rigidbody) continue;
            Transform bone = currentBoneData.BoneTransform;
            if (bone_vertex_pos_dict.ContainsKey(bone)) {
                List<Vector3> boneSpaceVertsList = bone_vertex_pos_dict[bone];
                if (boneSpaceVertsList.Count == 0)
                {
                    //
                }
                else {
                    Vector3 max = boneSpaceVertsList[0];  
                    Vector3 min = boneSpaceVertsList[0];
                    foreach (Vector3 boneSapceVert in boneSpaceVertsList)
                    {  //计算包围盒大小
                        max.x = Mathf.Max(max.x, boneSapceVert.x);
                        max.y = Mathf.Max(max.y, boneSapceVert.y);
                        max.z = Mathf.Max(max.z, boneSapceVert.z);
                        min.x = Mathf.Min(min.x, boneSapceVert.x);
                        min.y = Mathf.Min(min.y, boneSapceVert.y);
                        min.z = Mathf.Min(min.z, boneSapceVert.z);
                    }
                    CapsuleCollider capsuleCollider = bone.gameObject.AddComponent<CapsuleCollider>();
                    Vector3 size = max - min;
                    if (size.x >= size.y && size.x >= size.z)  //最长的边为胶囊体的方向
                    {
                        capsuleCollider.direction = 0;
                        //Vector3 center = new Vector3((max.x + min.x) * 0.5f, 0, 0);
                        //capsuleCollider.center = center;
                        capsuleCollider.height = max.x - min.x;
                        capsuleCollider.radius = (size.y + size.z) * 0.25f;  //剩下两个边平均一下是直径，再取一半为半径
                    }
                    else if (size.y >= size.z && size.y >= size.x)
                    {
                        capsuleCollider.direction = 1;
                        //Vector3 center = new Vector3(0, (max.y + min.y) * 0.5f, 0);
                        //capsuleCollider.center = center;
                        capsuleCollider.height = max.y - min.y;
                        capsuleCollider.radius = (size.x + size.z) * 0.25f;
                    }
                    else {
                        capsuleCollider.direction = 2;
                        //Vector3 center = new Vector3(0, 0,(max.z + min.z) * 0.5f);
                        //capsuleCollider.center = center;
                        capsuleCollider.height = max.z - min.z;
                        capsuleCollider.radius = (size.x + size.y) * 0.25f;
                    }
                    capsuleCollider.center = Vector3.Lerp(min, max, 0.5f);  //使用包围盒中心作为胶囊体中心
                }
            }
        }
    }

    private void OnSceneGUI()
    {
        PhysicalAnimation physicalAnimation = target as PhysicalAnimation;
        foreach (PhysicalBoneEditorData data in boneData) {
            SerializedObject s_o = new SerializedObject(data.constraints);
            SerializedProperty s_p_unrealConstraints = s_o.FindProperty("constraints");
            for(int i = 0; i < s_p_unrealConstraints.arraySize; i++)
            {
                SerializedProperty s_p_unrealConstraint = s_p_unrealConstraints.GetArrayElementAtIndex(i);
                UnrealConstraint unrealConstraint = s_p_unrealConstraint.objectReferenceValue as UnrealConstraint;
                SerializedObject s_o_unrealConstraint = new SerializedObject(unrealConstraint);
                SerializedProperty s_p_bakedConstraintData = s_o_unrealConstraint.FindProperty("bakedConstraintData");
                UnrealConstraintEditor.DrawConstraint(s_p_bakedConstraintData, 0.5f);
            }
        }
    }

    [System.Serializable]
    public class PhysicalBoneEditorData {
        Transform transform;
        public bool expand = true;
        public bool ancestorsAllExpand;
        public int parentIndex = -1;
        public UnrealConstraintWrapper constraints;

        public Transform BoneTransform {
            get => transform;
        }

        public PhysicalBoneEditorData(Transform transform) {
            this.transform = transform;
            parentIndex = -1;
            constraints = ScriptableObject.CreateInstance<UnrealConstraintWrapper>();
        }

        public void UpdateConstraints()
        {
            constraints.constraints = transform.gameObject.GetComponents<UnrealConstraint>();
            constraints.rigidbody = transform.gameObject.GetComponent<Rigidbody>();
            constraints.colliders = transform.gameObject.GetComponents<Collider>();
        }
    }

    public class UnrealConstraintWrapper : ScriptableObject {
        public UnrealConstraint[] constraints;
        public Rigidbody rigidbody;
        public Collider[] colliders;
    }
}
