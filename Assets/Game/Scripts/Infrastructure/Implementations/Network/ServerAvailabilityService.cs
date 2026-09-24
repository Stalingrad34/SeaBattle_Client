using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Network;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.Infrastructure.Implementations.Network
{
    /// <summary>Checks the externally started server without owning its process.</summary>
    public sealed class ServerAvailabilityService
    {
        private readonly ConnectionConfig _config;
        public ServerAvailabilityService(ConnectionConfig config)
        {
            _config = config;
        }

        public async UniTask EnsureReadyAsync(CancellationToken cancellationToken)
        {
            var endpoint = new Uri(_config.Endpoint);
            if (endpoint.Scheme != "ws" && endpoint.Scheme != "wss")
                throw new InvalidOperationException("Endpoint must use ws:// or wss://.");
            var healthUrl = new UriBuilder(endpoint)
            {
                Scheme = endpoint.Scheme == "wss" ? "https" : "http",
                Path = "/health"
            }.Uri.AbsoluteUri;
            using var request = UnityWebRequest.Get(healthUrl);
            request.timeout = _config.TimeoutSeconds;
            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            }
            catch (UnityWebRequestException)
            {
                throw new InvalidOperationException("SeaBattle server is unavailable. In Server run npm start and restart Play Mode.");
            }

            var health = JsonUtility.FromJson<Health>(request.downloadHandler.text);
            if (health == null || health.service != "seabattle" || health.protocol != 1 || health.status != "ready")
                throw new InvalidOperationException("The endpoint belongs to another service or incompatible protocol.");
        }

        [Serializable]
        private sealed class Health
        {
            public string service;
            public int protocol;
            public string status;
        }
    }
}
