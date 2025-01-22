using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TGeneratorT))]
public class TGeneratorTEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get the target object
        TGeneratorT generator = (TGeneratorT)target;

        // Draw the default inspector
        DrawDefaultInspector();

        // Add a slider for maxSlopeHeight
        generator.maxSlopeHeight = EditorGUILayout.Slider("Max Slope Height", generator.maxSlopeHeight, 0f, 10f);

        // Regenerate terrain whenever the slider value changes
        if (GUI.changed)
        {
            generator.GenerateTerrain();
        }

        // Add a button to manually trigger terrain generation
        if (GUILayout.Button("Generate Terrain"))
        {
            generator.GenerateTerrain();
        }

        if (GUILayout.Button("Delete Terrain"))
        {
            generator.DeleteTerrain();
        }
    }
}
