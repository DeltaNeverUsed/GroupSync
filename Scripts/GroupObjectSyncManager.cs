using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupObjectSyncManager : UdonSharpBehaviour
{
    public FakeObjectSync[] syncedObjects = Array.Empty<FakeObjectSync>();
    
    public GroupObjectSync[] syncedRealObjects = Array.Empty<GroupObjectSync>();

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

    public int AddRealObject(GroupObjectSync obj)
    {
        var size = syncedRealObjects.Length;
        var temp_arr = new GroupObjectSync[size + 1];
        Array.Copy(syncedRealObjects, temp_arr, size);
        temp_arr[size] = obj;
        syncedRealObjects = temp_arr;

        return size;
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
        foreach (var obj in syncedRealObjects)
        {
            if (obj.FakeSyncId != -1)
                obj.UnSync();
        }
    }

    public void ResetRealFakeIds()
    {
        foreach (var obj in syncedRealObjects)
        {
            obj.FakeSyncId = -1;
        }
    }
}
