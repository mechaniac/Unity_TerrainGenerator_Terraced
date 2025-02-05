using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;


namespace tree_gen
{

    [CustomEditor(typeof(procedural_stem_01))]
    public class procedural_stem_01_Editor : Editor
    {
        SerializedProperty _numOfBranches;
        SerializedProperty _branchScale;
        SerializedProperty _branchScaleRandom;

        SerializedProperty _smoosh;
        SerializedProperty _randomise;
        SerializedProperty _randomOffsetMax;
        SerializedProperty _seed;

        SerializedProperty _branchParents;

        SerializedProperty _branchPrefabs;
        SerializedProperty _leavesPrefab;
        SerializedProperty _leafSize;
        SerializedProperty _subleavesPrefab;
        SerializedProperty _subLeaveArrangement;
        SerializedProperty _subLeaveAmount;
        SerializedProperty _subLeaveSize;
        SerializedProperty _subLeaveRadius;
        SerializedProperty _subLeaveOffset;
        SerializedProperty _subLeafOffsetRotation;


        SerializedProperty _rotations;
        SerializedProperty _rotationRandoms;

        SerializedProperty _showGizmos;
        public bool showGizmos = true;

        void OnEnable()
        {
            _branchParents = serializedObject.FindProperty("branchParents");

            _branchPrefabs = serializedObject.FindProperty("branchPrefabs");
            _leavesPrefab = serializedObject.FindProperty("leavesPrefab");
            _leafSize = serializedObject.FindProperty("leafSize");
            _subleavesPrefab = serializedObject.FindProperty("subleavesPrefab");
            _subLeaveArrangement = serializedObject.FindProperty("subLeaveArrangement");
            _subLeaveAmount = serializedObject.FindProperty("subLeaveAmount");
            _subLeaveRadius = serializedObject.FindProperty("subLeaveRadius");
            _subLeaveOffset = serializedObject.FindProperty("subLeaveOffset");
            _subLeafOffsetRotation = serializedObject.FindProperty("subLeafOffsetRotation");
            _subLeaveSize = serializedObject.FindProperty("subLeaveSize");

            _numOfBranches = serializedObject.FindProperty("numberOfBranches");
            _branchScale = serializedObject.FindProperty("branchScale");
            _branchScaleRandom = serializedObject.FindProperty("branchScaleRandom");
            _smoosh = serializedObject.FindProperty("smoosh");
            _randomise = serializedObject.FindProperty("randomise");
            _randomOffsetMax = serializedObject.FindProperty("randomOffsetMax");

            _seed = serializedObject.FindProperty("seed");

            _rotations = serializedObject.FindProperty("rotations");
            _rotationRandoms = serializedObject.FindProperty("rotationRandoms");
            _showGizmos = serializedObject.FindProperty("showGizmos");
        }

        float CalculatePropertiesCheckSum()
        {
            float subLeafOffset_f = _subLeafOffsetRotation.vector3Value.x + _subLeafOffsetRotation.vector3Value.y + _subLeafOffsetRotation.vector3Value.z + _subLeaveOffset.vector3Value.x + _subLeaveOffset.vector3Value.y + _subLeaveOffset.vector3Value.z;

            float subLeaves = _subLeaveArrangement.enumValueIndex + _subLeaveAmount.intValue + _subLeaveSize.floatValue + _subLeaveRadius.floatValue + subLeafOffset_f;
            return _numOfBranches.intValue + _leafSize.floatValue + _branchScale.floatValue + _branchScaleRandom.floatValue + _smoosh.floatValue + _randomise.floatValue + _randomOffsetMax.floatValue + subLeaves +_seed.intValue ; //removed: 
        }

        float CalculatePropertiesCheckVector3Sum()
        {
            return AddVectorUp(_rotations.vector3Value) + AddVectorUp(_rotationRandoms.vector3Value);
        }

        float AddVectorUp(Vector3 v)
        {
            return v.x + v.y + v.z;
        }

