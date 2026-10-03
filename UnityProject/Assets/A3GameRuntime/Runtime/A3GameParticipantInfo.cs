using System;

namespace A3GameRuntime
{
    /// <summary>
    /// Session-level metadata for a runtime participant (a connected user).
    /// Mirrors the Python <c>RuntimeParticipantInfo</c> dataclass.
    /// </summary>
    [Serializable]
    public struct A3GameParticipantInfo
    {
        /// <summary>Unique participant identifier.</summary>
        public string participant_id;

        /// <summary>World this participant belongs to.</summary>
        public string world_id;

        /// <summary>External user identifier (from the platform).</summary>
        public string user_id;

        /// <summary>Asset path for the participant's avatar.</summary>
        public string avatar_asset_path;

        /// <summary>Asset path for the idle animation.</summary>
        public string idle_animation_path;

        /// <summary>Asset path for the movement animation.</summary>
        public string move_animation_path;

        /// <summary>Unity runtime input bridge host.</summary>
        public string unity_input_host;

        /// <summary>Unity runtime input bridge UDP port.</summary>
        public int unity_input_port;

        /// <summary>Entity currently associated with this participant.</summary>
        public string entity_id;

        /// <summary>Whether the participant is currently online.</summary>
        public bool online;

        /// <summary>Unix timestamp (seconds) when the participant first joined.</summary>
        public double created_at;

        /// <summary>Unix timestamp (seconds) of the last heartbeat or activity.</summary>
        public double last_seen_at;
    }
}
