using UnityEngine;

namespace A3GameRuntime
{
    /// <summary>
    /// MonoBehaviour that connects a GameObject to runtime entity state.
    /// Tracks locomotion state, motion label, and last-input time, and
    /// broadcasts normalized input to concrete gameplay components.
    /// Implements <see cref="IA3GameControllableEntity"/> so the session
    /// subsystem can drive it generically.
    /// The Unity equivalent of UE5's <c>UA3GameRuntimeEntityComponent</c>.
    /// </summary>
    [AddComponentMenu("A3Game/Runtime Entity Component")]
    [DisallowMultipleComponent]
    public class A3GameRuntimeEntityComponent : MonoBehaviour, IA3GameControllableEntity
    {
        /// <summary>Stable runtime entity identifier.</summary>
        [HideInInspector] public string entityId;

        /// <summary>World this entity belongs to.</summary>
        [HideInInspector] public string worldId;

        /// <summary>Avatar asset path (set during initialization).</summary>
        [HideInInspector] public string avatarAssetPath;

        /// <summary>Idle animation asset path (set during initialization).</summary>
        [HideInInspector] public string idleAnimationPath;

        /// <summary>Movement animation asset path (set during initialization).</summary>
        [HideInInspector] public string moveAnimationPath;

        /// <summary>Actor label (set during initialization).</summary>
        [HideInInspector] public string actorLabel;

        /// <summary>Opaque JSON parameters string (set during initialization).</summary>
        [HideInInspector] public string parameters;

        /// <summary>Whether this entity persists across participant reconnects.</summary>
        public bool persistent = true;

        /// <summary>Current locomotion state (updated by ApplyInput).</summary>
        public A3GameLocomotionState locomotionState = A3GameLocomotionState.Idle;

        /// <summary>Current motion state string (updated by ApplyInput).</summary>
        [HideInInspector] public string motionState = "idle";

        /// <summary>Unix timestamp (seconds) of the last applied input.</summary>
        [HideInInspector] public double lastInputTime;

        /// <summary>Unix timestamp (seconds) when the entity was created.</summary>
        [HideInInspector] public double createdTime;

        /// <summary>Initial spawn position, captured at Initialize time.</summary>
        private Vector3 _spawnPosition;

        /// <summary>Initial spawn rotation (euler), captured at Initialize time.</summary>
        private Vector3 _spawnRotation;

        /// <summary>Whether Initialize has been called.</summary>
        private bool _initialized;

        /// <summary>
        /// Raised after locomotion metadata is updated. Concrete gameplay
        /// owns movement, gravity, jumping, and other actions, matching the
        /// UE5 A3GamePlayable component contract.
        /// </summary>
        public event System.Action<A3GameRuntimeInputState> RuntimeInput;

        /// <summary>
        /// Initialize the entity component with identity and asset metadata.
        /// Captures the spawn transform for ResetEntity.
        /// </summary>
        public void Initialize(string entityId, string worldId)
        {
            this.entityId = entityId ?? string.Empty;
            this.worldId = worldId ?? string.Empty;
            if (!_initialized)
            {
                _spawnPosition = transform.position;
                _spawnRotation = transform.eulerAngles;
                createdTime = EpochSeconds;
                _initialized = true;
            }
        }

        /// <summary>
        /// Initialize with full spawn metadata including asset paths and
        /// spawn transform.
        /// </summary>
        public void Initialize(
            string entityId,
            string worldId,
            string avatarAssetPath,
            string idleAnimationPath,
            string moveAnimationPath,
            string actorLabel,
            string parameters,
            bool persistent,
            Vector3 spawnPosition,
            Vector3 spawnRotation)
        {
            this.entityId = entityId ?? string.Empty;
            this.worldId = worldId ?? string.Empty;
            this.avatarAssetPath = avatarAssetPath ?? string.Empty;
            this.idleAnimationPath = idleAnimationPath ?? string.Empty;
            this.moveAnimationPath = moveAnimationPath ?? string.Empty;
            this.actorLabel = actorLabel ?? string.Empty;
            this.parameters = parameters ?? string.Empty;
            this.persistent = persistent;

            _spawnPosition = spawnPosition;
            _spawnRotation = spawnRotation;
            transform.position = spawnPosition;
            transform.rotation = Quaternion.Euler(spawnRotation);

            if (!_initialized)
            {
                createdTime = EpochSeconds;
                _initialized = true;
            }
        }

        /// <summary>
        /// Apply a single input tick: update normalized locomotion metadata and
        /// broadcast the input to concrete gameplay. Returns <c>true</c> when
        /// the framework accepted and dispatched the input.
        /// </summary>
        public bool ApplyInput(A3GameRuntimeInputState input)
        {
            bool moving = Mathf.Abs(input.move_x) > 0.001f || Mathf.Abs(input.move_y) > 0.001f;

            if (input.jump)
            {
                locomotionState = A3GameLocomotionState.Jump;
                motionState = "jump";
            }
            else if (moving && input.run)
            {
                locomotionState = A3GameLocomotionState.Run;
                motionState = "run";
            }
            else if (moving)
            {
                locomotionState = A3GameLocomotionState.Walk;
                motionState = "walk";
            }
            else
            {
                locomotionState = A3GameLocomotionState.Idle;
                motionState = "idle";
            }

            lastInputTime = input.ts > 0.0 ? input.ts : EpochSeconds;
            RuntimeInput?.Invoke(input);
            return true;
        }

        /// <summary>
        /// Return a serializable snapshot of the entity's current state.
        /// Position and rotation are read from the live Transform.
        /// </summary>
        public A3GameEntitySnapshot GetSnapshot()
        {
            var snapshot = new A3GameEntitySnapshot
            {
                entity_id = entityId,
                world_id = worldId,
                avatar_asset_path = avatarAssetPath,
                idle_animation_path = idleAnimationPath,
                move_animation_path = moveAnimationPath,
                actor_label = string.IsNullOrEmpty(actorLabel) ? gameObject.name : actorLabel,
                parameters = parameters ?? string.Empty,
                persistent = persistent,
                locomotion_state = locomotionState,
                motion_state = motionState,
                created_at = createdTime,
                last_input_at = lastInputTime,
            };

            if (_initialized)
            {
                snapshot.spawn_transform = new A3GameEntitySpawnRequest.SpawnTransform
                {
                    position = _spawnPosition,
                    rotation = _spawnRotation,
                };
            }

            if (transform != null)
            {
                snapshot.position = transform.position;
                snapshot.rotation = transform.eulerAngles;
            }

            return snapshot;
        }

        /// <summary>
        /// Reset the entity to its spawn transform and idle locomotion.
        /// </summary>
        public void ResetEntity()
        {
            locomotionState = A3GameLocomotionState.Idle;
            motionState = "idle";
            lastInputTime = 0.0;

            if (transform != null && _initialized)
            {
                transform.position = _spawnPosition;
                transform.rotation = Quaternion.Euler(_spawnRotation);
            }
        }

        /// <summary>Return the stable runtime entity identifier.</summary>
        public string GetEntityId()
        {
            return entityId;
        }

        private static double EpochSeconds =>
            (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
    }
}
