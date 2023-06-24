using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(GroupObjectSyncManager), true)]
public class GroupObjectSyncManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Assign new IDs"))
        {
            var gos = Resources.FindObjectsOfTypeAll<GroupObjectSync>();
            foreach (var obj in gos)
                obj.networkId = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            var gcs = Resources.FindObjectsOfTypeAll<GroupCustomSync>();
            foreach (var obj in gcs)
                obj.networkId = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
        
        DrawDefaultInspector();
    }
}

#endif

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupObjectSyncManager : UdonSharpBehaviour
{
    public FakeObjectSync[] syncedObjects = Array.Empty<FakeObjectSync>();
    
    public DataDictionary syncedRealObjects = new DataDictionary();
    public DataDictionary syncedCustomObjects = new DataDictionary();

    private void Start()
    {
        syncedObjects = new FakeObjectSync[transform.childCount];
        var i = 0;
        foreach (Transform child in transform)
        {
            syncedObjects[i] = child.GetComponent<FakeObjectSync>();
            syncedObjects[i].id = i;
            i++;
        }
        
    }

    private int _activeIndex = 0;
    public void FixedUpdate()
    {
        var obj = syncedObjects[_activeIndex];
        if (Networking.IsOwner(obj.gameObject) && obj.group == -1)
            obj.gameObject.SetActive(false);
        else 
            obj.gameObject.SetActive(true);
        _activeIndex++;
        if (_activeIndex >= syncedObjects.Length)
            _activeIndex = 0;
    }

    public void AddRealObject(GroupObjectSync obj)
    {
        if (!syncedRealObjects.ContainsKey(obj.networkId))
            syncedRealObjects.Add(obj.networkId, obj);
        else
            Debug.LogError($"Duplicate ID on real object: {obj.name}");
    }
    public void AddCustomObject(GroupCustomSync obj)
    {
        if (!syncedCustomObjects.ContainsKey(obj.networkId))
            syncedCustomObjects.Add(obj.networkId, obj);
        else
            Debug.LogError($"Duplicate ID on custom object: {obj.name}, ID is: {obj.networkId}");
    }

    // Only the master should call this to avoid FakeID collisions
    public int GetFakeSync()
    {
        for (int i = 0; i < syncedObjects.Length; i++)
        {
            if (syncedObjects[i] == null || syncedObjects[i].group != -1)
                continue;
            return i;
        }
        return -1;
    }

    public int GetFakeSyncFromObjectInGroup(int objectId, int group)
    {
        for (int i = 0; i < syncedObjects.Length; i++)
        {
            if (syncedObjects[i] == null || syncedObjects[i].group != group || syncedObjects[i].target != objectId)
                continue;
            return i;
        }
        return -1;
    }

    // Call before changing group
    public void LocalDropAll()
    {
        var values = syncedRealObjects.GetValues();
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            var obj = (GroupObjectSync)value.Reference;
            if (obj != null && obj.fakeSyncId != -1)
                obj.UnSync();
        }
    }

    public void ResetRealFakeIds()
    {
        var values = syncedRealObjects.GetValues();
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            var obj = (GroupObjectSync)value.Reference;
            obj.UnSync();
        }
    }
}