        public override void OnInspectorGUI()
        {
            procedural_stem_01 s = (procedural_stem_01)target;
            serializedObject.Update();
            // DrawDefaultInspector();
            float _checksum = CalculatePropertiesCheckSum();
            float _checkVectorSum = CalculatePropertiesCheckVector3Sum();
            // Create a slider for the specified public variable
            EditorGUILayout.PropertyField(_showGizmos);
            EditorGUILayout.PropertyField(_branchParents);
            EditorGUILayout.PropertyField(_branchPrefabs);
            EditorGUILayout.PropertyField(_leavesPrefab);
            EditorGUILayout.PropertyField(_leafSize);
            EditorGUILayout.PropertyField(_subleavesPrefab);
            EditorGUILayout.PropertyField(_subLeaveAmount);
            EditorGUILayout.PropertyField(_subLeaveArrangement);
            EditorGUILayout.Slider(_subLeaveSize, 0.01f, 10, new GUIContent("SubLeaf Scale"));
            EditorGUILayout.Slider(_subLeaveRadius, 0.01f, 10, new GUIContent("SubLeaf Radius"));
            EditorGUILayout.PropertyField(_subLeaveOffset);
            EditorGUILayout.PropertyField(_subLeafOffsetRotation);
            EditorGUILayout.PropertyField(_seed);
            EditorGUILayout.IntSlider(_numOfBranches, 1, 12, new GUIContent("Number of Branches"));
            EditorGUILayout.Slider(_branchScale, 0.1f, 10, new GUIContent("Branch Scale"));
            EditorGUILayout.Slider(_branchScaleRandom, 0, 1, new GUIContent("Branch Scale Random"));

            EditorGUILayout.Slider(_smoosh, -1, 1, new GUIContent("Smooosh"));
            EditorGUILayout.Slider(_randomise, 0, 1, new GUIContent("Randomise"));
            EditorGUILayout.PropertyField(_randomOffsetMax);

            EditorGUILayout.PropertyField(_rotations, new GUIContent("Axis Rotations"));

            GUILayout.BeginHorizontal();
            // EditorGUI.indentLevel++;
            // X Slider
            GUILayout.Label("Axis Randomness", GUILayout.ExpandWidth(true), GUILayout.MinWidth(60), GUILayout.MaxWidth(900));
            GUILayout.FlexibleSpace();
            _rotationRandoms.vector3Value = new Vector3(
                GUILayout.HorizontalSlider(_rotationRandoms.vector3Value.x, 0f, 1f, GUILayout.ExpandWidth(true), GUILayout.MinWidth(58), GUILayout.MaxWidth(300)),
                _rotationRandoms.vector3Value.y,
                _rotationRandoms.vector3Value.z
            );

            // Y Slider
            _rotationRandoms.vector3Value = new Vector3(
                _rotationRandoms.vector3Value.x,
                GUILayout.HorizontalSlider(_rotationRandoms.vector3Value.y, 0f, 1f, GUILayout.ExpandWidth(true), GUILayout.MinWidth(58), GUILayout.MaxWidth(300)),
                _rotationRandoms.vector3Value.z
            );

            // Z Slider
            _rotationRandoms.vector3Value = new Vector3(
                _rotationRandoms.vector3Value.x,
                _rotationRandoms.vector3Value.y,
                GUILayout.HorizontalSlider(_rotationRandoms.vector3Value.z, 0f, 1f, GUILayout.ExpandWidth(true), GUILayout.MinWidth(58), GUILayout.MaxWidth(300))
            );
            // EditorGUI.indentLevel--;
            GUILayout.EndHorizontal();




            if (GUI.changed && (_checksum != CalculatePropertiesCheckSum() || _checkVectorSum != CalculatePropertiesCheckVector3Sum()))
            {
                // Debug.Log("prev: " + previousIntValue + " current: " +_numOfBranches.intValue);
                EditorApplication.delayCall += () =>
                {
                    s.InstantiateBranchesAndLeaves(_seed.intValue);
                };
            }
            // Apply modifications to the serialized object
            serializedObject.ApplyModifiedProperties();



            if (GUILayout.Button("Instantiate Branches"))
            {
                s.InstantiateBranchesAndLeaves(_seed.intValue);
            }

            if (GUILayout.Button("destroy Branches"))
            {
                s.DestroyExistingBranches();
            }

        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmoForMyScript(procedural_stem_01 stem, GizmoType gizmoType)
        {
            if (stem.showGizmos && stem.leaveParents != null)
            {
                Handles.color = Color.green;

                foreach (Transform leaveParent in stem.leaveParents)
                {
                    if (leaveParent != null)
                    {
                        Handles.DrawWireCube(leaveParent.position, Vector3.one);
                    }
                }
            }

        }
    }
}