using System;
using System.Security.Cryptography;
using System.Text;
using Game.Scripts.Infrastructure.Core.Services;
using UnityEngine;

namespace Game.Scripts.Infrastructure.Implementations.Services
{
    public sealed class PlayerPrefsSessionStorage : ISessionStorage
    {
        private readonly string _key;

        public PlayerPrefsSessionStorage()
        {
            // MPPM editors have separate data paths. Builds may use --player-profile=second.
            var profile = Application.isEditor ? Application.dataPath : "standalone";
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (argument.StartsWith("--player-profile="))
                    profile += ":" + argument.Substring("--player-profile=".Length);
            }
            using var hash = SHA256.Create();
            _key = "SeaBattle.Session." + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(profile)));
        }

        public string Read()
        {
            return PlayerPrefs.GetString(_key, "");
        }

        public void Write(string value)
        {
            PlayerPrefs.SetString(_key, value);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
        }
    }
}
