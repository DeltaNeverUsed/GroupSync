
using System.Text;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

public class DebugGroups : UdonSharpBehaviour
{

    public GroupManager groupManager;
    public USPPNetEveryPlayerManager playerManager;
    
    public TextMeshProUGUI netinfo;
    public TextMeshProUGUI groupinfo;

    public void FixedUpdate()
    {
        if (playerManager.local_object != null)
            DebugDisplayGroup();
    }

    private void DebugDisplayGroup()
    {
        var bytesSent = (int)playerManager.local_object.GetProgramVariable("bytesSent");
        netinfo.text = $"Local player last sent: {bytesSent} Bytes";
        
        if (groupManager.groups.Length < groupManager.maxGroups * groupManager.maxPlayersPerGroup)
            return;

        var t = ""; // Groups
        for (int x = 0; x < groupManager.maxGroups; x++)
        {
            var groupName = groupManager.joinable.Length == groupManager.maxGroups ? groupManager.joinable[x] : "Not Host";
            var players = "";
            for (int y = 0; y < groupManager.maxPlayersPerGroup; y++)
            {
                if (groupManager.groups[groupManager.GroupAndPlayer2Index(x, y)] == -1) continue;
                var guy = VRCPlayerApi.GetPlayerById(groupManager.groups[groupManager.GroupAndPlayer2Index(x, y)]);
                var displayName = guy != null ? guy.displayName : "N/A";
                players += $"({displayName}, {groupManager.groups[groupManager.GroupAndPlayer2Index(x, y)]})";
            }
            t += $"Group: {x}, name: {groupName}, contains: {players}\n";
        }

        groupinfo.text = t;
    }
}
