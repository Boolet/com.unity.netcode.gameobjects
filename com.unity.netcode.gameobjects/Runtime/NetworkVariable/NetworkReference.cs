using System;
using UnityEngine;

namespace Unity.Netcode {
    public class NetworkReference<T> : NetworkVariableBase where T : NetworkBehaviour
    {
        // Functions that serialize other types
        internal static void Write(FastBufferWriter writer, in NetworkBehaviourReference value) {
            writer.WriteValueSafe(value);
        }

        internal static void Read(FastBufferReader reader, out NetworkBehaviourReference value) {
            reader.ReadValueSafe(out value);
        }

        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="previousValue">The value before the change</param>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate(T value);
        /// <summary>
        /// The callback to be invoked when the value will be changed by the server
        /// </summary>
        public OnValueChangedDelegate OnValueWillChange;
        /// <summary>
        /// The callback to be invoked when a new value is available
        /// </summary>
        public OnValueChangedDelegate OnValueChangeResolved;

        /// <summary>
        /// Creates a NetworkVariable with the default value and custom read permission
        /// </summary>
        /// <param name="readPerm">The read permission for the NetworkVariable</param>

        public NetworkReference() {
        }

        /// <summary>
        /// Creates a NetworkVariable with the default value and custom read permission
        /// </summary>
        /// <param name="readPerm">The read permission for the NetworkVariable</param>
        public NetworkReference(NetworkVariableReadPermission readPerm) : base(readPerm) {
        }

        /// <summary>
        /// Creates a NetworkVariable with a custom value and custom settings
        /// </summary>
        /// <param name="readPerm">The read permission for the NetworkVariable</param>
        /// <param name="value">The initial value to use for the NetworkVariable</param>
        public NetworkReference(NetworkVariableReadPermission readPerm, T value) : base(readPerm) {
            m_InternalValue = value;
            m_Resolved = m_NetworkBehaviour && m_NetworkBehaviour.NetworkManager.IsServer;
        }

        /// <summary>
        /// Creates a NetworkVariable with a custom value and the default read permission
        /// </summary>
        /// <param name="value">The initial value to use for the NetworkVariable</param>
        public NetworkReference(T value) {
            m_InternalValue = value;
            m_Resolved = m_NetworkBehaviour && m_NetworkBehaviour.NetworkManager.IsServer;
        }

        [SerializeField]
        private protected T m_InternalValue;
        private protected bool m_Resolved;
        private protected bool m_AnnounceOnResolve;

        /// <summary>
        /// The value of the NetworkVariable container
        /// </summary>
        public T Value {
            get {
                if (!m_Resolved) {
                    throw new InvalidOperationException("Value should not be read while unresolved");
                }
                if (m_InternalValue.NetworkObject == m_NetworkBehaviour.NetworkObject
                    && m_InternalValue.NetworkBehaviourId == m_NetworkBehaviour.NetworkBehaviourId) {
                    return null;
                }
                return m_InternalValue;
            }
            set {
                // this could be improved. The Networking Manager is not always initialized here
                //  Good place to decouple network manager from the network variable

                // Also, note this is not really very water-tight, if you are running as a host
                //  we cannot tell if a NetworkVariable write is happening inside client-ish code
                if (m_NetworkBehaviour && (m_NetworkBehaviour.NetworkManager.IsClient && !m_NetworkBehaviour.NetworkManager.IsHost)) {
                    throw new InvalidOperationException("Client can't write to NetworkVariables");
                }
                Set(value);
            }
        }

        /// <summary>
        /// Whether the current value of the reference is valid
        /// </summary>
        public bool IsResolved => m_Resolved;

        private protected void Set(T value) {
            p_IsDirty = true;
            T previousValue = m_InternalValue;
            OnValueWillChange?.Invoke(previousValue);
            m_InternalValue = value;
            OnValueChangeResolved?.Invoke(m_InternalValue);
        }

        /// <summary>
        /// Writes the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the value to</param>
        public override void WriteDelta(FastBufferWriter writer) {
            WriteField(writer);
        }


        /// <summary>
        /// Reads value from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the value from</param>
        /// <param name="keepDirtyDelta">Whether or not the container should keep the dirty delta, or mark the delta as consumed</param>
        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta) {
            T previousValue = m_InternalValue;
            OnValueWillChange?.Invoke(previousValue);
            ResolveReference(reader, true);

            if (keepDirtyDelta) {
                p_IsDirty = true;
            }
        }

        /// <inheritdoc />
        public override void ReadField(FastBufferReader reader) {
            ResolveReference(reader, false);
        }

        /// <inheritdoc />
        public override void WriteField(FastBufferWriter writer) {
            Write(writer, m_InternalValue);
        }

        /// <summary>
        /// Attempts to read a NetworkBehaviourReference from the passed reader
        /// and resolve it to an actual component reference, assigning the result
        /// to the internal value.
        ///
        /// If the resolution fails, attempts to wait until the resolution target
        /// spawns and then finishes resolving.
        ///
        /// When the resolution succeeds eventually, the event OnValueChangeResolved
        /// will be invoked if announce is <see langword="true"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="announce"></param>
        private void ResolveReference(FastBufferReader reader, bool announce) {
            Read(reader, out NetworkBehaviourReference reference);
            if (m_Resolved = reference.TryGet(out m_InternalValue)) {
                if (announce) {
                    OnValueChangeResolved?.Invoke(m_InternalValue);
                }
            } else {
                m_AnnounceOnResolve = announce;
                AwaitSpawnHelper.AwaitSpawn(reference, OnReferenceResolved);
            }
        }

        /// <summary>
        /// Callback for reference target spawning
        /// </summary>
        /// <param name="resolution"></param>
        private void OnReferenceResolved(NetworkBehaviour resolution) {
            m_InternalValue = (T)resolution;
            m_Resolved = true;
            if (m_AnnounceOnResolve) {
                OnValueChangeResolved?.Invoke(m_InternalValue);
            }
        }
    }
}
