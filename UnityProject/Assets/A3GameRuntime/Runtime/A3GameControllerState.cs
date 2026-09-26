using System;

namespace A3GameRuntime
{
    /// <summary>
    /// Session-level state for a runtime controller (an input source bound to
    /// a participant). Mirrors the Python <c>RuntimeControllerState</c>
    /// dataclass.
    /// </summary>
    [Serializable]
    public struct A3GameControllerState
    {
        /// <summary>Unique controller identifier.</summary>
        public string controller_id;

        /// <summary>Participant that owns this controller.</summary>
        public string participant_id;

        /// <summary>World this controller operates in.</summary>
        public string world_id;

        /// <summary>Controller kind: "human", "ai", "scripted", etc.</summary>
        public string kind;

        /// <summary>Unity runtime input bridge host for this controller.</summary>
        public string unity_input_host;

        /// <summary>Unity runtime input bridge UDP port for this controller.</summary>
        public int unity_input_port;

        /// <summary>Whether the controller is currently online.</summary>
        public bool online;

        /// <summary>Unix timestamp (seconds) when the controller was created.</summary>
        public double created_at;

        /// <summary>Unix timestamp (seconds) of the last heartbeat.</summary>
        public double last_seen_at;
    }
}
