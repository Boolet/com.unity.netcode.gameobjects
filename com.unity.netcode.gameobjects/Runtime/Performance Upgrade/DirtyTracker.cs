using System.Collections.Generic;

namespace Unity.Netcode {

    public static class DirtyTracker {

        public static HashSet<NetworkObject> DirtyObjects;

        static DirtyTracker() {
            DirtyObjects = new HashSet<NetworkObject>();
        }

        public static void MarkDirty(NetworkObject networkObject, bool dirty = true) {
            if (networkObject == null) return;
            if (dirty) {
                DirtyObjects.Add(networkObject);
            } else {
                DirtyObjects.Remove(networkObject);
            }
        }

        public static void MarkDirty(NetworkBehaviour networkBehaviour, bool dirty = true) {
            if (networkBehaviour == null) return;
            MarkDirty(networkBehaviour.NetworkObject, dirty);
        }

        public static void Remove(NetworkObject no) {
            DirtyObjects.Remove(no);
        }

        public static void Clear() {
            DirtyObjects.Clear();
        }

    }

}
