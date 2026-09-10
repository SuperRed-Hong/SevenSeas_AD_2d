using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ObstablePrefabGeneratorWindow : EditorWindow
{
    [MenuItem("Tools/Seven Seas/Generate Obstacle Prefabs")]
    private static void OpenWindow()
    {
        GetWindow<ObstablePrefabGeneratorWindow>("Obstacle Prefab Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Obstacle Prefab Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select Sprites in Project window, and then Generate Obstacle Prefabs",MessageType.Info);


        Sprite[] sprites =
            Selection.GetFiltered<Sprite>(SelectionMode.Unfiltered);
        EditorGUILayout.LabelField("Selected Sprites Amount", sprites.Length.ToString());

        foreach (Sprite sprite in sprites)
        {
            EditorGUILayout.LabelField(sprite.name);
        }
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private List<Sprite> GetSelectedSprites()
    {
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object selected in Selection.objects)
        {
            if (selected is Sprite sprite)
            {
                if (!sprites.Contains(sprite))
                    sprites.Add(sprite);
            }
            else if (selected is Texture2D texture)
            {
                string path = AssetDatabase.GetAssetPath(texture);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

                foreach (Object asset in assets)
                {
                    if (asset is Sprite childSprite && !sprites.Contains(childSprite))
                    {
                        sprites.Add(childSprite);
                    }
                }
            }
        }
        return sprites;
    }
    
}
