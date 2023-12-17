using System;

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace GroupSync
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class USPPNetEveryPlayerManager : UdonSharpBehaviour
    {
        public GroupManager groupManager;
    
        [NonSerialized] public USPPNetEveryPlayer local_object;
        [NonSerialized] public USPPNetEveryPlayer[] Objects;

        public void Bootstrap()
        {
            Objects = new USPPNetEveryPlayer[transform.childCount];
            var i = 0;
            foreach (Transform child in transform)
            {
                Objects[i] = child.GetComponent<USPPNetEveryPlayer>();
                i++;
            }
            
            SendCustomEventDelayedSeconds(nameof(ErrorLocalObject), 10);
        }

        private USPPNetEveryPlayer GetPlayerObject()
        {
            foreach (var playerObject in Objects)
            {
                if (playerObject.owned)
                    continue;
                
                if (Networking.IsOwner(playerObject.gameObject))
                    return playerObject;
                
                Debug.LogError("Object isn't owned, but has wrong owner?");
            }

            return null;
        }

        public void ErrorLocalObject()
        {
            if (Utilities.IsValid(local_object))
                return;
            
            Debug.LogError("This is really bad! report this to me, please! send me your log! discord: deltaneverused");
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            var objectToAssign = GetPlayerObject();
            if (Utilities.IsValid(objectToAssign))
                objectToAssign.SetOwner(player);
            else
                Debug.LogError("Got Invalid object somehow? ran out?");
        }
    }
}
