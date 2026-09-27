using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Core.Logging;
using Unity.Core.Services.RemoteConfig;

namespace Unity.Firebase
{
    /// <summary>
    /// Adapter tích hợp Firebase Remote Config với hệ thống RemoteConfigService của Unity Core Framework.
    /// Tự động trả về cấu hình an toàn khi mất mạng hoặc chưa fetch xong.
    /// </summary>
    public class FirebaseRemoteConfigProvider : IRemoteConfigProvider
    {
        public bool IsFetched => FirebaseManager.IsRemoteConfigInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            RemoteConfigService.Register(new FirebaseRemoteConfigProvider());
        }

        public void Initialize()
        {
            AppLogger.Log("[FirebaseRemoteConfigProvider] Khởi tạo Remote Config Adapter...");
        }

        public void Shutdown()
        {
            AppLogger.Log("[FirebaseRemoteConfigProvider] Đóng Remote Config Adapter.");
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            return FirebaseManager.GetValue(key, defaultValue);
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            return FirebaseManager.TryGetValue(key, out value);
        }

        public void FetchAsync(Action<bool> onComplete = null)
        {
            FirebaseManager.FetchRemoteConfig(onComplete);
        }

        public Task WaitForFetchAsync()
        {
            return FirebaseManager.WaitForFirebaseRemoteConfig();
        }
    }
}
