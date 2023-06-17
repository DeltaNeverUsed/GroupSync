using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

[RequireComponent(typeof(VRCObjectSync))]
[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class FakeObjectSync : UdonSharpBehaviour
{
    [SerializeField] private GroupManager gm;
    
    [UdonSynced] [HideInInspector] public int target = -1;
    [UdonSynced] [HideInInspector] public int group = -1;
    [UdonSynced] [HideInInspector] public bool pickedUp;

    [HideInInspector] public int id;

    public void Start()
    {
        if (gm == null)
        {
            gm = GameObject.Find("GroupNetworkingStuff").GetComponent<GroupManager>();
            if (gm == null)
                Debug.LogError("Couldn't find GroupNetworkingStuff");
        }
    }

    public void UnSync()
    {
        target = -1;
        group = -1;
        pickedUp = false;
    }

    private int _lastTarget = -1;
    private int ownerCheck = 0;
    public void FixedUpdate()
    {
        ownerCheck--;
        if (ownerCheck < 0 && group == -1 && Networking.IsMaster && !Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            ownerCheck = 120;
        }
        
        if (_lastTarget != target && group == gm.local_group)
        {
            if (!gm.syncManager.syncedRealObjects.TryGetValue(target, out var data)) return;
            var obj = (GroupObjectSync)data.Reference;

            obj.fakeSyncId = id;
            obj.fakeSync = this;
        }
        _lastTarget = target;
    }
    
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (player != null && player.IsValid())
            if (player.IsOwner(gameObject))
                UnSync();
    }
}
