using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Drawing;

public class ObstablePrefabGeneratorWindow : EditorWindow
{
    
    
        
    private string outputFolder = "Assets/Prefabs/Obstacles";
    private enum ColliderShape
    {
        Box,
        Capsule,
        Polygon
    }

    private ColliderShape colliderShape = ColliderShape.Box;
    
    
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


        List<Sprite> sprites = GetSelectedSprites();
        
        outputFolder = EditorGUILayout.TextField(
            new GUIContent(
                "Output Folder",
                "生成的 Prefab 保存位置，必须是 Assets 下的文件夹。"),
            outputFolder);
        EditorGUILayout.LabelField(
            "Selected Sprites Amount", sprites.Count.ToString());

        foreach (Sprite sprite in sprites)
        {
            EditorGUILayout.LabelField(sprite.name);
        }
        
        
        if (GUILayout.Button("Generate Prefabs"))
        {
            if (sprites.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "没有 Sprite",
                    "请先在 Project 中选择图片或 Sprite。",
                    "确定");
                return;
            }

            string folder = outputFolder.Trim().Replace('\\', '/').TrimEnd('/');

            if (folder != "Assets" && !folder.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog(
                    "路径不正确",
                    "输出目录必须位于 Assets 下。",
                    "确定");
                return;
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog(
                    "文件夹不存在",
                    "请先在 Project 中创建这个文件夹。",
                    "确定");
                return;
            }

            outputFolder = folder;

            foreach (Sprite sprite in sprites)
            {
                GeneratePrefab(sprite, outputFolder);
            }

            Debug.Log($"生成完成，共处理 {sprites.Count} 个 Sprite。");
        }
    }
    private void GeneratePrefab(Sprite sprite, string folder)
    {
        string path = AssetDatabase.GenerateUniqueAssetPath(
            $"{folder}/{sprite.name}.prefab");

        GameObject obstacle = new GameObject(sprite.name);

        try
        {
            SpriteRenderer renderer =
                obstacle.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            Vector2 size = (Vector2)sprite.bounds.size;
            Vector2 center = (Vector2)sprite.bounds.center;

            switch (colliderShape)
            {
                case ColliderShape.Box:
                {
                    BoxCollider2D collider =
                        obstacle.AddComponent<BoxCollider2D>();

                    collider.size = size;
                    collider.offset = center;
                    break;
                }

                case ColliderShape.Capsule:
                {
                    CapsuleCollider2D collider =
                        obstacle.AddComponent<CapsuleCollider2D>();

                    collider.direction = size.x >= size.y
                        ? CapsuleDirection2D.Horizontal
                        : CapsuleDirection2D.Vertical;

                    collider.size = size;
                    collider.offset = center;
                    break;
                }

                case ColliderShape.Polygon:
                {
                    obstacle.AddComponent<PolygonCollider2D>();
                    break;
                }
            }
            
            obstacle.AddComponent<ReelingObstacle>();

            PrefabUtility.SaveAsPrefabAsset(obstacle, path);
        }
        finally
        {
            DestroyImmediate(obstacle);
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
