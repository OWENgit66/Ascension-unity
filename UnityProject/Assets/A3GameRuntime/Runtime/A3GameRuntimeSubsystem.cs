using System;
using System.Collections.Generic;
using UnityEngine;

namespace A3GameRuntime
{
    /// <summary>
    /// MonoBehaviour singleton that acts as the top-level runtime coordinator
    /// (analogous to a UE5 GameInstance subsystem). It registers entity
    /// factories, coordinates entity creation/destruction, and dispatches
    /// extension messages to registered handlers.
    /// The Unity equivalent of UE5's <c>UA3GameRuntimeSubsystem</c>.
    /// </summary>
    [AddComponentMenu("A3Game/Runtime Subsystem")]
    [DefaultExecutionOrder(-100)]
    public class A3GameRuntimeSubsystem : MonoBehaviour
    {
        /// <summary>Global singleton accessor.</summary>
        public static A3GameRuntimeSubsystem Instance { get; private set; }

        /// <summary>The world identifier managed by this runtime instance.</summary>
        [Tooltip("World identifier for this runtime instance.")]
        public string worldId = "world_001";

        /// <summary>Whether to auto-create a session subsystem if none exists.</summary>
        [Tooltip("If true, a World Session Subsystem is created automatically on Awake.")]
        public bool autoCreateSession = true;

        /// <summary>Whether to auto-create an input receiver if none exists.</summary>
        [Tooltip("If true, a Runtime Input Receiver is created automatically on Awake.")]
        public bool autoCreateInputReceiver = true;

        private readonly Dictionary<string, IA3GameEntityFactory> _factories =
            new Dictionary<string, IA3GameEntityFactory>();

        private readonly Dictionary<string, IA3GameRuntimeMessageHandler> _messageHandlers =
            new Dictionary<string, IA3GameRuntimeMessageHandler>();

        private readonly Dictionary<string, GameObject> _entities =
            new Dictionary<string, GameObject>();

        private A3GameWorldSessionSubsystem _session;
        private A3GameRuntimeInputReceiver _inputReceiver;

        /// <summary>Cached session subsystem reference (found or auto-created).</summary>
        public A3GameWorldSessionSubsystem Session
        {
            get
            {
                if (_session != null) return _session;
                _session = FindObjectOfType<A3GameWorldSessionSubsystem>();
                if (_session == null && autoCreateSession)
                {
                    var go = new GameObject("A3Game_WorldSessionSubsystem");
                    _session = go.AddComponent<A3GameWorldSessionSubsystem>();
                }
                return _session;
            }
        }

        /// <summary>Cached input receiver reference (found or auto-created).</summary>
        public A3GameRuntimeInputReceiver InputReceiver
        {
            get
            {
                if (_inputReceiver != null) return _inputReceiver;
                _inputReceiver = FindObjectOfType<A3GameRuntimeInputReceiver>();
                if (_inputReceiver == null && autoCreateInputReceiver)
                {
                    var go = new GameObject("A3Game_RuntimeInputReceiver");
                    _inputReceiver = go.AddComponent<A3GameRuntimeInputReceiver>();
                }
                return _inputReceiver;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[A3GameRuntime] Duplicate A3GameRuntimeSubsystem detected; destroying.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            // Touch the Session and InputReceiver properties so they are
            // created early in the lifecycle if auto-creation is enabled.
            _ = Session;
            _ = InputReceiver;
        }

        /// <summary>
        /// Initialize the runtime with a world identifier. Sets the worldId
        /// and propagates it to the session subsystem.
        /// </summary>
        public void Initialize(string worldId)
        {
            if (!string.IsNullOrEmpty(worldId))
                this.worldId = worldId;

            if (Session != null)
                Session.Initialize(this.worldId);

            Debug.Log("[A3GameRuntime] Initialized worldId=" + this.worldId);
        }

        // ── Factory registration ────────────────────────────────────────────

        /// <summary>
        /// Register an entity factory. The factory's type full name is used as
        /// the dictionary key; registering again with the same type replaces
        /// the previous registration.
        /// </summary>
        public void RegisterFactory(IA3GameEntityFactory factory)
        {
            if (factory == null) return;
            string key = factory.GetType().FullName;
            _factories[key] = factory;
            Debug.Log("[A3GameRuntime] Registered factory: " + key);
        }

        /// <summary>
        /// Unregister a previously registered entity factory.
        /// Returns <c>true</c> if the factory was found and removed.
        /// </summary>
        public bool UnregisterFactory(IA3GameEntityFactory factory)
        {
            if (factory == null) return false;
            string key = factory.GetType().FullName;
            bool removed = _factories.Remove(key);
            if (removed)
                Debug.Log("[A3GameRuntime] Unregistered factory: " + key);
            return removed;
        }

        /// <summary>Number of currently registered factories.</summary>
        public int FactoryCount => _factories.Count;

        // ── Message handler registration ────────────────────────────────────

        /// <summary>
        /// Register a message handler for extension messages.
        /// </summary>
        public void RegisterMessageHandler(IA3GameRuntimeMessageHandler handler)
        {
            if (handler == null) return;
            string key = handler.GetType().FullName;
            _messageHandlers[key] = handler;
        }

        /// <summary>
        /// Unregister a previously registered message handler.
        /// </summary>
        public bool UnregisterMessageHandler(IA3GameRuntimeMessageHandler handler)
        {
            if (handler == null) return false;
            string key = handler.GetType().FullName;
            return _messageHandlers.Remove(key);
        }

        /// <summary>
        /// Dispatch an extension message to all registered handlers.
        /// Returns <c>true</c> if at least one handler reported it handled
        /// the message.
        /// </summary>
        public bool DispatchExtensionMessage(string type, string jsonPayload)
        {
            if (string.IsNullOrEmpty(type)) return false;

            bool handled = false;
            foreach (var handler in _messageHandlers.Values)
            {
                try
                {
                    if (handler.HandleMessage(type, jsonPayload))
                        handled = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        "[A3GameRuntime] Message handler threw: " + e.Message);
                }
            }
            return handled;
        }

