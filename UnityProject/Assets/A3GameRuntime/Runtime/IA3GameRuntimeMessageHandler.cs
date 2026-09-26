namespace A3GameRuntime
{
    /// <summary>
    /// Handler contract for runtime messages whose type is not recognised by
    /// the built-in <see cref="A3GameRuntimeInputReceiver"/>. Register
    /// implementations with
    /// <see cref="A3GameRuntimeSubsystem.RegisterMessageHandler"/>.
    /// The Unity equivalent of UE5's <c>IA3GameRuntimeMessageHandler</c>.
    /// </summary>
    public interface IA3GameRuntimeMessageHandler
    {
        /// <summary>
        /// Handle a runtime message. Return <c>true</c> if the message was
        /// recognised and handled.
        /// </summary>
        /// <param name="type">Message type string from the JSON envelope.</param>
        /// <param name="jsonPayload">Raw JSON payload string.</param>
        bool HandleMessage(string type, string jsonPayload);
    }
}
