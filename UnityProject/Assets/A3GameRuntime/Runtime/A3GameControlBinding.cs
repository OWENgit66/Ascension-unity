using System;

namespace A3GameRuntime
{
    /// <summary>
    /// Binding between a controller and a controllable entity, with a control
    /// mode and priority. Mirrors the Python <c>RuntimeControlBinding</c>
    /// dataclass.
    /// </summary>
    [Serializable]
    public struct A3GameControlBinding
    {
        /// <summary>Controller that owns this binding.</summary>
        public string controller_id;

        /// <summary>Entity that the controller is bound to.</summary>
        public string entity_id;

        /// <summary>World this binding belongs to.</summary>
        public string world_id;

        /// <summary>Control mode (Player, AI, Spectator, Scripted).</summary>
        public A3GameControlMode mode;

        /// <summary>Priority for conflict resolution when multiple controllers bind.</summary>
        public int priority;

        /// <summary>Whether this binding is currently active.</summary>
        public bool active;

        /// <summary>Unix timestamp (seconds) when the binding was established.</summary>
        public double bound_at;
    }
}
