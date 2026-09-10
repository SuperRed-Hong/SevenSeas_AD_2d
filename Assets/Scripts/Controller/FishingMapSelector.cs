using UnityEngine;

// Select before scene entry and FishSpawner.Start query the active map's obstacles.
[DefaultExecutionOrder(-1000)]
public sealed class FishingMapSelector : MonoBehaviour
{
    [SerializeField, Tooltip("三张地图的公共父对象。初始化会开启它及其祖先；选图组件应放在独立、常开的场景入口上。")]
    private Transform mapContainer;
    [SerializeField, Tooltip("所有候选地图，不受编辑器中的激活状态影响。每次场景加载等概率选一张，允许重复。")]
    private GameObject[] maps;

    public GameObject SelectedMap { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        // Entry code may call this as well. Pausing or enabling the entry again must not reroll.
        if (SelectedMap != null) return;
        if (maps == null || maps.Length == 0)
        {
            Debug.LogError("FishingMapSelector requires map roots.", this);
            return;
        }
        // The candidate roots already identify their container. Recover an omitted
        // Inspector reference instead of leaving the authored preview maps active.
        if (mapContainer == null && maps[0] != null)
            mapContainer = maps[0].transform.parent;
        if (mapContainer == null)
        {
            Debug.LogError("FishingMapSelector requires map roots with a common parent.", this);
            return;
        }
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] == null || maps[i].transform.parent != mapContainer)
            {
                Debug.LogError("FishingMapSelector maps must be direct child map roots.", this);
                return;
            }
            for (int j = 0; j < i; j++)
                if (maps[i] == maps[j])
                {
                    Debug.LogError("FishingMapSelector contains a duplicate map root.", this);
                    return;
                }
        }

        int selected = Random.Range(0, maps.Length);
        SelectedMap = maps[selected];
        for (int i = 0; i < maps.Length; i++)
            maps[i].SetActive(i == selected);
        // Resolve map visibility explicitly, including a container hidden for scene editing.
        // Only activate this map's ancestry; unrelated roots keep their authored state.
        for (Transform ancestor = mapContainer; ancestor != null; ancestor = ancestor.parent)
            ancestor.gameObject.SetActive(true);
        // Tilemap shapes normally rebuild in LateUpdate, after FishSpawner.Start.
        foreach (var collider in SelectedMap.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapCollider2D>())
            collider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        Debug.Log($"Fishing map selected: {SelectedMap.name}", this);
    }
}
