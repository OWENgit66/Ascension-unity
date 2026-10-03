using UnityEngine;

namespace A3GameRuntime
{
    /// <summary>
    /// MonoBehaviour that stores stable runtime identity (entity_id, world_id)
    /// on a GameObject. Attached automatically by the runtime subsystem when
    /// an entity is spawned, or manually by generated gameplay code.
    /// The Unity equivalent of UE5's <c>UA3GameIdentityComponent</c>.
    /// </summary>
    [AddComponentMenu("A3Game/Identity Component")]
    [DisallowMultipleComponent]
    public class A3GameIdentityComponent : MonoBehaviour
    {
        /// <summary>Stable runtime entity identifier.</summary>
        [HideInInspector] public string entity_id;

        /// <summary>World this entity belongs to.</summary>
        [HideInInspector] public string world_id;

        /// <summary>
        /// Initialize the identity with the given entity and world IDs.
        /// Called once after spawn; subsequent calls update the stored values.
        /// </summary>
        public void Initialize(string entityId, string worldId)
        {
            entity_id = entityId ?? string.Empty;
            world_id = worldId ?? string.Empty;
        }

        /// <summary>Whether this component has been initialized with a non-empty entity_id.</summary>
        public bool IsInitialized => !string.IsNullOrEmpty(entity_id);
    }
}
