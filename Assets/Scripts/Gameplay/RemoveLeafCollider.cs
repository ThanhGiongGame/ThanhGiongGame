using UnityEditor;
using UnityEngine;

public class RemoveLeafCollider
{
    [MenuItem("Tools/Remove Leaf Colliders")]
    static void Remove()
    {
        SphereCollider[] cols = Object.FindObjectsOfType<SphereCollider>(true);

        int count = 0;

        foreach (var col in cols)
        {
            if (col.gameObject.name.Contains("Flower"))
            {
                Undo.DestroyObjectImmediate(col);
                count++;
            }
        }

        Debug.Log($"Removed {count} colliders.");
    }
}