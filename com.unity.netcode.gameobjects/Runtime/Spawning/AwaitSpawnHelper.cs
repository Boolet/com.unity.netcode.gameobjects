using System.Collections.Generic;
using Unity.Netcode;

public static class AwaitSpawnHelper {

    public delegate void AwaitedSpawnComponentDelegate(NetworkBehaviour networkObject);
    public delegate void AwaitedSpawnObjectDelegate(NetworkObject networkObject);

    private static readonly Dictionary<ulong, Dictionary<ushort, HashSet<AwaitedSpawnComponentDelegate>>> k_AwaitComponentCallbacks
        = new Dictionary<ulong, Dictionary<ushort, HashSet<AwaitedSpawnComponentDelegate>>>();
    private static readonly Dictionary<ulong, HashSet<AwaitedSpawnObjectDelegate>> k_AwaitObjectCallbacks
        = new Dictionary<ulong, HashSet<AwaitedSpawnObjectDelegate>>();

    static AwaitSpawnHelper() {
        k_AwaitComponentCallbacks.Clear();
        k_AwaitObjectCallbacks.Clear();
    }

    #region Subscribe

    /// <summary>
    /// When the object referred to by UnresolvedReference is spawned on this
    /// client, the method Callback will be invoked. A callback delegate can
    /// only await one object at a time; further calls to AwaitSpawn for that
    /// delegate will supercede the previous.
    /// </summary>
    /// <param name="unresolvedReference"></param>
    /// <param name="callback"></param>
    public static void AwaitSpawn(NetworkObjectReference unresolvedReference, AwaitedSpawnObjectDelegate callback) {
        AwaitSpawn(unresolvedReference.NetworkObjectId, callback);
    }

    /// <summary>
    /// When the behavior referred to by UnresolvedReference is spawned on this
    /// client, the method Callback will be invoked. A callback delegate can
    /// only await one behavior at a time; further calls to AwaitSpawn for that
    /// delegate will supercede the previous.
    /// </summary>
    /// <param name="unresolvedReference"></param>
    /// <param name="callback"></param>
    public static void AwaitSpawn(NetworkBehaviourReference unresolvedReference, AwaitedSpawnComponentDelegate callback) {
        AwaitSpawn(unresolvedReference.NetworkObjectId, unresolvedReference.NetworkBehaviorId, callback);
    }

    /// <summary>
    /// When the component referred to by AwaitedNetId and AwaitedComponentIndex
    /// is spawned on this client, the method Callback will be invoked.
    /// A callback delegate can only await one object at a time; further calls
    /// to AwaitSpawn for that delegate will supercede the previous.
    /// </summary>
    /// <param name="unresolvedReference"></param>
    /// <param name="callback"></param>
    public static void AwaitSpawn(ulong awaitedNetId, ushort awaitedComponentIndex, AwaitedSpawnComponentDelegate callback) {
        AddCallback(awaitedNetId, awaitedComponentIndex, callback);
    }

    /// <summary>
    /// When the object referred to by AwaitedNetId is spawned on this
    /// client, the method Callback will be invoked. A callback delegate can
    /// only await one object at a time; further calls to AwaitSpawn for that
    /// delegate will supercede the previous.
    /// </summary>
    /// <param name="unresolvedReference"></param>
    /// <param name="callback"></param>
    public static void AwaitSpawn(ulong awaitedNetId, AwaitedSpawnObjectDelegate callback) {
        AddCallback(awaitedNetId, callback);
    }

    #endregion

    #region Unsubscribe

    public static void Unsubscribe(NetworkBehaviourReference unresolvedReference, AwaitedSpawnComponentDelegate callback) {
        RemoveCallback(unresolvedReference.NetworkObjectId, unresolvedReference.NetworkBehaviorId, callback);
    }

    public static void Unsubscribe(NetworkObjectReference unresolvedReference, AwaitedSpawnObjectDelegate callback) {
        RemoveCallback(unresolvedReference.NetworkObjectId, callback);
    }

    #endregion

    #region Resolution

