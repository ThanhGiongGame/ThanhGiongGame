using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraController))]
public class CameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            return;
        }

        CameraController controller = (CameraController)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Use this test pause instead of Unity's toolbar Pause. The match stops, but the Game view keeps rendering camera changes.",
            MessageType.Info);

        if (Time.timeScale > 0f)
        {
            if (GUILayout.Button("Pause Match For Camera Tuning"))
            {
                controller.PauseForCameraTuning();
            }
        }
        else if (GUILayout.Button("Resume Match"))
        {
            controller.ResumeFromCameraTuning();
        }

        if (Time.timeScale > 0f)
        {
            return;
        }

        controller.PreviewFromInspector();
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
        Repaint();
    }
}
