namespace A3GameRuntime
{
    /// <summary>
    /// Contract for a runtime-controllable entity. Implemented by
    /// <see cref="A3GameRuntimeEntityComponent"/> and optionally by
    /// custom MonoBehaviour scripts on generated actors.
    /// The Unity equivalent of UE5's <c>IA3GameControllableEntity</c>.
    /// </summary>
    public interface IA3GameControllableEntity
    {
        /// <summary>Return a serializable snapshot of the entity's current state.</summary>
        A3GameEntitySnapshot GetSnapshot();

        /// <summary>
        /// Apply a single input tick to this entity. Returns <c>true</c> if the
        /// input was accepted and applied.
        /// </summary>
        bool ApplyInput(A3GameRuntimeInputState input);

        /// <summary>
        /// Reset the entity to its initial spawn state (locomotion, position,
        /// timing). Called on world reset or entity respawn.
        /// </summary>
        void ResetEntity();

        /// <summary>Return the stable runtime entity identifier.</summary>
        string GetEntityId();
    }
}
