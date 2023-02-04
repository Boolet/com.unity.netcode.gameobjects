using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// NetworkRigidbody allows for the use of <see cref="Rigidbody"/> on network objects. By controlling the kinematic
    /// mode of the rigidbody and disabling it on all peers but the authoritative one.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    //[RequireComponent(typeof(NetworkTransform))]
    public class NetworkRigidbody : NetworkTransform
    {
        public bool RunOnFixedUpdate = true;

        private Rigidbody m_Rigidbody;

        private bool m_OriginalKinematic;
        private RigidbodyInterpolation m_OriginalInterpolation;

        // Used to cache the authority state of this rigidbody during the last frame
        private bool m_IsAuthority;

        /// <summary>
        /// Gets a bool value indicating whether this <see cref="NetworkRigidbody"/> on this peer currently holds authority.
        /// </summary>
        private bool HasAuthority => CanCommitToTransform;

        protected override bool m_AutoUpdateTransform => !RunOnFixedUpdate;

        private new void Awake()
        {
            base.Awake();
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!IsSpawned) {
                return;
            }

            if (NetworkManager.IsListening) {
                if (HasAuthority != m_IsAuthority) {
                    m_IsAuthority = HasAuthority;
                    UpdateRigidbodyKinematicMode();
                }
            }

            // apply interpolated value
            if (!m_AutoUpdateTransform && (m_CachedNetworkManager.IsConnectedClient || m_CachedNetworkManager.IsListening)) {
                // eventually, we could hoist this calculation so that it happens once for all objects, not once per object
                //var cachedDeltaTime = Time.deltaTime;
                //var serverTime = NetworkManager.ServerTime;
                //var cachedServerTime = serverTime.Time;
                //var cachedRenderTime = serverTime.TimeTicksAgo(1).Time;

                //if (Interpolate) {
                //    foreach (var interpolator in m_AllFloatInterpolators) {
                //        interpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                //    }

                //    m_RotationInterpolator.Update(cachedDeltaTime, cachedRenderTime, cachedServerTime);
                //}

                if (!CanCommitToTransform) {
                    // Apply updated interpolated value
                    ApplyToTransform();
                }
            }
        }

        // Puts the rigidbody in a kinematic non-interpolated mode on everyone but the server.
        private void UpdateRigidbodyKinematicMode()
        {
            if (m_IsAuthority == false)
            {
                m_OriginalKinematic = m_Rigidbody.isKinematic;
                m_Rigidbody.isKinematic = true;

                m_OriginalInterpolation = m_Rigidbody.interpolation;
                // Set interpolation to none, the NetworkTransform component interpolates the position of the object.
                m_Rigidbody.interpolation = RigidbodyInterpolation.None;
            }
            else
            {
                // Resets the rigidbody back to it's non replication only state. Happens on shutdown and when authority is lost
                //Debug.Log("m_OriginalKinematic value: " + m_OriginalKinematic);
                m_Rigidbody.isKinematic = m_OriginalKinematic;
                m_Rigidbody.interpolation = m_OriginalInterpolation;
            }
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_IsAuthority = HasAuthority;
            m_OriginalKinematic = m_Rigidbody.isKinematic;
            m_OriginalInterpolation = m_Rigidbody.interpolation;
            UpdateRigidbodyKinematicMode();
        }

        /// <inheritdoc />
        new public void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            UpdateRigidbodyKinematicMode();
        }
    }
}
