
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class GroupZoneTrigger : UdonSharpBehaviour
{
    public USPPNetEveryPlayerManager playerNetworkManager;
    public string zoneName = "ChangeMe";
    
    void Start()
    {
        if (playerNetworkManager == null)
            Debug.LogError($"playerNetworkManager is not set on: {gameObject.name}");
        
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer && player.IsValid())
            return;
        
        Debug.Log($"Entered: {zoneName}!");
        
        playerNetworkManager.local_object.request_new_group(zoneName, player.playerId);
    }
    
    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer)
            return;
        
        //playerNetworkManager.local_object.request_new_group("", player.playerId);

    }
}
