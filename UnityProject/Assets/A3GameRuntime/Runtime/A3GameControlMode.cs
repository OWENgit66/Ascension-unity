namespace A3GameRuntime
{
    /// <summary>
    /// Defines how a controller interacts with a controllable entity.
    /// The Unity equivalent of UE5's <c>EA3GameControlMode</c>.
    /// </summary>
    public enum A3GameControlMode
    {
        /// <summary>No control mode assigned.</summary>
        None = 0,

        /// <summary>Direct human player control (default for new bindings).</summary>
        Player = 1,

        /// <summary>AI-driven control for NPCs and bots.</summary>
        AI = 2,

        /// <summary>Spectator-only observation; no input applied to the entity.</summary>
        Spectator = 3,

        /// <summary>Scripted or cinematic control driven by game logic.</summary>
        Scripted = 4,
    }
}