    public static void ObjectSpawned(NetworkObject spawnedObject) {
        ulong objectId = spawnedObject.NetworkObjectId;
        if (k_AwaitObjectCallbacks.TryGetValue(spawnedObject.NetworkObjectId, out HashSet<AwaitedSpawnObjectDelegate> objectCallbacks)) {
            foreach (var callback in objectCallbacks) {
                callback.Invoke(spawnedObject);
            }
            k_AwaitObjectCallbacks.Remove(objectId);
        }

        if (k_AwaitComponentCallbacks.TryGetValue(spawnedObject.NetworkObjectId, out Dictionary<ushort, HashSet<AwaitedSpawnComponentDelegate>> componentCallbacks)) {
            foreach(var kv in componentCallbacks){
                foreach (var callback in kv.Value) {
                    callback.Invoke(spawnedObject.GetNetworkBehaviourAtOrderIndex(kv.Key));
                }
            }
            k_AwaitComponentCallbacks.Remove(objectId);
        }
    }

    #endregion

    #region Helpers

    private static void AddCallback(ulong awaitedNetId, ushort awaitedComponentIndex, AwaitedSpawnComponentDelegate callback) {
        if (!k_AwaitComponentCallbacks.TryGetValue(awaitedNetId, out Dictionary<ushort, HashSet<AwaitedSpawnComponentDelegate>> objectEntry)){
            k_AwaitComponentCallbacks.Add(awaitedNetId, objectEntry = new Dictionary<ushort, HashSet<AwaitedSpawnComponentDelegate>>());
        }
        if (!objectEntry.TryGetValue(awaitedComponentIndex, out HashSet<AwaitedSpawnComponentDelegate> componentCallbacks)) {
            objectEntry.Add(awaitedComponentIndex, componentCallbacks = new HashSet<AwaitedSpawnComponentDelegate>());
        }
        componentCallbacks.Add(callback);
    }

    private static void AddCallback(ulong awaitedNetId, AwaitedSpawnObjectDelegate callback) {
        if (!k_AwaitObjectCallbacks.ContainsKey(awaitedNetId)) {
            k_AwaitObjectCallbacks.Add(awaitedNetId, new HashSet<AwaitedSpawnObjectDelegate>());
        }
        k_AwaitObjectCallbacks[awaitedNetId].Add(callback);
    }

    private static void RemoveCallback(ulong awaitedNetId, ushort awaitedComponentIndex, AwaitedSpawnComponentDelegate callback) {
        if (k_AwaitComponentCallbacks.TryGetValue(awaitedNetId, out Dictionary<ushort, HashSet<AwaitedSpawnComponentDelegate>> objectEntry)) {
            if (objectEntry.TryGetValue(awaitedComponentIndex, out HashSet<AwaitedSpawnComponentDelegate> componentCallbacks)) {
                componentCallbacks.Remove(callback);
                if (componentCallbacks.Count == 0) {
                    objectEntry.Remove(awaitedComponentIndex);
                }
            }
            if (objectEntry.Count == 0) {
                k_AwaitComponentCallbacks.Remove(awaitedNetId);
            }
        }
    }

    private static void RemoveCallback(ulong awaitedNetId, AwaitedSpawnObjectDelegate callback) {
        if (k_AwaitObjectCallbacks.TryGetValue(awaitedNetId, out HashSet<AwaitedSpawnObjectDelegate> objectEntry)) {
            objectEntry.Remove(callback);
            if (objectEntry.Count == 0) {
                k_AwaitObjectCallbacks.Remove(awaitedNetId);
            }
        }
    }

    //private static void RemoveCallbacksForComponent(ulong awaitedNetId, ushort awaitedComponentIndex) {
    //    if (k_AwaitComponentCallbacks.TryGetValue(awaitedNetId, out Dictionary<ushort, HashSet<AwaitedSpawnComponentDelegate>> objectEntry)) {
    //        if (objectEntry.Remove(awaitedComponentIndex) && objectEntry.Count == 0) {
    //            k_AwaitComponentCallbacks.Remove(awaitedNetId);
    //        }
    //    }
    //}

    //private static void RemoveCallbacksForObject(ulong awaitedNetId) {
    //    k_AwaitComponentCallbacks.Remove(awaitedNetId);
    //    k_AwaitObjectCallbacks.Remove(awaitedNetId);
    //}

    #endregion
}
