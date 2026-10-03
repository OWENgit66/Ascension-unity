using System;
using UnityEngine;

namespace A3GameRuntime
{
    /// <summary>
    /// Immutable snapshot of runtime input for one tick. Carries movement axes,
    /// action flags, camera orientation, and a monotonic sequence number.
    /// Mirrors the Python <c>RuntimeInputState</c> dataclass.
    /// </summary>
    [Serializable]
    public struct A3GameRuntimeInputState
    {
        /// <summary>World the input belongs to (routing).</summary>
        public string world_id;

        /// <summary>Participant that owns the controller (routing).</summary>
        public string participant_id;

        /// <summary>Controller issuing the input (routing).</summary>
        public string controller_id;

        /// <summary>Target entity for the input (routing, validated against binding).</summary>
        public string entity_id;

        /// <summary>Strafe axis, clamped to [-1, 1].</summary>
        public float move_x;

        /// <summary>Forward axis, clamped to [-1, 1].</summary>
        public float move_y;

        /// <summary>Whether the run/sprint modifier is active.</summary>
        public bool run;

        /// <summary>Whether the jump action was triggered this tick.</summary>
        public bool jump;

        /// <summary>Camera yaw in degrees.</summary>
        public float yaw;

        /// <summary>Camera pitch in degrees.</summary>
        public float pitch;

        /// <summary>Monotonic sequence number per controller.</summary>
        public int seq;

        /// <summary>Unix timestamp (seconds) when the input was generated.</summary>
        public double ts;

        /// <summary>
        /// Locomotion state computed from the input axes and action flags.
        /// Matches the logic in <c>A3GameRuntimeEntityComponent.ApplyInput</c>
        /// and the Python <c>RuntimeSessionService._locomotion_from_input</c>.
        /// </summary>
        public A3GameLocomotionState locomotion_state
        {
            get
            {
                bool moving = Mathf.Abs(move_x) > 0.001f || Mathf.Abs(move_y) > 0.001f;
                if (jump) return A3GameLocomotionState.Jump;
                if (moving && run) return A3GameLocomotionState.Run;
                if (moving) return A3GameLocomotionState.Walk;
                return A3GameLocomotionState.Idle;
            }
        }

        /// <summary>
        /// Returns a clamped copy of this input state with move axes limited to
        /// [-1, 1] and missing routing fields filled from the provided defaults.
        /// </summary>
        public A3GameRuntimeInputState Normalized(
            string defaultWorldId,
            string resolvedParticipantId,
            string resolvedEntityId,
            double fallbackTimestamp)
        {
            var copy = this;
            if (string.IsNullOrEmpty(copy.world_id))
                copy.world_id = defaultWorldId;
            if (string.IsNullOrEmpty(copy.participant_id))
                copy.participant_id = resolvedParticipantId;
            copy.entity_id = resolvedEntityId;
            copy.move_x = Mathf.Clamp(copy.move_x, -1f, 1f);
            copy.move_y = Mathf.Clamp(copy.move_y, -1f, 1f);
            if (copy.ts <= 0.0)
                copy.ts = fallbackTimestamp;
            return copy;
        }
    }
}
