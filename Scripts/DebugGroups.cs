
using TMPro;
using UdonSharp;
using UnityEngine.Serialization;
using VRC.SDKBase;

public class DebugGroups : UdonSharpBehaviour
{

    public GroupManager groupManager;
    public GroupObjectSyncManager syncManager;
    
    
    public TextMeshProUGUI groupinfo;
    public TextMeshProUGUI fakeObjectInfo;
    public TextMeshProUGUI objectInfo;

    public void Log()
    {
        // TODO: make logging in game, instead of using Debug.Log();
    }
    
    public void FixedUpdate()
    {
        DebugDisplayGroup();
    }

    private string format_bool(bool b)
    {
        return b ? $"<color=green>{b}</color>" : $"<color=red>{b}</color>";
    }

    private void DebugDisplayGroup()
    {
        if (groupManager.groups.Length < groupManager.maxGroups * groupManager.maxPlayersPerGroup)
            return;

        var t = ""; // Groups
        for (int x = 0; x < groupManager.maxGroups; x++)
        {
            var groupName = groupManager.joinable.Length == groupManager.maxGroups ? groupManager.joinable[x] : "Not Host";
            var players = "";
            for (int y = 0; y < groupManager.maxPlayersPerGroup; y++)
            {
                if (groupManager.groups[groupManager.Pos2Index(x, y)] == -1) continue;
                var guy = VRCPlayerApi.GetPlayerById(groupManager.groups[groupManager.Pos2Index(x, y)]);
                var displayName = guy != null ? guy.displayName : "N/A";
                players += $"({displayName}, {groupManager.groups[groupManager.Pos2Index(x, y)]})";
            }
            t += $"Group: {x}, name: {groupName}, contains: {players}\n";
        }

        groupinfo.text = t;
        
        
        t = ""; // Fake Objects
        for (int x = 0; x < syncManager.syncedObjects.Length; x++)
        {
            var obj = syncManager.syncedObjects[x];
            var guy = Networking.GetOwner(obj.gameObject);
            if (guy == null)
                continue;
            
            t += $"fakeId: {obj.id}, Enabled: {format_bool(obj.gameObject.activeSelf)}, group: {obj.group}, CObject: {obj.objectId}, Owner: ({guy.displayName}, {guy.playerId}), PickedUp: {format_bool(obj.pickedUp)}\n";
        }

        fakeObjectInfo.text = t;
        
        t = ""; // real Objects
        for (int x = 0; x < syncManager.syncedRealObjects.Length; x++)
        {
            var obj = syncManager.syncedRealObjects[x];
            t += $"ObjectId: {obj.ObjectId}, FakeSyncId: {obj.FakeSyncId}, PickedUp: {format_bool(obj.PickedUp)}\n";
        }

        objectInfo.text = t;
    }
}
