using TMPro;
using UnityEngine;

[ExecuteAlways]
public sealed class DeveloperPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text tutorialStatus;
    [SerializeField] private TMP_Text gamesStatus;
    [SerializeField] private TMP_Text[] nameLabels;
    [SerializeField, Tooltip("名单唯一编辑入口。格式为 Role: Name，编辑预览和运行显示会自动生成。")]
    private string[] developerNames = new string[7];

#if UNITY_EDITOR
    private void OnEnable() => QueueCreditsPreview();
    private void OnValidate() => QueueCreditsPreview();

    private void OnDisable() => UnityEditor.EditorApplication.delayCall -= UpdateCreditsPreview;

    private void QueueCreditsPreview()
    {
        // OnValidate can run while Unity is deserializing; update UI on the next editor tick.
        UnityEditor.EditorApplication.delayCall -= UpdateCreditsPreview;
        UnityEditor.EditorApplication.delayCall += UpdateCreditsPreview;
    }

    private void UpdateCreditsPreview()
    {
        if (this == null || Application.IsPlaying(gameObject)) return;
        RefreshNames();
    }
#endif

    public void Open()
    {
        Refresh();
        panelRoot.transform.SetAsLastSibling();
        panelRoot.SetActive(true);
    }

    public void Close() => panelRoot.SetActive(false);

    [ContextMenu("Toggle Tutorial Completed")]
    public void ToggleTutorialCompleted()
    {
        LocalPlayerProgress.TutorialCompleted = !LocalPlayerProgress.TutorialCompleted;
        Refresh();
#if UNITY_EDITOR
        Debug.Log($"Tutorial Completed: {LocalPlayerProgress.TutorialCompleted}. Applies to the next Start.", this);
#endif
    }

    private void Refresh()
    {
        tutorialStatus.text = "Tutorial";
        gamesStatus.text = "Games Started: " + LocalPlayerProgress.GamesStarted;
        RefreshNames();
    }

    private void RefreshNames()
    {
        if (nameLabels == null) return;
        for (int i = 0; i < nameLabels.Length; i++)
        {
            if (nameLabels[i] == null) continue;
            string name = developerNames != null && i < developerNames.Length ? developerNames[i] ?? "" : "";
            int separator = name.IndexOf(':');
            string text = string.IsNullOrWhiteSpace(name) ? $"{i + 1:00}  ---"
                : separator < 0 ? name
                : $"<size=65%><color=#83ADC6>{name.Substring(0, separator).ToUpperInvariant()}</color></size>\n{name.Substring(separator + 1).Trim()}";
            if (nameLabels[i].text == text) continue;
            nameLabels[i].text = text;
#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject))
            {
                UnityEditor.EditorUtility.SetDirty(nameLabels[i]);
                if (nameLabels[i].gameObject.scene.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(nameLabels[i].gameObject.scene);
            }
#endif
        }
    }
}
