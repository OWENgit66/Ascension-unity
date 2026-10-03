using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace A3GameRuntime
{
    /// <summary>
    /// MonoBehaviour that receives UDP JSON datagrams on a configurable port
    /// (default 30030) and dispatches them to the runtime and session
    /// subsystems. Uses a background thread for receiving and processes
    /// messages on the main thread in <see cref="Update"/>.
    /// The Unity equivalent of UE5's <c>AA3GameRuntimeInputReceiver</c>.
    /// </summary>
    [AddComponentMenu("A3Game/Runtime Input Receiver")]
    [DisallowMultipleComponent]
    public class A3GameRuntimeInputReceiver : MonoBehaviour
    {
        /// <summary>Default UDP listen port for runtime input.</summary>
        public const int DefaultPort = 30030;

        /// <summary>UDP port to listen on (default 30030).</summary>
        [Tooltip("UDP port for receiving runtime input datagrams.")]
        public int port = DefaultPort;

        /// <summary>Whether to automatically start receiving on enable.</summary>
        [Tooltip("If true, the receiver starts listening on OnEnable.")]
        public bool autoStart = true;

        /// <summary>Maximum receive buffer size in bytes.</summary>
        [Tooltip("UDP receive buffer size in bytes.")]
        public int receiveBufferSize = 2 * 1024 * 1024;

        private UdpClient _udpClient;
        private Thread _receiveThread;
        private volatile bool _running;
        private readonly ConcurrentQueue<string> _messageQueue =
            new ConcurrentQueue<string>();

        private A3GameRuntimeSubsystem _runtime;
        private A3GameWorldSessionSubsystem _session;

        /// <summary>Number of messages waiting to be processed.</summary>
        public int PendingMessageCount => _messageQueue.Count;

        /// <summary>Whether the receiver is currently listening.</summary>
        public bool IsRunning => _running && _udpClient != null;

        void OnEnable()
        {
            ResolvePortFromCommandLine();
            if (autoStart)
                StartReceiver();
        }

        void OnDisable()
        {
            StopReceiver();
        }

        void Update()
        {
            // Drain the message queue on the main thread.
            while (_messageQueue.TryDequeue(out string json))
            {
                try
                {
                    HandleRuntimeJson(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[A3GameRuntime] Failed to handle message: " + e.Message);
                }
            }
        }

        void OnDestroy()
        {
            StopReceiver();
        }

        /// <summary>
        /// Start the UDP receiver on the configured port.
        /// Returns <c>true</c> if the socket was successfully bound.
        /// </summary>
        public bool StartReceiver()
        {
            if (_running) return true;

            try
            {
                _udpClient = new UdpClient(port);
                _udpClient.Client.SendBufferSize = receiveBufferSize;
                _udpClient.Client.ReceiveBufferSize = receiveBufferSize;
            }
            catch (SocketException e)
            {
                Debug.LogError(
                    "[A3GameRuntime] Failed to bind UDP port " + port + ": " + e.Message);
                _udpClient?.Dispose();
                _udpClient = null;
                return false;
            }

            _running = true;
            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "A3GameRuntimeInputReceiver"
            };
            _receiveThread.Start();

            Debug.Log("[A3GameRuntime] UDP receiver started on port " + port);
            return true;
        }

        /// <summary>
        /// Stop the UDP receiver and close the socket.
        /// </summary>
        public void StopReceiver()
        {
            _running = false;

            if (_udpClient != null)
            {
                try { _udpClient.Close(); }
                catch { /* ignore */ }
                _udpClient = null;
            }

            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                try { _receiveThread.Join(1000); }
                catch { /* ignore */ }
            }
            _receiveThread = null;
        }

        /// <summary>
        /// Process a raw JSON message string. Parses the "type" field and
        /// dispatches to the appropriate handler.
        /// </summary>
        public bool HandleRuntimeJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            var fields = SimpleJson.Parse(json);
            if (fields == null) return false;

            string type = SimpleJson.GetString(fields, "type");
            if (string.IsNullOrEmpty(type)) return false;

            switch (type)
            {
                case "sync_session":
                    return HandleSyncSession(fields);

                case "input_state":
                case "input":
                    return HandleInputState(fields);

                case "participant_offline":
                    return HandleParticipantOffline(fields);

                case "destroy_entity":
                    return HandleDestroyEntity(fields);

                default:
                    // Forward unknown message types to the runtime subsystem.
                    var runtime = GetRuntime();
                    if (runtime != null)
                        return runtime.DispatchExtensionMessage(type, json);
                    return false;
            }
        }

        // ── Message handlers ────────────────────────────────────────────────

        private bool HandleSyncSession(Dictionary<string, string> fields)
        {
            var session = GetSession();
            if (session == null) return false;

            string worldId = SimpleJson.GetString(fields, "world_id");
            string participantId = SimpleJson.GetString(fields, "participant_id");
            string userId = SimpleJson.GetString(fields, "user_id");
            string controllerId = SimpleJson.GetString(fields, "controller_id");
            string kind = SimpleJson.GetString(fields, "kind");
            string entityId = SimpleJson.GetString(fields, "entity_id");
            string mode = SimpleJson.GetString(fields, "mode");
            int priority = SimpleJson.GetInt(fields, "priority", 0);
            string avatarAssetPath = SimpleJson.GetString(fields, "avatar_asset_path");
            string idleAnimationPath = SimpleJson.GetString(fields, "idle_animation_path");
            string moveAnimationPath = SimpleJson.GetString(fields, "move_animation_path");
            string actorLabel = SimpleJson.GetString(fields, "actor_label");
            string unityInputHost = SimpleJson.GetString(fields, "unity_input_host");
            int unityInputPort = SimpleJson.GetInt(fields, "unity_input_port", 0);
            string parameters = SimpleJson.GetRaw(fields, "parameters");

            // Generic session clients historically placed asset identity in
            // the opaque parameters object.  Prefer explicit top-level
            // fields, but fall back to that object so both wire contracts
            // create the same runtime entity.
            var parameterFields = SimpleJson.Parse(parameters);
            avatarAssetPath = GetStringWithFallback(
                fields, parameterFields, "avatar_asset_path");
            idleAnimationPath = GetStringWithFallback(
                fields, parameterFields, "idle_animation_path");
            moveAnimationPath = GetStringWithFallback(
                fields, parameterFields, "move_animation_path");
            actorLabel = GetStringWithFallback(
                fields, parameterFields, "actor_label");

            var transform = ParseTransform(fields);

            var result = session.Join(
                participantId: participantId,
                userId: userId,
                worldId: worldId,
                avatarAssetPath: avatarAssetPath,
                idleAnimationPath: idleAnimationPath,
                moveAnimationPath: moveAnimationPath,
                controllerKind: kind,
                unityInputHost: unityInputHost,
                unityInputPort: unityInputPort,
                transform: transform,
                parameters: parameters,
                entityId: entityId,
                controllerId: controllerId,
                mode: ParseControlMode(mode),
                priority: priority);

            bool ok = result.TryGetValue("ok", out var okVal) && okVal is bool b && b;
            Debug.Log("[A3GameRuntime] sync_session entity=" + entityId +
                      " controller=" + controllerId +
                      " result=" + (ok ? "ok" : "failed"));
            return ok;
        }

        private static string GetStringWithFallback(
            Dictionary<string, string> primary,
            Dictionary<string, string> fallback,
            string key)
        {
            string value = SimpleJson.GetString(primary, key);
            if (!string.IsNullOrEmpty(value)) return value;
            return SimpleJson.GetString(fallback, key);
        }

        private bool HandleInputState(Dictionary<string, string> fields)
        {
            var session = GetSession();
            if (session == null) return false;

            var input = new A3GameRuntimeInputState
            {
                world_id = SimpleJson.GetString(fields, "world_id") ?? string.Empty,
                participant_id = SimpleJson.GetString(fields, "participant_id") ?? string.Empty,
                controller_id = SimpleJson.GetString(fields, "controller_id") ?? string.Empty,
                entity_id = SimpleJson.GetString(fields, "entity_id") ?? string.Empty,
                move_x = SimpleJson.GetFloat(fields, "move_x", 0f),
                move_y = SimpleJson.GetFloat(fields, "move_y", 0f),
                run = SimpleJson.GetBool(fields, "run", false),
                jump = SimpleJson.GetBool(fields, "jump", false),
                yaw = SimpleJson.GetFloat(fields, "yaw", 0f),
                pitch = SimpleJson.GetFloat(fields, "pitch", 0f),
                seq = SimpleJson.GetInt(fields, "seq", 0),
                ts = SimpleJson.GetDouble(fields, "ts", 0.0),
            };

            var result = session.ApplyInput(input);
            return result.TryGetValue("ok", out var okVal) && okVal is bool b && b;
        }

        private bool HandleParticipantOffline(Dictionary<string, string> fields)
        {
            var session = GetSession();
            if (session == null) return false;

            string participantId = SimpleJson.GetString(fields, "participant_id");
            string controllerId = SimpleJson.GetString(fields, "controller_id");

            var result = session.Leave(participantId: participantId, controllerId: controllerId);
            return result.TryGetValue("ok", out var okVal) && okVal is bool b && b;
        }

        private bool HandleDestroyEntity(Dictionary<string, string> fields)
        {
            var session = GetSession();
            if (session == null) return false;

            string entityId = SimpleJson.GetString(fields, "entity_id");
            string participantId = SimpleJson.GetString(fields, "participant_id");
            string controllerId = SimpleJson.GetString(fields, "controller_id");
            bool destroyActor = SimpleJson.GetBool(fields, "destroy_actor", true);

            var result = session.ClearEntity(
                participantId: participantId,
                controllerId: controllerId,
                entityId: entityId,
                destroyActor: destroyActor);

            return result.TryGetValue("ok", out var okVal) && okVal is bool b && b;
        }

        // ── Subsystem accessors ────────────────────────────────────────────

        private A3GameRuntimeSubsystem GetRuntime()
        {
            if (_runtime != null) return _runtime;
            _runtime = A3GameRuntimeSubsystem.Instance
                       ?? FindObjectOfType<A3GameRuntimeSubsystem>();
            return _runtime;
        }

        private A3GameWorldSessionSubsystem GetSession()
        {
            if (_session != null) return _session;
            _session = A3GameWorldSessionSubsystem.Instance
                       ?? FindObjectOfType<A3GameWorldSessionSubsystem>();
            return _session;
        }

        // ── UDP receive thread ──────────────────────────────────────────────

        private void ReceiveLoop()
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);

            while (_running)
            {
                try
                {
                    byte[] data = _udpClient.Receive(ref endpoint);
                    if (data == null || data.Length == 0) continue;

                    string json = Encoding.UTF8.GetString(data);
                    _messageQueue.Enqueue(json);
                }
                catch (SocketException)
                {
                    // Socket closed or error — exit the loop.
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[A3GameRuntime] Receive error: " + e.Message);
                }
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void ResolvePortFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-A3GameRuntimeInputPort=", StringComparison.OrdinalIgnoreCase))
                {
                    string portStr = args[i].Substring("-A3GameRuntimeInputPort=".Length);
                    if (int.TryParse(portStr, out int parsedPort) && parsedPort > 0 && parsedPort <= 65535)
                    {
                        port = parsedPort;
                    }
                }
            }
        }

        private static A3GameControlMode ParseControlMode(string value)
        {
            if (string.IsNullOrEmpty(value)) return A3GameControlMode.Player;
            switch (value.ToLowerInvariant())
            {
                case "player":
                case "exclusive":
                    return A3GameControlMode.Player;
                case "ai":
                    return A3GameControlMode.AI;
                case "spectator":
                case "observing":
                    return A3GameControlMode.Spectator;
                case "scripted":
                    return A3GameControlMode.Scripted;
                default:
                    return A3GameControlMode.Player;
            }
        }

        private static A3GameEntitySpawnRequest.SpawnTransform? ParseTransform(
            Dictionary<string, string> fields)
        {
            string transformJson = SimpleJson.GetRaw(fields, "transform");
            if (string.IsNullOrEmpty(transformJson)) return null;

            var transformFields = SimpleJson.Parse(transformJson);
            if (transformFields == null) return null;

            // Position: try "position" then "location".
            Vector3 position = Vector3.zero;
            string positionJson = SimpleJson.GetRaw(transformFields, "position");
            if (!string.IsNullOrEmpty(positionJson))
                position = ParseVector3(positionJson);
            else
            {
                string locationJson = SimpleJson.GetRaw(transformFields, "location");
                if (!string.IsNullOrEmpty(locationJson))
                    position = ParseVector3(locationJson);
            }

            // Rotation: try "rotation" (x/y/z or pitch/yaw/roll).
            Vector3 rotation = Vector3.zero;
            string rotationJson = SimpleJson.GetRaw(transformFields, "rotation");
            if (!string.IsNullOrEmpty(rotationJson))
            {
                var rotFields = SimpleJson.Parse(rotationJson);
                if (rotFields != null)
                {
                    if (rotFields.ContainsKey("pitch"))
                    {
                        rotation = new Vector3(
                            SimpleJson.GetFloat(rotFields, "pitch", 0f),
                            SimpleJson.GetFloat(rotFields, "yaw", 0f),
                            SimpleJson.GetFloat(rotFields, "roll", 0f));
                    }
                    else
                    {
                        rotation = ParseVector3(rotationJson);
                    }
                }
            }

            return new A3GameEntitySpawnRequest.SpawnTransform
            {
                position = position,
                rotation = rotation,
            };
        }

        private static Vector3 ParseVector3(string json)
        {
            if (string.IsNullOrEmpty(json)) return Vector3.zero;
            var fields = SimpleJson.Parse(json);
            if (fields == null) return Vector3.zero;
            return new Vector3(
                SimpleJson.GetFloat(fields, "x", 0f),
                SimpleJson.GetFloat(fields, "y", 0f),
                SimpleJson.GetFloat(fields, "z", 0f));
        }

        // ── Simple JSON parser ──────────────────────────────────────────────

        /// <summary>
        /// Minimal top-level JSON object parser. Extracts key-value pairs from
        /// a JSON object string. Values are returned as strings:
        /// - String values have quotes stripped.
        /// - Object/array values are returned as raw JSON substrings.
        /// - Number/bool/null values are returned as their literal text.
        /// This avoids the need for third-party JSON libraries and handles
        /// the nested objects (transform, parameters) that JsonUtility cannot.
        /// </summary>
        internal static class SimpleJson
        {
            /// <summary>
            /// Parse a JSON object string into a dictionary of top-level
            /// key-value pairs.
            /// </summary>
            public static Dictionary<string, string> Parse(string json)
            {
                var result = new Dictionary<string, string>();
                if (string.IsNullOrEmpty(json)) return result;

                int i = SkipWhitespace(json, 0);
                if (i >= json.Length) return result;

                // Allow parsing without outer braces for robustness.
                bool hasBraces = json[i] == '{';
                if (hasBraces) i++;

                while (i < json.Length)
                {
                    i = SkipWhitespace(json, i);
                    if (i >= json.Length) break;

                    if (json[i] == '}')
                    {
                        i++;
                        break; // end of object
                    }
                    if (json[i] == ',')
                    {
                        i++;
                        continue;
                    }
                    if (json[i] != '"') break; // malformed

                    // Parse key.
                    string key;
                    int afterKey;
                    if (!TryReadString(json, i, out key, out afterKey))
                        break;
                    i = afterKey;

                    i = SkipWhitespace(json, i);
                    if (i >= json.Length || json[i] != ':') break;
                    i++; // skip ':'
                    i = SkipWhitespace(json, i);

                    // Parse value.
                    string value;
                    int afterValue;
                    if (!TryReadValue(json, i, out value, out afterValue))
                        break;
                    i = afterValue;

                    result[key] = value;
                }

                return result;
            }

            /// <summary>Get a string value (quotes already stripped).</summary>
            public static string GetString(Dictionary<string, string> values,
                string key, string defaultValue = null)
            {
                if (values == null) return defaultValue;
                return values.TryGetValue(key, out var v) ? v : defaultValue;
            }

            /// <summary>Get a float value.</summary>
            public static float GetFloat(Dictionary<string, string> values,
                string key, float defaultValue = 0f)
            {
                if (values == null) return defaultValue;
                if (!values.TryGetValue(key, out var v)) return defaultValue;
                return float.TryParse(v, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var f) ? f : defaultValue;
            }

            /// <summary>Get an int value.</summary>
            public static int GetInt(Dictionary<string, string> values,
                string key, int defaultValue = 0)
            {
                if (values == null) return defaultValue;
                if (!values.TryGetValue(key, out var v)) return defaultValue;
                return int.TryParse(v, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var n) ? n : defaultValue;
            }

            /// <summary>Get a double value.</summary>
            public static double GetDouble(Dictionary<string, string> values,
                string key, double defaultValue = 0.0)
            {
                if (values == null) return defaultValue;
                if (!values.TryGetValue(key, out var v)) return defaultValue;
                return double.TryParse(v, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var d) ? d : defaultValue;
            }

            /// <summary>Get a bool value.</summary>
            public static bool GetBool(Dictionary<string, string> values,
                string key, bool defaultValue = false)
            {
                if (values == null) return defaultValue;
                if (!values.TryGetValue(key, out var v)) return defaultValue;
                if (v == "true") return true;
                if (v == "false") return false;
                return defaultValue;
            }

            /// <summary>
            /// Get a raw value for a key. For object/array values, this returns
            /// the raw JSON substring (including braces/brackets). For string
            /// values, it returns the unquoted string. For other types, the
            /// literal text.
            /// </summary>
            public static string GetRaw(Dictionary<string, string> values,
                string key, string defaultValue = null)
            {
                if (values == null) return defaultValue;
                return values.TryGetValue(key, out var v) ? v : defaultValue;
            }

            // ── Internal parsing helpers ────────────────────────────────────

            private static int SkipWhitespace(string json, int i)
            {
                while (i < json.Length)
                {
                    char c = json[i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                        i++;
                    else
                        break;
                }
                return i;
            }

            private static bool TryReadString(string json, int start,
                out string value, out int after)
            {
                value = null;
                after = start;
                if (start >= json.Length || json[start] != '"') return false;

                var sb = new StringBuilder();
                int i = start + 1;
                while (i < json.Length)
                {
                    char c = json[i];
                    if (c == '\\' && i + 1 < json.Length)
                    {
                        char next = json[i + 1];
                        switch (next)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'n': sb.Append('\n'); break;
                            case 't': sb.Append('\t'); break;
                            case 'r': sb.Append('\r'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            default: sb.Append(next); break;
                        }
                        i += 2;
                        continue;
                    }
                    if (c == '"')
                    {
                        value = sb.ToString();
                        after = i + 1;
                        return true;
                    }
                    sb.Append(c);
                    i++;
                }
                return false;
            }

            private static bool TryReadValue(string json, int start,
                out string value, out int after)
            {
                value = null;
                after = start;
                if (start >= json.Length) return false;

                char c = json[start];

                if (c == '"')
                {
                    // String value.
                    return TryReadString(json, start, out value, out after);
                }

                if (c == '{' || c == '[')
                {
                    // Object or array — find matching close.
                    int close = FindMatchingClose(json, start);
                    if (close < 0) return false;
                    value = json.Substring(start, close - start + 1);
                    after = close + 1;
                    return true;
                }

                if (c == 't' || c == 'f' || c == 'n' ||
                    char.IsDigit(c) || c == '-')
                {
                    // Number, bool, or null — read until delimiter.
                    int i = start;
                    while (i < json.Length)
                    {
                        char ch = json[i];
                        if (ch == ',' || ch == '}' || ch == ']' ||
                            ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                            break;
                        i++;
                    }
                    value = json.Substring(start, i - start);
                    after = i;
                    return true;
                }

                return false;
            }

            private static int FindMatchingClose(string json, int openIdx)
            {
                char openChar = json[openIdx];
                char closeChar = openChar == '{' ? '}' : ']';
                int depth = 1;
                bool inString = false;
                int i = openIdx + 1;

                while (i < json.Length)
                {
                    char c = json[i];
                    if (inString)
                    {
                        if (c == '\\' && i + 1 < json.Length)
                        {
                            i += 2;
                            continue;
                        }
                        if (c == '"') inString = false;
                    }
                    else
                    {
                        if (c == '"') inString = true;
                        else if (c == openChar) depth++;
                        else if (c == closeChar)
                        {
                            depth--;
                            if (depth == 0) return i;
                        }
                    }
                    i++;
                }
                return -1;
            }
        }
    }
}
