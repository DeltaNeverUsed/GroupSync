using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using USPPNet;
using VRC.SDKBase;

[RequireComponent(typeof(Rigidbody))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupObjectSync : GroupCustomSync
{
    [Space(20)]
    [HideInInspector] public int fakeSyncId = -1;

    [HideInInspector] public FakeObjectSync fakeSync;
    [HideInInspector] public bool hasPickup; 
    [HideInInspector] public VRC_Pickup pickup;
    
    public override void OnPickup()
    {
        StartSync();
    }

    public override void OnDrop()
    {
        //UnSync(false);
    }

    public void StartSync()
    {
        if (psm.groupManager.local_group == -1)
            return;
        
        if (fakeSyncId != -1 && fakeSync.used && Networking.IsOwner(fakeSync.gameObject)) return;
        psm.local_object.close_group_joinings(psm.groupManager.local_group);
        psm.local_object.request_start_sync(networkId, Networking.LocalPlayer.playerId);
    }

    public void FinishSync(int newSyncId)
    {
        if (newSyncId == -1)
        {
            Debug.LogError("Got Fake id -1 for some reason");
            return;
        }
        
        SetVariableInLocalGroup("fakeSyncId", newSyncId, true, false);
        CallFunctionInLocalGroup("enableFakeSync", true, true);

        if (!Networking.IsOwner(fakeSync.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, fakeSync.gameObject);
            Debug.Log("Not owner yet >:(");
        }
    }

    public void enableFakeSync()
    {
        fakeSync = gosm.syncedObjects[fakeSyncId];
        fakeSync.used = true;
        fakeSync.gameObject.SetActive(true);
    }
    
    public void LeaveCallback()
    {
        UnSync();
    }

    public void UnSync(bool d = true, int target = -1)
    {
        drop = d;
        SetVariableInLocalGroup("usTar", target == -1 ? fakeSyncId : target, true, false);
        CallFunctionInLocalGroup("NetUnSync", true, true);
    }

    public int usTar = -1;
    public bool drop = true;
    public void NetUnSync()
    {
        if (usTar != -1)
            gosm.syncedObjects[usTar].UnSync();

        fakeSyncId = -1;

        if (drop && hasPickup)
            pickup.Drop();
        drop = false;
        usTar = -1;
    }


    private void Start()
    {
        StartNet();
        SubLeaveGroupCallback();
        
        gosm.AddRealObject(this);
        pickup = GetComponent<VRC_Pickup>();

        hasPickup = pickup != null;
        Debug.Log(hasPickup);
    }


    private void Update()
    {
        if (psm.groupManager.local_group == -1)
        {
            if (hasPickup)
                pickup.pickupable = false;
            return;
        }
        if (hasPickup)
            pickup.pickupable = true;
        
        if (fakeSync != null && !fakeSync.used)
            fakeSyncId = -1;
        
        if (fakeSyncId == -1)
            return;
        fakeSync = gosm.syncedObjects[fakeSyncId];

        var fakeSyncTransform = fakeSync.transform;
        if (Networking.IsOwner(fakeSync.gameObject))
        {
            fakeSyncTransform.position = transform.position;
            fakeSyncTransform.rotation = transform.rotation;
            if (hasPickup)
                fakeSync.pickedUp = pickup.IsHeld;
        }
        else
        {
            transform.position = fakeSyncTransform.position;
            transform.rotation = fakeSyncTransform.rotation;
            if (hasPickup)
                pickup.pickupable = !fakeSync.pickedUp || !pickup.DisallowTheft;
        }
    }
}