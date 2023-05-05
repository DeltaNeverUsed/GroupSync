using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupObjectSyncManager : UdonSharpBehaviour
{
    public FakeObjectSync[] syncedObjects = Array.Empty<FakeObjectSync>();
    
    public DataDictionary syncedRealObjects = new DataDictionary();

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

    private int _updateActive;
    private void FixedUpdate()
    {
        _updateActive++;
        if (_updateActive%8 != 0)
            return;

        foreach (var syncObj in syncedObjects)
        {
            if (Networking.IsOwner(syncObj.gameObject))
                continue;
            syncObj.gameObject.SetActive(true);
        }
        
    }

    public void AddRealObject(GroupObjectSync obj)
    {
        syncedRealObjects.Add(obj.networkId, obj);
    }

    public int GetFakeSync()
    {
        for (int i = 0; i < syncedObjects.Length; i++)
        {
            if (syncedObjects[i].group != -1)
                continue;
            syncedObjects[i].gameObject.SetActive(true);
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
            if (obj.FakeSyncId != -1)
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
            obj.FakeSyncId = -1;
        }
    }
}