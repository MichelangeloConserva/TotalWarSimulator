using PathCreation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierPathManager : MonoBehaviour
{
    private readonly Dictionary<int, PathCreator> m_instances = new Dictionary<int, PathCreator>();
    public PathCreator this[int index]
    {
        get
        {
            PathCreator value;
            return m_instances.TryGetValue(index, out value) ? value : null;
        }
    }
    public GameObject CreateFromPrefab(GameObject prefab, Transform holder, bool enableImmediately = false)
    {
        var gameObject = Object.Instantiate(prefab, holder);
        if (!enableImmediately)
            gameObject.SetActive(false);
        Add(gameObject);
        return gameObject;
    }
    public void Add(GameObject gameObject)
    {
        m_instances[gameObject.GetInstanceID()] = gameObject.GetComponent<PathCreator>();
    }
    public void Destroy(int instanceId)
    {
        var gameObject = m_instances[instanceId];
        m_instances.Remove(instanceId);
        Object.Destroy(gameObject);
    }
    #region Singleton
    private static BezierPathManager INSTANCE = new BezierPathManager();
    static BezierPathManager()
    {
    }
    private BezierPathManager()
    {
    }
    public static BezierPathManager Instance
    {
        get { return INSTANCE; }
    }
#if UNITY_EDITOR
    // for quick play mode entering 
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        INSTANCE = new BezierPathManager();
    }
#endif
    #endregion


}
