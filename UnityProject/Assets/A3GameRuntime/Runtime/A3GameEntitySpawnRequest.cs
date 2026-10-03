using System;
using UnityEngine;

namespace A3GameRuntime
{
    /// <summary>
    /// Request to spawn a controllable entity in the runtime world.
    /// Mirrors the Python <c>RuntimeEntityState</c> spawn fields and the
    /// UE5 <c>FA3GameEntitySpawnRequest</c>.
    /// </summary>
    [Serializable]
    public struct A3GameEntitySpawnRequest
    {
        /// <summary>Stable runtime entity identifier. Auto-generated if empty.</summary>
        public string entity_id;

        /// <summary>Asset path for the avatar prefab/model to instantiate.</summary>
        public string avatar_asset_path;

        /// <summary>Asset path for the idle animation clip.</summary>
        public string idle_animation_path;

        /// <summary>Asset path for the movement animation clip.</summary>
        public string move_animation_path;

        /// <summary>Human-readable label for the spawned actor.</summary>
        public string actor_label;

        /// <summary>Initial transform (position + euler rotation) at spawn.</summary>
        public SpawnTransform spawn_transform;

        /// <summary>Opaque JSON parameter string passed to the entity factory.</summary>
        public string parameters;

        /// <summary>Whether the entity persists across participant reconnects.</summary>
        public bool persistent;

        /// <summary>
        /// Position + euler-angle rotation pair used at spawn time.
        /// </summary>
        [Serializable]
        public struct SpawnTransform
        {
            /// <summary>World-space position.</summary>
            public Vector3 position;

            /// <summary>Euler angles: x = pitch, y = yaw, z = roll.</summary>
            public Vector3 rotation;

            /// <summary>Convenience factory for a position-only transform.</summary>
            public static SpawnTransform Identity => new SpawnTransform
            {
                position = Vector3.zero,
                rotation = Vector3.zero,
            };
        }
    }
}
