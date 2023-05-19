using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using USPPNet;
using VRC.SDKBase;

[RequireComponent(typeof(Rigidbody))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupObjectSync : UdonSharpBehaviour
{
    public int networkId = -1;
    
    [Space]
    
    public USPPNetEveryPlayerManager playerManager;
    public GroupObjectSyncManager syncManager;
    public GroupManager groupManager;
    [HideInInspector] public int FakeSyncId = -1;
    [HideInInspector] public bool PickedUp = false;

    public FakeObjectSync fakeSync;
    private bool _hasPickup;
    private VRC_Pickup _pickup;
    
    public override void OnPickup()
    {
        playerManager.local_object.close_group_joinings(groupManager.local_group);
        StartSync();
    }

    public override void OnDrop()
    {
        //UnSync(false);
    }

    public void StartSync()
    {
        if (groupManager.local_group == -1)
            return;

        playerManager.local_object.request_start_sync(networkId, Networking.LocalPlayer.playerId);
    }

    public void FinishSync(int newSyncId)
    {
        if (newSyncId == -1)
        {
            Debug.LogError("Got Fake id -1 for some reason");
            return;
        }
        
        FakeSyncId = newSyncId;
        fakeSync = syncManager.syncedObjects[FakeSyncId];
        
        if (!Networking.IsOwner(fakeSync.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, fakeSync.gameObject);
            Debug.Log("Not owner yet >:(");
        }
        fakeSync.group = groupManager.local_group;
        fakeSync.objectId = networkId;
    }

    public void UnSync(bool drop = true)
    {
        if (fakeSync != null)
        {
            fakeSync.group = -1;
            fakeSync.objectId = -1;
            fakeSync.pickedUp = false;
        }

        FakeSyncId = -1;
        PickedUp = false;

        if (drop && _hasPickup)
            _pickup.Drop();
    }


    private void Start()
    {
        syncManager.AddRealObject(this);
        _pickup = GetComponent<VRC_Pickup>();

        _hasPickup = _pickup != null;
        Debug.Log(_hasPickup);
    }


    private void Update()
    {
        if (groupManager.local_group == -1)
        {
            if (_hasPickup)
                _pickup.pickupable = false;
            return;
        }
        if (_hasPickup)
            _pickup.pickupable = true;
        
        if (fakeSync != null && fakeSync.objectId == -1)
            FakeSyncId = -1;
        if (FakeSyncId == -1)
            return;
        fakeSync = syncManager.syncedObjects[FakeSyncId];

        var fakeSyncTransform = fakeSync.transform;
        if (Networking.IsOwner(fakeSync.gameObject))
        {
            fakeSyncTransform.position = transform.position;
            fakeSyncTransform.rotation = transform.rotation;
            if (_hasPickup)
                PickedUp = _pickup.IsHeld;
        }
        else
        {
            transform.position = fakeSyncTransform.position;
            transform.rotation = fakeSyncTransform.rotation;
            if (_hasPickup)
                _pickup.pickupable = !fakeSync.pickedUp || !_pickup.DisallowTheft;
        }
    }
}