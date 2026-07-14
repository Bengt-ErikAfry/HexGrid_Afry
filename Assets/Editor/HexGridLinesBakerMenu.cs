
// Assets/Editor/HexGridLinesBakerMenu.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class HexGridLinesBakerMenu
{
    [MenuItem("Tools/Hex/Bake Grid Lines")]
    public static void BakeSelected()
    {
        var sel = Selection.activeGameObject;
        HexGridLinesBaker baker = sel ? sel.GetComponent<HexGridLinesBaker>() : null;

        if (baker == null)
        {
            if (!EditorUtility.DisplayDialog(
                "No HexGridLinesBaker selected",
                "No GameObject with HexGridLinesBaker is selected.\n\n" +
                "Create a new one in the scene?",
                "Create", "Cancel"))
            {
                return;
            }

            var go = new GameObject("Hex Grid Lines");
            baker = go.AddComponent<HexGridLinesBaker>();
            // Optional defaults:
            //baker.pointTop = true;
            baker.worldRadius = 10;
            baker.hexSize = 1f;
            baker.lineThickness = 0.02f;

            Selection.activeGameObject = go;
        }

        baker.Bake();
    }
}
#endif
