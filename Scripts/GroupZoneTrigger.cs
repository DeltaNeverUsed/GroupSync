
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

    private void Start()
    {
        if (playerNetworkManager != null) return;
        playerNetworkManager = GameObject.Find("EachPlayerUSPPNet").GetComponent<USPPNetEveryPlayerManager>();

        if (playerNetworkManager != null) return;
        Debug.LogError($"playerNetworkManager is not set on: {gameObject.name}");
        gameObject.SetActive(false);
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer)
            return;
        
        Debug.Log($"Entered: {zoneName}!");
        
        playerNetworkManager.local_object.request_new_group(zoneName, player.playerId);
    }
}
