using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class RandomizeRiceRotationEditor : EditorWindow
{
    [MenuItem("Tools/Randomize Rice Rotation")]
    public static void ShowWindow()
    {
        GetWindow<RandomizeRiceRotationEditor>("Randomize Rice");
    }

    private void OnGUI()
    {
        GUILayout.Label("Randomize Rice05Grass600_OriginalModel", EditorStyles.boldLabel);

        GUILayout.Space(10);
        GUILayout.Label($"Current Scene: {SceneManager.GetActiveScene().name}");
        GUILayout.Space(10);

        if (GUILayout.Button("Randomize Y Rotation in Active Scene"))
        {
            RandomizeRotation();
        }
    }

    private static void RandomizeRotation()
    {
        // Find all Transforms in the scene to include inactive ones if needed, 
        // but typically FindObjectsOfType<Transform>(true) works in newer Unity versions.
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);
        int count = 0;

        foreach (Transform t in allTransforms)
        {
            if (t.name.Contains("Rice05Grass600_OriginalModel"))
            {
                // Record the state for Undo
                Undo.RecordObject(t, "Randomize Rice Rotation");
                
                // Keep the original X and Z rotation, only change Y
                // This ensures they rotate horizontally and don't turn upside down.
                float randomY = Random.Range(0f, 360f);
                Vector3 currentEuler = t.localEulerAngles;
                
                t.localEulerAngles = new Vector3(currentEuler.x, randomY, currentEuler.z);
                count++;
            }
        }

        Debug.Log($"Randomized Y rotation for {count} 'Rice05Grass600_OriginalModel' objects.");
    }
}
