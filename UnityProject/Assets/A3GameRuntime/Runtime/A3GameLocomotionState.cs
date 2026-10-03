namespace A3GameRuntime
{
    /// <summary>
    /// Locomotion state for a controllable entity, derived from input.
    /// The Unity equivalent of UE5's <c>EA3GameLocomotionState</c>.
    /// </summary>
    public enum A3GameLocomotionState
    {
        /// <summary>Standing still, no movement input.</summary>
        Idle = 0,

        /// <summary>Walking at normal speed.</summary>
        Walk = 1,

        /// <summary>Running at increased speed.</summary>
        Run = 2,

        /// <summary>Jumping (vertical movement initiated).</summary>
        Jump = 3,

        /// <summary>Falling (airborne, descending).</summary>
        Fall = 4,

        /// <summary>Swimming in water volumes.</summary>
        Swim = 5,

        /// <summary>Crouching at reduced height and speed.</summary>
        Crouch = 6,

        /// <summary>Custom locomotion state defined by generated gameplay code.</summary>
        Custom = 7,
    }
}
