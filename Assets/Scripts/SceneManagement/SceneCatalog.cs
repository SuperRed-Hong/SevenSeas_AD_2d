using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
[CreateAssetMenu(
    fileName = "SceneCatalog",
    menuName = "Seven Seas/SceneCatalog")]
public sealed class SceneCatalog : ScriptableObject
{
    [Serializable]
    private struct SceneEntry
    {
        public E_SceneID id;

        [FormerlySerializedAs("scenName")]
        public string sceneName;
    }

    [SerializeField] private List<SceneEntry> sceneEntries = new List<SceneEntry>();

    public bool TryGetSceneName(E_SceneID sceneID, out string sceneName)
    {
        foreach (var entry in sceneEntries)
        {
            if (entry.id == sceneID)
            {
                sceneName = entry.sceneName;
                return true;
            }
        }
        sceneName = null;
        return false;
    }
}
