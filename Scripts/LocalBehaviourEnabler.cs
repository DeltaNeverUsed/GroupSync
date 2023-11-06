using UdonSharp;
using UnityEngine;

[RequireComponent(typeof(GroupObjectSync))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LocalBehaviourEnabler : UdonSharpBehaviour
{
    public UdonSharpBehaviour[] excluded = { };

    private UdonSharpBehaviour[] _compList = { };
    
    private static bool Contains<T>(T[] haystack, T needle)
    {
        foreach (var hay in haystack)
            if (hay.Equals(needle))
                return true;
        return false;
    }
    private static T[] Add<T>(T[] array, T item)
    {
        var len = array.Length + 1;
        var tempArray = new T[len];
        array.CopyTo(tempArray, 0);
        tempArray[len-1] = item;
        return tempArray;
    }

    private void Start()
    {
        excluded = Add(excluded, GetComponent<GroupObjectSync>());
        excluded = Add(excluded, this);
        var objectComps = GetComponents<UdonSharpBehaviour>();
        foreach (var comp in objectComps)
        {
            if (Contains(excluded, comp))
                return;
            _compList = Add(_compList, comp);
        }
    }

    public void EnableComps()
    {
        foreach (var comp in _compList)
            comp.enabled = true;
    }
    public void DisableComps()
    {
        foreach (var comp in _compList)
            comp.enabled = false;
    }
}
