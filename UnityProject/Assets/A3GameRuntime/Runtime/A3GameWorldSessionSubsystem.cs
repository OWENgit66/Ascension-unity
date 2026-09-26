using System;
using System.Collections.Generic;
using UnityEngine;

namespace A3GameRuntime
{
    /// <summary>
    /// MonoBehaviour singleton that owns all session-level state: participants,
    /// controllers, control bindings, entity states, and latest inputs.
    /// Provides Join/Leave/Heartbeat/ApplyInput/Snapshot/ResetWorld/ClearEntity
    /// operations that mirror the Python <c>RuntimeSessionService</c>.
    /// The Unity equivalent of UE5's <c>UA3GameWorldSessionSubsystem</c>.
    /// </summary>
    [AddComponentMenu("A3Game/World Session Subsystem")]
    [DefaultExecutionOrder(-90)]
    public class A3GameWorldSessionSubsystem : MonoBehaviour
    {
        /// <summary>Global singleton accessor.</summary>
        public static A3GameWorldSessionSubsystem Instance { get; private set; }

        /// <summary>Default world identifier when none is specified.</summary>
        [Tooltip("Default world identifier for this session.")]
        public string worldId = "world_001";

        /// <summary>
        /// Maximum number of queued inputs per controller before older
        /// entries are dropped.
        /// </summary>
        [Tooltip("Maximum queued inputs per controller.")]
        public int inputQueueSize = 64;

        // ── In-memory state dictionaries ────────────────────────────────────

        private readonly Dictionary<string, A3GameParticipantInfo> _participants =
            new Dictionary<string, A3GameParticipantInfo>();

        private readonly Dictionary<string, A3GameControllerState> _controllers =
            new Dictionary<string, A3GameControllerState>();

        private readonly Dictionary<string, A3GameEntitySnapshot> _entityStates =
            new Dictionary<string, A3GameEntitySnapshot>();

        private readonly Dictionary<string, A3GameControlBinding> _bindings =
            new Dictionary<string, A3GameControlBinding>();

        private readonly Dictionary<string, A3GameRuntimeInputState> _latestInputs =
            new Dictionary<string, A3GameRuntimeInputState>();

        private readonly Dictionary<string, Queue<A3GameRuntimeInputState>> _inputQueues =
            new Dictionary<string, Queue<A3GameRuntimeInputState>>();

        private readonly Dictionary<string, GameObject> _entityGameObjects =
            new Dictionary<string, GameObject>();

        private readonly Dictionary<string, IA3GameControllableEntity> _entityObjects =
            new Dictionary<string, IA3GameControllableEntity>();

        private A3GameRuntimeSubsystem _runtime;

