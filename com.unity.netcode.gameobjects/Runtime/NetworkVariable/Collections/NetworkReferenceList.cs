//using System;
//using System.Collections.Generic;
//using Unity.Collections;

//namespace Unity.Netcode
//{
//    public class NetworkReferenceList<T> : NetworkVariableBase where T : NetworkBehaviour {

//        private Dictionary<NetworkBehaviourReference, int> m_AwaitedIndices = new Dictionary<NetworkBehaviourReference, int>();
//        private List<T> m_List = new List<T>(64);
//        private List<NetworkListEvent<T>> m_DirtyEvents = new List<NetworkListEvent<T>>(64);

//        /// <summary>
//        /// Delegate type for list changed event
//        /// </summary>
//        /// <param name="changeEvent">Struct containing information about the change event</param>
//        public delegate void OnListChangedDelegate(NetworkListEvent<T> changeEvent);

//        /// <summary>
//        /// The callback to be invoked when the list gets changed
//        /// </summary>
//        public event OnListChangedDelegate OnListChanged;

//        /// <summary>
//        /// Creates a NetworkList with the default value and settings
//        /// </summary>
//        public NetworkReferenceList() { }

//        /// <summary>
//        /// Creates a NetworkList with the default value and custom settings
//        /// </summary>
//        /// <param name="readPerm">The read permission to use for the NetworkList</param>
//        /// <param name="values">The initial value to use for the NetworkList</param>
//        public NetworkReferenceList(NetworkVariableReadPermission readPerm, IEnumerable<T> values) : base(readPerm) {
//            foreach (var value in values) {
//                m_List.Add(value);
//            }
//        }

//        /// <summary>
//        /// Creates a NetworkList with a custom value and the default settings
//        /// </summary>
//        /// <param name="values">The initial value to use for the NetworkList</param>
//        public NetworkReferenceList(IEnumerable<T> values) {
//            foreach (var value in values) {
//                m_List.Add(value);

//            }
//        }

//        /// <inheritdoc />
//        public override void ResetDirty() {
//            base.ResetDirty();
//            m_DirtyEvents.Clear();
//        }

//        /// <inheritdoc />
//        public override bool IsDirty() {
//            // we call the base class to allow the SetDirty() mechanism to work
//            return base.IsDirty() || m_DirtyEvents.Count > 0;
//        }

//        /// <inheritdoc />
//        public override void WriteDelta(FastBufferWriter writer) {

//            if (base.IsDirty()) {
//                writer.WriteValueSafe((ushort)1);
//                writer.WriteValueSafe(NetworkListEvent<T>.EventType.Full);
//                WriteField(writer);

//                return;
//            }

