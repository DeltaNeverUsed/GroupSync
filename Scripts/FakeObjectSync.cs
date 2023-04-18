

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class FakeObjectSync : UdonSharpBehaviour
{
    public GroupObjectSyncManager syncManager;
    public GroupManager groupManager;
    
    [UdonSynced] [HideInInspector] public int group = -1;
    [UdonSynced] [HideInInspector] public int objectId = -1;
    [UdonSynced] [HideInInspector] public bool pickedUp = false;

    [HideInInspector] public int id;
    
    private Vector3 _pos = Vector3.zero;
    private Vector3 _rot = Vector3.zero;

    [UdonSynced(UdonSyncMode.Smooth)] private Vector3 _posSynced = Vector3.zero;
    [UdonSynced(UdonSyncMode.Smooth)] private Vector3 _rotSynced = Vector3.zero;
    
    private float[] _updateTimes = new float[12];
    private int _UTIndex;

    private void Start()
    {
        for (int i = 0; i < _updateTimes.Length; i++)
        {
            _updateTimes[i] = 0.1f;
        }
    }

    public float dieTimer;
    private void Update()
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

        var obj =  syncManager.syncedRealObjects[objectId];

        if (Networking.IsOwner(gameObject))
        {
            _posSynced = transform.position;
            _rotSynced = transform.rotation * Vector3.forward; 
            pickedUp = obj.PickedUp;
        }
        else
        {
            transform.position = _posSynced;
            transform.rotation = Quaternion.LookRotation(_rotSynced);
            obj.PickedUp = pickedUp;
            obj.FakeSyncId = id;
        }
        
    }
}
