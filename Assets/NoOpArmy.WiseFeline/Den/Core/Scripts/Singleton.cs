using UnityEngine;

namespace NoOpArmy
{
    /// <summary>
    /// Process-wide state shared by every <see cref="Singleton{T}"/>. It lives outside the generic
    /// class on purpose: <see cref="RuntimeInitializeOnLoadMethod"/> cannot be applied to a method
    /// of an open generic type, and "the application is quitting" is a global fact anyway rather
    /// than something each closed generic type should track for itself.
    /// </summary>
    internal static class SingletonRuntimeState
    {
        /// <summary>
        /// True once the player (or the editor's play session) has started shutting down. Managers
        /// must not be created after that point, because a freshly created one would be torn down
        /// again immediately and its <c>Awake</c> would run against a half-destroyed scene.
        /// </summary>
        internal static bool ApplicationIsQuitting;

        /// <summary>
        /// Re-arms the quit flag every time Play Mode starts. Without this the flag would stay set
        /// from the previous session when Enter Play Mode Options has Domain Reload disabled, and
        /// every <c>Instance</c> lookup would return null for the rest of the editor session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            ApplicationIsQuitting = false;
            // Re-subscribing after an unsubscribe keeps this to a single handler: with Domain
            // Reload disabled the event itself survives between play sessions.
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            ApplicationIsQuitting = true;
        }
    }

    /// <summary>
    /// Base class for the manager components that Wise Feline looks up by type (tags, smart
    /// objects, influence maps, recording). Put one in the scene to configure it, or let the first
    /// <see cref="Instance"/> access create one on demand.
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        /// <summary>
        /// The single instance of this manager, or null while the application is quitting.
        /// Resolves in order: the cached instance, an instance already present in the loaded
        /// scenes, and finally a new <c>DontDestroyOnLoad</c> GameObject created for it.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (SingletonRuntimeState.ApplicationIsQuitting)
                    return null;

                // A destroyed instance compares equal to null, so a stale reference left over from
                // a previous play session resolves itself here; no static reset needed.
                if (_instance != null)
                    return _instance;

                _instance = FindObjectOfType<T>();
                if (_instance != null)
                    return _instance;

                // Only Play Mode may spawn one. Creating a GameObject from a property getter in
                // Edit Mode would dirty whatever scene happens to be open, just because an
                // inspector or editor tool read this property.
                if (!Application.isPlaying)
                    return null;

                var singleton = new GameObject($"(singleton) {typeof(T).Name}");
                _instance = singleton.AddComponent<T>();
                DontDestroyOnLoad(singleton);
                return _instance;
            }
        }

        protected void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                return;
            }

            if (ReferenceEquals(_instance, this))
                return;

            Debug.LogWarning($"Only one instance of the type {typeof(T).Name} should exist! " +
                             $"Destroying the extra one on '{name}'.", this);
            Destroy(gameObject);
        }

        protected void OnDestroy()
        {
            // Clearing the cache rather than latching a quit flag: this fires for every ordinary
            // destroy too, and a latched flag never recovers when Domain Reload is disabled.
            if (ReferenceEquals(_instance, this))
                _instance = null;
        }
    }
}