        /// <summary>Cached runtime subsystem reference.</summary>
        private A3GameRuntimeSubsystem Runtime
        {
            get
            {
                if (_runtime != null) return _runtime;
                _runtime = A3GameRuntimeSubsystem.Instance
                           ?? FindObjectOfType<A3GameRuntimeSubsystem>();
                return _runtime;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[A3GameRuntime] Duplicate WorldSessionSubsystem; destroying.");
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

        /// <summary>
        /// Initialize the session with a world identifier.
        /// </summary>
        public void Initialize(string worldId)
        {
            if (!string.IsNullOrEmpty(worldId))
                this.worldId = worldId;
        }

        // ── Join ────────────────────────────────────────────────────────────

        /// <summary>
        /// Create or reconnect a participant and bind a fresh controller to
        /// its persistent entity. If a runtime subsystem is available, the
        /// actual GameObject is spawned via registered factories.
        /// Mirrors the Python <c>RuntimeSessionService.join</c>.
        /// </summary>
        public Dictionary<string, object> Join(
            string participantId = null,
            string userId = null,
            string worldId = null,
            string avatarAssetPath = null,
            string idleAnimationPath = null,
            string moveAnimationPath = null,
            string controllerKind = null,
            string unityInputHost = null,
            int unityInputPort = 0,
            A3GameEntitySpawnRequest.SpawnTransform? transform = null,
            string parameters = null,
            string entityId = null,
            string controllerId = null,
            A3GameControlMode mode = A3GameControlMode.Player,
            int priority = 0)
        {
            double now = EpochSeconds;
            string resolvedWorldId = string.IsNullOrEmpty(worldId) ? this.worldId : worldId;

            // Find or create participant.
            A3GameParticipantInfo participant;

            if (!string.IsNullOrEmpty(participantId) && _participants.TryGetValue(participantId, out participant))
            {
                // Reconnect: update fields.
                participant.online = true;
                participant.last_seen_at = now;
                if (!string.IsNullOrEmpty(userId)) participant.user_id = userId;
                if (!string.IsNullOrEmpty(avatarAssetPath)) participant.avatar_asset_path = avatarAssetPath;
                if (!string.IsNullOrEmpty(idleAnimationPath)) participant.idle_animation_path = idleAnimationPath;
                if (!string.IsNullOrEmpty(moveAnimationPath)) participant.move_animation_path = moveAnimationPath;
                if (!string.IsNullOrEmpty(unityInputHost)) participant.unity_input_host = unityInputHost;
                if (unityInputPort != 0) participant.unity_input_port = unityInputPort;
                _participants[participantId] = participant;

                // Ensure entity exists.
                if (string.IsNullOrEmpty(participant.entity_id) ||
                    !_entityStates.ContainsKey(participant.entity_id))
                {
                    entityId = entityId ?? A3GameRuntimeSubsystem.NewId("ent");
                    participant.entity_id = entityId;
                    _participants[participantId] = participant;
                    CreateEntityState(entityId, resolvedWorldId, avatarAssetPath,
                        idleAnimationPath, moveAnimationPath, transform, parameters, now);
                }
                else
                {
                    entityId = participant.entity_id;
                    // Update entity state fields.
                    UpdateEntityState(entityId, avatarAssetPath, idleAnimationPath,
                        moveAnimationPath, transform, parameters);
                }
            }
            else
            {
                // New participant.
                participantId = string.IsNullOrEmpty(participantId)
                    ? A3GameRuntimeSubsystem.NewId("p")
                    : participantId;
                entityId = string.IsNullOrEmpty(entityId)
                    ? A3GameRuntimeSubsystem.NewId("ent")
                    : entityId;

                participant = new A3GameParticipantInfo
                {
                    participant_id = participantId,
                    world_id = resolvedWorldId,
                    user_id = userId ?? string.Empty,
                    avatar_asset_path = avatarAssetPath ?? string.Empty,
                    idle_animation_path = idleAnimationPath ?? string.Empty,
                    move_animation_path = moveAnimationPath ?? string.Empty,
                    unity_input_host = unityInputHost ?? string.Empty,
                    unity_input_port = unityInputPort,
                    entity_id = entityId,
                    online = true,
                    created_at = now,
                    last_seen_at = now,
                };
                _participants[participantId] = participant;

                CreateEntityState(entityId, resolvedWorldId, avatarAssetPath,
                    idleAnimationPath, moveAnimationPath, transform, parameters, now);
            }

            entityId = participant.entity_id;

            // Create controller.
            controllerId = string.IsNullOrEmpty(controllerId)
                ? A3GameRuntimeSubsystem.NewId("ctrl")
                : controllerId;

            var controller = new A3GameControllerState
            {
                controller_id = controllerId,
                participant_id = participant.participant_id,
                world_id = participant.world_id,
                kind = string.IsNullOrEmpty(controllerKind) ? "human" : controllerKind,
                unity_input_host = !string.IsNullOrEmpty(unityInputHost)
                    ? unityInputHost
                    : participant.unity_input_host,
                unity_input_port = unityInputPort != 0
                    ? unityInputPort
                    : participant.unity_input_port,
                online = true,
                created_at = now,
                last_seen_at = now,
            };
            _controllers[controllerId] = controller;

            // Mark old controllers for this participant offline.
            var oldControllerIds = new List<string>();
            foreach (var pair in _controllers)
            {
                if (pair.Value.participant_id == participant.participant_id &&
                    pair.Key != controllerId)
                {
                    oldControllerIds.Add(pair.Key);
                }
            }
            foreach (var oldId in oldControllerIds)
            {
                var oldController = _controllers[oldId];
                oldController.online = false;
                _controllers[oldId] = oldController;

                if (_bindings.TryGetValue(oldId, out var oldBinding))
                {
                    oldBinding.active = false;
                    _bindings[oldId] = oldBinding;
                }
            }

            // Create binding.
            var binding = new A3GameControlBinding
            {
                controller_id = controllerId,
                entity_id = entityId,
                world_id = participant.world_id,
                mode = mode,
                priority = priority,
                active = mode != A3GameControlMode.Spectator,
                bound_at = now,
            };
            _bindings[controllerId] = binding;

            // Spawn actual GameObject if runtime is available and not already tracked.
            bool gameObjectSpawned = false;
            if (!_entityGameObjects.ContainsKey(entityId) && Runtime != null)
            {
                var spawnTransform = transform ?? A3GameEntitySpawnRequest.SpawnTransform.Identity;
                var request = new A3GameEntitySpawnRequest
                {
                    entity_id = entityId,
                    avatar_asset_path = participant.avatar_asset_path,
                    idle_animation_path = participant.idle_animation_path,
                    move_animation_path = participant.move_animation_path,
                    actor_label = "A3Game_Entity_" + entityId,
                    spawn_transform = spawnTransform,
                    parameters = parameters ?? string.Empty,
                    persistent = true,
                };
                GameObject obj = Runtime.SpawnEntity(request);
                if (obj != null)
                {
                    _entityGameObjects[entityId] = obj;
                    var controllable = FindControllableEntity(obj);
                    if (controllable != null)
                        _entityObjects[entityId] = controllable;
                    gameObjectSpawned = true;
                }
            }

            return new Dictionary<string, object>
            {
                { "ok", true },
                { "world_id", participant.world_id },
                { "participant_id", participant.participant_id },
                { "controller_id", controllerId },
                { "entity_id", entityId },
                { "entity_persistent", true },
                { "game_object_spawned", gameObjectSpawned },
            };
        }

        // ── Leave ───────────────────────────────────────────────────────────

        /// <summary>
        /// Mark a controller/participant offline without destroying the entity.
        /// Mirrors the Python <c>RuntimeSessionService.leave</c>.
        /// </summary>
        public Dictionary<string, object> Leave(
            string participantId = null,
            string controllerId = null)
        {
            double now = EpochSeconds;
            string resolvedParticipantId = participantId;
            string resolvedControllerId = controllerId;

            // Resolve controller.
            A3GameControllerState controller;
            if (!string.IsNullOrEmpty(controllerId))
            {
                _controllers.TryGetValue(controllerId, out controller);
            }
            else
            {
                controller = ResolveControllerByParticipant(participantId);
                if (controller.controller_id != null)
                    resolvedControllerId = controller.controller_id;
            }

            if (controller.controller_id != null)
            {
                controller.online = false;
                controller.last_seen_at = now;
                _controllers[controller.controller_id] = controller;
                resolvedParticipantId = controller.participant_id;

                if (_bindings.TryGetValue(controller.controller_id, out var binding))
                {
                    binding.active = false;
                    _bindings[controller.controller_id] = binding;
                }
            }

            // Update participant online status.
            string entityId = string.Empty;
            if (!string.IsNullOrEmpty(resolvedParticipantId) &&
                _participants.TryGetValue(resolvedParticipantId, out var participant))
            {
                bool anyOnline = false;
                foreach (var pair in _controllers)
                {
                    if (pair.Value.participant_id == resolvedParticipantId && pair.Value.online)
                    {
                        anyOnline = true;
                        break;
                    }
                }
                participant.online = anyOnline;
                participant.last_seen_at = now;
                _participants[resolvedParticipantId] = participant;
                entityId = participant.entity_id;
            }

            bool entityRetained = !string.IsNullOrEmpty(entityId) && _entityStates.ContainsKey(entityId);

            return new Dictionary<string, object>
            {
                { "ok", true },
                { "participant_id", resolvedParticipantId ?? string.Empty },
                { "controller_id", resolvedControllerId ?? string.Empty },
                { "entity_id", entityId },
                { "entity_retained", entityRetained },
            };
        }

        // ── Heartbeat ───────────────────────────────────────────────────────

        /// <summary>
        /// Refresh the last-seen timestamp for a controller and its participant.
        /// Mirrors the Python <c>RuntimeSessionService.heartbeat</c>.
        /// </summary>
        public Dictionary<string, object> Heartbeat(string controllerId)
        {
            if (string.IsNullOrEmpty(controllerId))
                return ErrorResult("Missing controller_id");

            if (!_controllers.TryGetValue(controllerId, out var controller))
                return ErrorResult("Unknown controller_id: " + controllerId);

            double now = EpochSeconds;
            controller.online = true;
            controller.last_seen_at = now;
            _controllers[controllerId] = controller;

            string entityId = string.Empty;
            if (_participants.TryGetValue(controller.participant_id, out var participant))
            {
                participant.online = true;
                participant.last_seen_at = now;
                _participants[controller.participant_id] = participant;

                if (_bindings.TryGetValue(controllerId, out var binding))
                    entityId = binding.entity_id;
            }

            return new Dictionary<string, object>
            {
                { "ok", true },
                { "world_id", controller.world_id },
                { "participant_id", controller.participant_id },
                { "controller_id", controllerId },
                { "entity_id", entityId },
            };
        }

        // ── ApplyInput ───────────────────────────────────────────────────────

        /// <summary>
        /// Validate and apply an input state to the entity bound to the
        /// input's controller. Updates the stored entity state and calls
        /// ApplyInput on the live entity if available.
        /// Mirrors the Python <c>RuntimeSessionService.apply_input</c>.
        /// </summary>
        public Dictionary<string, object> ApplyInput(A3GameRuntimeInputState input)
        {
            if (string.IsNullOrEmpty(input.controller_id))
                return ErrorResult("Missing controller_id");

            if (!_controllers.TryGetValue(input.controller_id, out var controller))
                return ErrorResult("Unknown controller_id: " + input.controller_id);

            if (!_bindings.TryGetValue(input.controller_id, out var binding) || !binding.active)
                return ErrorResult("Controller is not bound to an active entity: " + input.controller_id);

            if (!string.IsNullOrEmpty(input.participant_id) &&
                input.participant_id != controller.participant_id)
                return ErrorResult("Input participant_id does not match controller owner");

            if (!string.IsNullOrEmpty(input.entity_id) && input.entity_id != binding.entity_id)
                return ErrorResult("Input entity_id does not match active control binding");

            // Normalize input.
            double now = EpochSeconds;
            input = input.Normalized(controller.world_id, controller.participant_id, binding.entity_id, now);

            // Store in latest inputs and queue.
            _latestInputs[input.controller_id] = input;
            if (!_inputQueues.TryGetValue(input.controller_id, out var queue))
            {
                queue = new Queue<A3GameRuntimeInputState>();
                _inputQueues[input.controller_id] = queue;
            }
            queue.Enqueue(input);
            while (queue.Count > inputQueueSize)
                queue.Dequeue();

            // Update entity state.
            if (_entityStates.TryGetValue(binding.entity_id, out var entityState))
            {
                entityState.last_input_at = input.ts;
                entityState.rotation = new Vector3(input.pitch, input.yaw, 0f);
                entityState.locomotion_state = input.locomotion_state;
                entityState.motion_state = LocomotionToString(input.locomotion_state);
                _entityStates[binding.entity_id] = entityState;
            }

            // Update controller last-seen.
            controller.last_seen_at = now;
            controller.online = true;
            _controllers[input.controller_id] = controller;

            // Apply to the live entity if available.
            if (_entityObjects.TryGetValue(binding.entity_id, out var entity))
            {
                try
                {
                    entity.ApplyInput(input);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[A3GameRuntime] Entity ApplyInput threw: " + e.Message);
                }
            }

            return new Dictionary<string, object>
            {
                { "ok", true },
                { "world_id", input.world_id },
                { "participant_id", input.participant_id },
                { "controller_id", input.controller_id },
                { "entity_id", input.entity_id },
                { "queued", queue.Count },
                { "locomotion_state", LocomotionToString(input.locomotion_state) },
                { "seq", input.seq },
            };
        }

        // ── Snapshot ───────────────────────────────────────────────────────

        /// <summary>
        /// Return a complete world-state snapshot: participants, controllers,
        /// bindings, and entity snapshots.
        /// Mirrors the Python <c>RuntimeSessionService.world_snapshot</c>.
        /// </summary>
        public Dictionary<string, object> Snapshot(string worldId = null)
        {
            string resolvedWorldId = string.IsNullOrEmpty(worldId) ? this.worldId : worldId;

            var participants = new List<A3GameParticipantInfo>();
            var controllers = new List<A3GameControllerState>();
            var bindings = new List<A3GameControlBinding>();
            var entities = new List<A3GameEntitySnapshot>();

            foreach (var pair in _participants)
                if (pair.Value.world_id == resolvedWorldId)
                    participants.Add(pair.Value);

            foreach (var pair in _controllers)
                if (pair.Value.world_id == resolvedWorldId)
                    controllers.Add(pair.Value);

            foreach (var pair in _bindings)
                if (pair.Value.world_id == resolvedWorldId)
                    bindings.Add(pair.Value);

            foreach (var pair in _entityStates)
            {
                if (pair.Value.world_id != resolvedWorldId) continue;

                // Refresh position/rotation from the live entity if available.
                var snapshot = pair.Value;
                if (_entityObjects.TryGetValue(pair.Key, out var entity))
                {
                    try
                    {
                        snapshot = entity.GetSnapshot();
                    }
                    catch (Exception)
                    {
                        // Fall back to stored state.
                    }
                }
                else if (_entityGameObjects.TryGetValue(pair.Key, out var go) && go != null)
                {
                    snapshot.position = go.transform.position;
                    snapshot.rotation = go.transform.eulerAngles;
                }
                entities.Add(snapshot);
            }

            return new Dictionary<string, object>
            {
                { "ok", true },
                { "world_id", resolvedWorldId },
                { "participants", participants },
                { "controllers", controllers },
                { "bindings", bindings },
                { "entities", entities },
                { "avatars", entities },
                { "server_time", EpochSeconds },
            };
        }

        // ── ResetWorld ──────────────────────────────────────────────────────

        /// <summary>
        /// Clear all broker state for a world. Does not destroy Unity
        /// GameObjects — call <see cref="A3GameRuntimeSubsystem.DestroyEntity"/>
        /// for that.
        /// Mirrors the Python <c>RuntimeSessionService.reset_world</c>.
        /// </summary>
        public Dictionary<string, object> ResetWorld(string worldId = null)
        {
            string resolvedWorldId = string.IsNullOrEmpty(worldId) ? this.worldId : worldId;

            var participantIds = new List<string>();
            var controllerIds = new List<string>();
            var entityIds = new List<string>();
            var bindingIds = new List<string>();

            foreach (var pair in _participants)
                if (pair.Value.world_id == resolvedWorldId)
                    participantIds.Add(pair.Key);

            foreach (var pair in _controllers)
                if (pair.Value.world_id == resolvedWorldId)
                    controllerIds.Add(pair.Key);

            foreach (var pair in _entityStates)
                if (pair.Value.world_id == resolvedWorldId)
                    entityIds.Add(pair.Key);

            foreach (var pair in _bindings)
                if (pair.Value.world_id == resolvedWorldId)
                    bindingIds.Add(pair.Key);

            foreach (var key in participantIds) _participants.Remove(key);
            foreach (var key in controllerIds)
            {
                _controllers.Remove(key);
                _latestInputs.Remove(key);
                _inputQueues.Remove(key);
            }
            foreach (var key in entityIds)
            {
                _entityStates.Remove(key);
                _entityGameObjects.Remove(key);
                _entityObjects.Remove(key);
            }
            foreach (var key in bindingIds) _bindings.Remove(key);

            return new Dictionary<string, object>
            {
                { "ok", true },
                { "world_id", resolvedWorldId },
                { "removed_participants", participantIds.Count },
                { "removed_controllers", controllerIds.Count },
                { "removed_entities", entityIds.Count },
                { "removed_bindings", bindingIds.Count },
            };
        }

        // ── ClearEntity ────────────────────────────────────────────────────

        /// <summary>
        /// Clear one participant/entity, preserving unrelated runtime users.
        /// Mirrors the Python <c>RuntimeSessionService.clear_entity</c>.
        /// </summary>
        public Dictionary<string, object> ClearEntity(
            string participantId = null,
            string controllerId = null,
            string entityId = null,
            bool destroyActor = true)
        {
            // Resolve controller → participant → entity.
            A3GameControllerState controller;
            if (!string.IsNullOrEmpty(controllerId) && _controllers.TryGetValue(controllerId, out controller))
            {
                participantId = controller.participant_id;
                if (string.IsNullOrEmpty(entityId) && _bindings.TryGetValue(controllerId, out var binding))
                    entityId = binding.entity_id;
            }
            else if (!string.IsNullOrEmpty(participantId) && _participants.TryGetValue(participantId, out var participant))
            {
                if (string.IsNullOrEmpty(entityId))
                    entityId = participant.entity_id;
            }

            // Find controllers and bindings to remove.
            var controllerIdsToRemove = new List<string>();
            var bindingIdsToRemove = new List<string>();

            foreach (var pair in _controllers)
            {
                if ((!string.IsNullOrEmpty(participantId) && pair.Value.participant_id == participantId) ||
                    (_bindings.TryGetValue(pair.Key, out var b) && b.entity_id == entityId))
                {
                    controllerIdsToRemove.Add(pair.Key);
                }
            }

            foreach (var pair in _bindings)
            {
                if (pair.Value.entity_id == entityId || controllerIdsToRemove.Contains(pair.Key))
                    bindingIdsToRemove.Add(pair.Key);
            }

            // Remove controllers, bindings, inputs.
            foreach (var key in controllerIdsToRemove)
            {
                _controllers.Remove(key);
                _latestInputs.Remove(key);
                _inputQueues.Remove(key);
            }
            foreach (var key in bindingIdsToRemove)
                _bindings.Remove(key);

            // Remove participant.
            bool removedParticipant = false;
            if (!string.IsNullOrEmpty(participantId))
                removedParticipant = _participants.Remove(participantId);

            // Remove entity state.
            bool removedEntity = false;
            if (!string.IsNullOrEmpty(entityId))
            {
                removedEntity = _entityStates.Remove(entityId);
                _entityObjects.Remove(entityId);

                if (_entityGameObjects.TryGetValue(entityId, out var go))
                {
                    _entityGameObjects.Remove(entityId);
                    if (destroyActor && go != null && Runtime != null)
                        Runtime.DestroyEntity(entityId);
                    else if (destroyActor && go != null)
                        Destroy(go);
                }
            }

            return new Dictionary<string, object>
            {
                { "ok", true },
                { "participant_id", participantId ?? string.Empty },
                { "controller_id", controllerId ?? string.Empty },
                { "entity_id", entityId ?? string.Empty },
                { "removed_controllers", controllerIdsToRemove.Count },
                { "removed_bindings", bindingIdsToRemove.Count },
                { "removed_participant", removedParticipant },
                { "removed_entity", removedEntity },
            };
        }

        // ── Public accessors ────────────────────────────────────────────────

        /// <summary>Number of registered participants.</summary>
        public int ParticipantCount => _participants.Count;

        /// <summary>Number of registered controllers.</summary>
        public int ControllerCount => _controllers.Count;

        /// <summary>Number of tracked entities.</summary>
        public int EntityCount => _entityStates.Count;

        /// <summary>Number of active bindings.</summary>
        public int BindingCount => _bindings.Count;

        /// <summary>Look up a participant by ID.</summary>
        public bool TryGetParticipant(string participantId, out A3GameParticipantInfo participant)
        {
            return _participants.TryGetValue(participantId, out participant);
        }

        /// <summary>Look up a controller by ID.</summary>
        public bool TryGetController(string controllerId, out A3GameControllerState controller)
        {
            return _controllers.TryGetValue(controllerId, out controller);
        }

        /// <summary>Look up a binding by controller ID.</summary>
        public bool TryGetBinding(string controllerId, out A3GameControlBinding binding)
        {
            return _bindings.TryGetValue(controllerId, out binding);
        }

        /// <summary>Look up an entity snapshot by entity ID.</summary>
        public bool TryGetEntityState(string entityId, out A3GameEntitySnapshot state)
        {
            return _entityStates.TryGetValue(entityId, out state);
        }

        /// <summary>Look up the GameObject for an entity.</summary>
        public GameObject GetEntityGameObject(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            _entityGameObjects.TryGetValue(entityId, out var go);
            return go;
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private void CreateEntityState(
            string entityId,
            string worldId,
            string avatarAssetPath,
            string idleAnimationPath,
            string moveAnimationPath,
            A3GameEntitySpawnRequest.SpawnTransform? transform,
            string parameters,
            double now)
        {
            var spawnTransform = transform ?? A3GameEntitySpawnRequest.SpawnTransform.Identity;
            var entityState = new A3GameEntitySnapshot
            {
                entity_id = entityId,
                world_id = worldId,
                avatar_asset_path = avatarAssetPath ?? string.Empty,
                idle_animation_path = idleAnimationPath ?? string.Empty,
                move_animation_path = moveAnimationPath ?? string.Empty,
                actor_label = "A3Game_Entity_" + entityId,
                spawn_transform = spawnTransform,
                parameters = parameters ?? string.Empty,
                persistent = true,
                locomotion_state = A3GameLocomotionState.Idle,
                motion_state = "idle",
                position = spawnTransform.position,
                rotation = spawnTransform.rotation,
                created_at = now,
                last_input_at = 0.0,
            };
            _entityStates[entityId] = entityState;
        }

        private void UpdateEntityState(
            string entityId,
            string avatarAssetPath,
            string idleAnimationPath,
            string moveAnimationPath,
            A3GameEntitySpawnRequest.SpawnTransform? transform,
            string parameters)
        {
            if (!_entityStates.TryGetValue(entityId, out var entityState)) return;

            if (!string.IsNullOrEmpty(avatarAssetPath)) entityState.avatar_asset_path = avatarAssetPath;
            if (!string.IsNullOrEmpty(idleAnimationPath)) entityState.idle_animation_path = idleAnimationPath;
            if (!string.IsNullOrEmpty(moveAnimationPath)) entityState.move_animation_path = moveAnimationPath;
            if (transform.HasValue) entityState.spawn_transform = transform.Value;
            if (parameters != null) entityState.parameters = parameters;

            _entityStates[entityId] = entityState;
        }

        private A3GameControllerState ResolveControllerByParticipant(string participantId)
        {
            if (string.IsNullOrEmpty(participantId))
                return default;

            A3GameControllerState best = default;
            double bestTime = 0;
            foreach (var pair in _controllers)
            {
                if (pair.Value.participant_id == participantId && pair.Value.online)
                {
                    if (pair.Value.last_seen_at > bestTime)
                    {
                        best = pair.Value;
                        bestTime = pair.Value.last_seen_at;
                    }
                }
            }
            return best;
        }

        private static IA3GameControllableEntity FindControllableEntity(GameObject obj)
        {
            if (obj == null) return null;

            // Concrete generated gameplay owns physical control. The generic
            // runtime entity component is the metadata/event fallback, just as
            // UE5's component does not replace a concrete Character/Pawn.
            foreach (var mb in obj.GetComponents<MonoBehaviour>())
            {
                if (mb is IA3GameControllableEntity controllable &&
                    !(mb is A3GameRuntimeEntityComponent))
                    return controllable;
            }
            return obj.GetComponent<A3GameRuntimeEntityComponent>();
        }

        private static string LocomotionToString(A3GameLocomotionState state)
        {
            switch (state)
            {
                case A3GameLocomotionState.Idle: return "idle";
                case A3GameLocomotionState.Walk: return "walk";
                case A3GameLocomotionState.Run: return "run";
                case A3GameLocomotionState.Jump: return "jump";
                case A3GameLocomotionState.Fall: return "fall";
                case A3GameLocomotionState.Swim: return "swim";
                case A3GameLocomotionState.Crouch: return "crouch";
                case A3GameLocomotionState.Custom: return "custom";
                default: return "idle";
            }
        }

        private static Dictionary<string, object> ErrorResult(string error)
        {
            return new Dictionary<string, object>
            {
                { "ok", false },
                { "error", error },
            };
        }

        private static double EpochSeconds =>
            (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }
}
