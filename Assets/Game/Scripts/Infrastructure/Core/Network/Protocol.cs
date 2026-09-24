using System;
using UnityEngine;

namespace Game.Scripts.Infrastructure.Core.Network
{
    public static class Protocol
    {
        public const int Version = 1;
        public const string ClientChannel = "client";
        public const string ServerChannel = "server";
        public const string Hello = "hello", Resume = "resume", Fire = "fire", Sync = "sync", Heartbeat = "heartbeat";
        public const string Welcome = "welcome", CommandResult = "commandResult", HeartbeatAck = "heartbeatAck", Error = "error";
        public static string Encode(ClientMessage message)
        {
            return JsonUtility.ToJson(message);
        }

        public static ServerMessage Decode(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 65536)
                throw new FormatException("Invalid server message size.");
            var message = JsonUtility.FromJson<ServerMessage>(json);
            if (message == null || message.protocolVersion != Version || (message.type != Welcome && message.type != CommandResult && message.type != HeartbeatAck && message.type != Error))
                throw new FormatException("Unsupported server message.");
            return message;
        }
    }

    // Public fields intentionally match TypeScript/JSON names; no Unity objects cross the wire.
    [Serializable]
    public sealed class ClientMessage
    {
        public int protocolVersion = Protocol.Version;
        public string type, requestId, matchId, playerId, connectionId, resumeToken, commandId;
        public long turnId, knownRevision;
        public int x, y;
        public double clientSentAtMs;
    }

    [Serializable]
    public sealed class ServerMessage
    {
        public int protocolVersion;
        public string type, requestId, matchId, playerId, connectionId, resumeToken;
        public double serverTimeMs, clientSentAtMs;
        public CommandResult command;
        public string errorCode;
    }

    [Serializable]
    public sealed class CommandResult
    {
        public string commandId, status, reason;
        public long turnId, revision;
    }
}
