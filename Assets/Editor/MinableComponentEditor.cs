using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(MinableComponent))]
[CanEditMultipleObjects]
public class MinableComponentEditor : Editor
{
    SerializedProperty worldRadiusProp;
    SerializedProperty hexSizeProp;
    SerializedProperty tilesDataProp; // expects a List<TileData> named 'tilesData' in MinableComponent
    ReorderableList tilesList;

    void OnEnable()
    {
        worldRadiusProp = serializedObject.FindProperty("worldRadius");
        hexSizeProp = serializedObject.FindProperty("hexSize");
        tilesDataProp = serializedObject.FindProperty("tilesData"); // you must add this field to MinableComponent

        if (tilesDataProp != null)
        {
            tilesList = new ReorderableList(serializedObject, tilesDataProp, true, true, true, true);
            tilesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Tile Data");
            tilesList.elementHeightCallback = (index) =>
            {
                var el = tilesDataProp.GetArrayElementAtIndex(index);
                // draw TileData property; allow it to expand
                return EditorGUI.GetPropertyHeight(el, true) + 4;
            };
            tilesList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var el = tilesDataProp.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUI.GetPropertyHeight(el, true)),
                                        el, new GUIContent($"Tile #{index}"), true);
            };
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(worldRadiusProp);
        EditorGUILayout.PropertyField(hexSizeProp);

        EditorGUILayout.Space();

        if (tilesList != null)
        {
            tilesList.DoLayoutList();
            EditorGUILayout.Space();

            if (GUILayout.Button("Populate TileData From Radius"))
            {
                // call into MinableComponent to populate (you still need a method on MinableComponent to do the generation)
                foreach (var t in targets)
                {
                    var mc = (MinableComponent)t;
                    mc.PopulateTilesFromRadiusEditor(); // implement this editor-safe method on MinableComponent (editor-only)
                    EditorUtility.SetDirty(mc);
                }
                serializedObject.Update();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Add a public List<TileData> tilesData to MinableComponent to edit tile data here.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}