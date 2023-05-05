

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[RequireComponent(typeof(VRCObjectSync))]
[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class FakeObjectSync : UdonSharpBehaviour
{
    public GroupObjectSyncManager syncManager;
    public GroupManager groupManager;
    
    [UdonSynced] [HideInInspector] public int group = -1;
    [UdonSynced] [HideInInspector] public int objectId = -1;
    [UdonSynced] [HideInInspector] public bool pickedUp = false;

    [HideInInspector] public int id;

    public float dieTimer;
    private void FixedUpdate()
    {
        if (objectId == -1)
        {
            dieTimer += Time.deltaTime;
            if (dieTimer > 1f)
            {
                dieTimer = 0f;
                gameObject.SetActive(false);
            }
            return;
        }
        
        if(groupManager.local_group != group)
            return;

        if (!syncManager.syncedRealObjects.TryGetValue(objectId, out var dataOut))
            return;
        var obj = (GroupObjectSync)dataOut.Reference;

        if (Networking.IsOwner(gameObject))
            pickedUp = obj.PickedUp;
        else
        {
            obj.PickedUp = pickedUp;
            obj.FakeSyncId = id;
        }
        
    }
}
