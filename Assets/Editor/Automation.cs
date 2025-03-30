using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(AutoReferenceLinker))]
public class AutoReferenceLinkerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        AutoReferenceLinker linker = (AutoReferenceLinker)target;

        if (GUILayout.Button("Auto-Assign UniqueIDs"))
        {
            AssignUniqueIDs(linker);
        }

        if (GUILayout.Button("Link All References"))
        {
            linker.LinkAllReferences(); // Now accessible
            EditorUtility.SetDirty(linker); // Mark as modified
        }
    }

    private void AssignUniqueIDs(AutoReferenceLinker linker)
    {
        // Use the public RootObjects property
        foreach (GameObject root in linker.RootObjects)
        {
            if (root == null) continue;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                UniqueID uid = child.GetComponent<UniqueID>();
                if (uid == null)
                {
                    uid = child.gameObject.AddComponent<UniqueID>();
                    EditorUtility.SetDirty(child.gameObject);
                }
            }
        }
    }
}