//            writer.WriteValueSafe((ushort)m_DirtyEvents.Count);
//            for (int i = 0; i < m_DirtyEvents.Count; i++) {
//                writer.WriteValueSafe(m_DirtyEvents[i].Type);
//                switch (m_DirtyEvents[i].Type) {
//                    case NetworkListEvent<T>.EventType.Add: {
//                            NetworkReference<T>.Write(writer, m_DirtyEvents[i].Value);
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.Insert: {
//                            writer.WriteValueSafe(m_DirtyEvents[i].Index);
//                            NetworkReference<T>.Write(writer, m_DirtyEvents[i].Value);
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.Remove: {
//                            NetworkReference<T>.Write(writer, m_DirtyEvents[i].Value);
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.RemoveAt: {
//                            writer.WriteValueSafe(m_DirtyEvents[i].Index);
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.Value: {
//                            writer.WriteValueSafe(m_DirtyEvents[i].Index);
//                            NetworkReference<T>.Write(writer, m_DirtyEvents[i].Value);
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.Clear: {
//                            //Nothing has to be written
//                        }
//                        break;
//                }
//            }
//        }

//        /// <inheritdoc />
//        public override void WriteField(FastBufferWriter writer) {
//            writer.WriteValueSafe((ushort)m_List.Count);
//            for (int i = 0; i < m_List.Count; i++) {
//                NetworkReference<T>.Write(writer, m_List[i]);
//            }
//        }

//        /// <inheritdoc />
//        public override void ReadField(FastBufferReader reader) {
//            m_List.Clear();
//            reader.ReadValueSafe(out ushort count);
//            for (int i = 0; i < count; i++) {
//                NetworkReference<T>.Read(reader, out NetworkBehaviourReference value);
//                if (value.TryGet(out T behaviour)) {
//                    m_List.Add(behaviour);
//                } else {
//                    m_AwaitedIndices.Add(value, i);
//                    AwaitSpawnHelper.AwaitSpawn(value, OnAwaitedComponentAppeared);
//                }
//            }
//        }

//        private void OnAwaitedComponentAppeared(NetworkBehaviour awaitedReference) {
//            var reference = new NetworkBehaviourReference(awaitedReference);
//            int finalIndex;
//            int currentIndex = finalIndex = m_AwaitedIndices[reference];
//            m_AwaitedIndices.Remove(reference);
//            foreach (var kv in m_AwaitedIndices) {
//                if (kv.Value < finalIndex) {
//                    --currentIndex;
//                }
//            }
//            m_List.Insert(currentIndex, (T)awaitedReference);
//        }

//        /// <inheritdoc />
//        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta) {
//            reader.ReadValueSafe(out ushort deltaCount);
//            for (int i = 0; i < deltaCount; i++) {
//                reader.ReadValueSafe(out NetworkListEvent<T>.EventType eventType);
//                switch (eventType) {
//                    case NetworkListEvent<T>.EventType.Add: {
//                            NetworkReference<T>.Read(reader, out NetworkBehaviourReference reference);
//                            if (reference.TryGet(out T value)) {
//                                m_List.Add(value);

//                                if (OnListChanged != null) {
//                                    OnListChanged(new NetworkListEvent<T> {
//                                        Type = eventType,
//                                        Index = m_List.Count - 1,
//                                        Value = m_List[m_List.Count - 1]
//                                    });
//                                }

//                                if (keepDirtyDelta) {
//                                    m_DirtyEvents.Add(new NetworkListEvent<T>() {
//                                        Type = eventType,
//                                        Index = m_List.Count - 1,
//                                        Value = m_List[m_List.Count - 1]
//                                    });
//                                }
//                            } else {
//                                m_AwaitedIndices.Add(value, m_List.Count + m_AwaitedIndices.Count);
//                                AwaitSpawnHelper.AwaitSpawn(value, OnAwaitedComponentAppeared);
//                            }
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.Insert: {
//                            reader.ReadValueSafe(out int index);
//                            NetworkReference<T>.Read(reader, out NetworkBehaviourReference value);
//                            if (value.TryGet(out NetworkBehaviour networkBehaviour)) {
//                                m_List.Insert(index, (T)value);

//                                if (OnListChanged != null) {
//                                    OnListChanged(new NetworkListEvent<T> {
//                                        Type = eventType,
//                                        Index = index,
//                                        Value = m_List[index]
//                                    });
//                                }

//                                if (keepDirtyDelta) {
//                                    m_DirtyEvents.Add(new NetworkListEvent<T>() {
//                                        Type = eventType,
//                                        Index = index,
//                                        Value = m_List[index]
//                                    });
//                                }
//                            } else {

//                            }
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.Remove: {
//                            NetworkReference<T>.Read(reader, out NetworkBehaviourReference reference);
//                            if (reference.TryGet(out T value)) {
//                                int index = m_List.IndexOf(value);
//                                if (index == -1) {
//                                    break;
//                                }

//                                m_List.RemoveAt(index);

//                                if (OnListChanged != null) {
//                                    OnListChanged(new NetworkListEvent<T> {
//                                        Type = eventType,
//                                        Index = index,
//                                        Value = value
//                                    });
//                                }

//                                if (keepDirtyDelta) {
//                                    m_DirtyEvents.Add(new NetworkListEvent<T>() {
//                                        Type = eventType,
//                                        Index = index,
//                                        Value = value
//                                    });
//                                }
//                            } else {
//                                m_AwaitedIndices.Remove(value);
//                            }
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.RemoveAt: {
//                            reader.ReadValueSafe(out int index);
//                            T value = m_List[index];
//                            m_List.RemoveAt(index);

//                            if (OnListChanged != null) {
//                                OnListChanged(new NetworkListEvent<T> {
//                                    Type = eventType,
//                                    Index = index,
//                                    Value = value
//                                });
//                            }

//                            if (keepDirtyDelta) {
//                                m_DirtyEvents.Add(new NetworkListEvent<T>() {
//                                    Type = eventType,
//                                    Index = index,
//                                    Value = value
//                                });
//                            }
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.Value: {
//                            reader.ReadValueSafe(out int index);
//                            NetworkReference<T>.Read(reader, out T value);
//                            if (index >= m_List.Count) {
//                                throw new Exception("Shouldn't be here, index is higher than list length");
//                            }

//                            var previousValue = m_List[index];
//                            m_List[index] = value;

//                            if (OnListChanged != null) {
//                                OnListChanged(new NetworkListEvent<T> {
//                                    Type = eventType,
//                                    Index = index,
//                                    Value = value,
//                                    PreviousValue = previousValue
//                                });
//                            }

//                            if (keepDirtyDelta) {
//                                m_DirtyEvents.Add(new NetworkListEvent<T>() {
//                                    Type = eventType,
//                                    Index = index,
//                                    Value = value,
//                                    PreviousValue = previousValue
//                                });
//                            }
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.Clear: {
//                            //Read nothing
//                            m_List.Clear();

//                            if (OnListChanged != null) {
//                                OnListChanged(new NetworkListEvent<T> {
//                                    Type = eventType,
//                                });
//                            }

//                            if (keepDirtyDelta) {
//                                m_DirtyEvents.Add(new NetworkListEvent<T>() {
//                                    Type = eventType
//                                });
//                            }
//                        }
//                        break;
//                    case NetworkListEvent<T>.EventType.Full: {
//                            ReadField(reader);
//                            ResetDirty();
//                        }
//                        break;
//                }
//            }
//        }
//    }
//}
