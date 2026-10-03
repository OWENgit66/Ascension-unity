using UnityEngine;

namespace A3GameRuntime
{
    /// <summary>
    /// Factory contract for creating and destroying controllable entity
    /// GameObjects. Register implementations with
    /// <see cref="A3GameRuntimeSubsystem.RegisterFactory"/>.
    /// The Unity equivalent of UE5's <c>IA3GameEntityFactory</c>.
    /// </summary>
    public interface IA3GameEntityFactory
    {
        /// <summary>
        /// Create a GameObject for the given spawn request. Return the
        /// created GameObject, or <c>null</c> if this factory cannot handle
        /// the request.
        /// </summary>
        GameObject CreateEntity(A3GameEntitySpawnRequest request);

        /// <summary>
        /// Destroy the entity with the given identifier. Return <c>true</c> if
        /// this factory handled the destruction, <c>false</c> otherwise.
        /// </summary>
        bool DestroyEntity(string entityId);
    }
}
