using System;
using UnityEngine;

namespace A3GameRuntime
{
    /// <summary>
    /// Serializable snapshot of a controllable entity's runtime state.
    /// Used for world-state serialization, observation, and session sync.
    /// Mirrors the Python <c>RuntimeEntityState</c> dataclass.
    /// </summary>
    [Serializable]
    public struct A3GameEntitySnapshot
    {
        /// <summary>Stable runtime entity identifier.</summary>
        public string entity_id;

        /// <summary>World this entity belongs to.</summary>
        public string world_id;

        /// <summary>Asset path for the avatar prefab/model.</summary>
        public string avatar_asset_path;

        /// <summary>Asset path for the idle animation.</summary>
        public string idle_animation_path;

        /// <summary>Asset path for the movement animation.</summary>
        public string move_animation_path;

        /// <summary>Human-readable actor label.</summary>
        public string actor_label;

        /// <summary>Initial spawn transform.</summary>
        public A3GameEntitySpawnRequest.SpawnTransform spawn_transform;

        /// <summary>Opaque JSON parameter string.</summary>
        public string parameters;

        /// <summary>Whether the entity persists across participant reconnects.</summary>
        public bool persistent;

        /// <summary>Current locomotion state.</summary>
        public A3GameLocomotionState locomotion_state;

        /// <summary>Current motion state as a lowercase string ("idle", "walk", "run", "jump").</summary>
        public string motion_state;

        /// <summary>Current world-space position.</summary>
        public Vector3 position;

        /// <summary>Current euler rotation: x = pitch, y = yaw, z = roll.</summary>
        public Vector3 rotation;

        /// <summary>Unix timestamp (seconds) when the entity was created.</summary>
        public double created_at;

        /// <summary>Unix timestamp (seconds) of the last input applied.</summary>
        public double last_input_at;
    }
}
