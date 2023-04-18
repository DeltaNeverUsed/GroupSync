using UdonSharp;
using UnityEngine;
using USPPNet;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupObjectSync : UdonSharpBehaviour
{
    public USPPNetEveryPlayerManager playerManager;
    public GroupObjectSyncManager syncManager;
    public GroupManager groupManager;
    [HideInInspector] public int FakeSyncId = -1;
    [HideInInspector] public int ObjectId = -1;
    [HideInInspector] public bool PickedUp = false;

    private FakeObjectSync _fakeSync;
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

        if (FakeSyncId == -1)
        {
            var tempId = syncManager.GetFakeSync();
            Debug.Log($"Got: {tempId}");
            if (tempId == -1)
                return;
            FakeSyncId = tempId;
        }

        _fakeSync = syncManager.syncedObjects[FakeSyncId];

        if (_fakeSync.group != -1 && !Networking.IsOwner(_fakeSync.gameObject))
            playerManager.local_object.request_unsync(ObjectId, Networking.LocalPlayer.playerId);
        else
            FinishSync();
    }

    public void FinishSync()
    {
        Networking.SetOwner(Networking.LocalPlayer, _fakeSync.gameObject);
        _fakeSync.group = groupManager.local_group;
        _fakeSync.objectId = ObjectId;
    }

    public void UnSync(bool drop = true)
    {
        if (_fakeSync != null)
        {
            _fakeSync.group = -1;
            _fakeSync.objectId = -1;
            _fakeSync.pickedUp = false;
        }

        FakeSyncId = -1;
        PickedUp = false;

        if (drop && _hasPickup)
            _pickup.Drop();
    }


    private void Start()
    {
        ObjectId = syncManager.AddRealObject(this);
        _pickup = GetComponent<VRC_Pickup>();

        _hasPickup = _pickup != null;
        Debug.Log(_hasPickup);
    }


    private void Update()
    {
        if (groupManager.local_group == -1)
        {
            _pickup.pickupable = false;
            return;
        }
        _pickup.pickupable = true;
        
        if (_fakeSync != null && _fakeSync.objectId == -1)
            FakeSyncId = -1;
        if (FakeSyncId == -1)
            return;
        _fakeSync = syncManager.syncedObjects[FakeSyncId];

        var fakeSyncTransform = _fakeSync.transform;
        if (Networking.IsOwner(_fakeSync.gameObject))
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
                _pickup.pickupable = !_fakeSync.pickedUp || !_pickup.DisallowTheft;
        }
    }
}