        // ── Entity lifecycle ───────────────────────────────────────────────

        /// <summary>
        /// Spawn a controllable entity from the given request. Tries each
        /// registered factory in order; if none succeeds, creates a basic
        /// GameObject with identity and entity components.
        /// </summary>
        public GameObject SpawnEntity(A3GameEntitySpawnRequest request)
        {
            if (string.IsNullOrEmpty(request.entity_id))
                request.entity_id = NewId("ent");

            // If the entity already exists, return the existing GameObject.
            if (_entities.TryGetValue(request.entity_id, out var existing) && existing != null)
                return existing;

            GameObject obj = null;

            // Try registered factories.
            foreach (var factory in _factories.Values)
            {
                try
                {
                    obj = factory.CreateEntity(request);
                    if (obj != null) break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        "[A3GameRuntime] Factory CreateEntity failed: " + e.Message);
                }
            }

            // Fallback: create a basic GameObject.
            if (obj == null)
            {
                obj = LoadRuntimePrefab(request.avatar_asset_path);
                if (obj != null)
                    obj.name = string.IsNullOrEmpty(request.actor_label)
                        ? obj.name
                        : request.actor_label;
            }

            // Last resort: keep the generic session alive even when no
            // runtime prefab was provided. Generated gameplay normally
            // registers a typed factory, while this fallback makes the
            // failure explicit in the snapshot instead of crashing the run.
            if (obj == null)
            {
                string name = string.IsNullOrEmpty(request.actor_label)
                    ? "A3Game_Entity_" + request.entity_id
                    : request.actor_label;
                obj = new GameObject(name);
            }

            // Apply spawn transform.
            obj.transform.position = request.spawn_transform.position;
            obj.transform.rotation = Quaternion.Euler(request.spawn_transform.rotation);

            // Ensure identity component exists.
            var identity = obj.GetComponent<A3GameIdentityComponent>();
            if (identity == null)
                identity = obj.AddComponent<A3GameIdentityComponent>();
            identity.Initialize(request.entity_id, worldId);

            // Ensure entity component exists and is initialized.
            var entityComponent = obj.GetComponent<A3GameRuntimeEntityComponent>();
            if (entityComponent == null)
                entityComponent = obj.AddComponent<A3GameRuntimeEntityComponent>();
            entityComponent.Initialize(
                request.entity_id,
                worldId,
                request.avatar_asset_path,
                request.idle_animation_path,
                request.move_animation_path,
                request.actor_label,
                request.parameters,
                request.persistent,
                request.spawn_transform.position,
                request.spawn_transform.rotation);

            _entities[request.entity_id] = obj;
            return obj;
        }

        private static GameObject LoadRuntimePrefab(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            string normalized = assetPath.Replace('\\', '/');
            const string prefix = "Assets/Resources/";
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(prefix.Length);
            else
                return null;
            int extension = normalized.LastIndexOf('.');
            if (extension > 0)
                normalized = normalized.Substring(0, extension);
            var prefab = Resources.Load<GameObject>(normalized);
            return prefab != null ? Instantiate(prefab) : null;
        }

        /// <summary>
        /// Destroy an entity by its identifier. Tries registered factories
        /// first; falls back to destroying the tracked GameObject.
        /// Returns <c>true</c> if the entity was found and removed.
        /// </summary>
        public bool DestroyEntity(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return false;

            // Try registered factories.
            bool factoryHandled = false;
            foreach (var factory in _factories.Values)
            {
                try
                {
                    if (factory.DestroyEntity(entityId))
                    {
                        factoryHandled = true;
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        "[A3GameRuntime] Factory DestroyEntity failed: " + e.Message);
                }
            }

            // Remove and destroy the tracked GameObject.
            if (_entities.TryGetValue(entityId, out var obj))
            {
                _entities.Remove(entityId);
                if (!factoryHandled && obj != null)
                    Destroy(obj);
                return true;
            }

            return factoryHandled;
        }

        /// <summary>Look up the GameObject for an entity, or null.</summary>
        public GameObject GetEntityObject(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            _entities.TryGetValue(entityId, out var obj);
            return obj;
        }

        /// <summary>Number of tracked entities.</summary>
        public int EntityCount => _entities.Count;

        /// <summary>
        /// Return all tracked entity IDs as a new list.
        /// </summary>
        public List<string> GetEntityIds()
        {
            return new List<string>(_entities.Keys);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>Generate a short runtime identifier with a prefix.</summary>
        public static string NewId(string prefix)
        {
            return prefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        }

        /// <summary>Current Unix epoch time in seconds.</summary>
        public static double EpochSeconds =>
            (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }
}
