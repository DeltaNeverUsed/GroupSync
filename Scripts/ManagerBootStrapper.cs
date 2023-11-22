using UdonSharp;
using UnityEngine;

namespace GroupSync
{
    [DefaultExecutionOrder(-9999999)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ManagerBootStrapper : UdonSharpBehaviour
    {
        // https://discord.com/channels/419351657743253524/657814494885707806/1176860941015584788
        // For some reason scripts with BehaviourSyncMode.None will always execute before any synced scripts, so this is a hack to run the start function on the synced manager scripts before the non-synced scripts. 
        private void OnEnable()
        {
            var onSelf = GetComponents<UdonSharpBehaviour>();
            foreach (var behaviour in onSelf)
            {
                if (behaviour == this)
                    continue;
                behaviour.SendCustomEvent("Bootstrap");
            }
            
            var onChildren = GetComponentsInChildren<UdonSharpBehaviour>();
            foreach (var behaviour in onChildren)
                behaviour.SendCustomEvent("Bootstrap");
        }
    }
}
