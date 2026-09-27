using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Core.Logging;

#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_APP_ENABLED
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
#endif

namespace Unity.Firebase
{
    /// <summary>
    /// Quản lý vòng đời và cấu hình cốt lõi của Firebase (Firebase App, Analytics, Remote Config).
    /// Hỗ trợ nạp cấu hình mặc định từ Resources, cache mã hóa nội bộ và cơ chế Graceful Fallback khi offline.
    /// </summary>
    public static class FirebaseManager
    {
        private static bool _isFirebaseInitialized;
        public static bool IsFirebaseInitialized
        {
            get => _isFirebaseInitialized;
            private set
            {
                _isFirebaseInitialized = value;
                if (value)
                {
                    _tcsFirebase?.TrySetResult(true);
                }
            }
        }

        private static bool _isRemoteConfigInitialized;
        public static bool IsRemoteConfigInitialized
        {
            get => _isRemoteConfigInitialized;
            private set
            {
                _isRemoteConfigInitialized = value;
                if (value)
                {
                    _tcsRemoteConfig?.TrySetResult(true);
                }
            }
        }

        public static bool IsEnableRemoteConfig { get; set; } = true;

        private static readonly TaskCompletionSource<bool> _tcsFirebase = new TaskCompletionSource<bool>();
        private static readonly TaskCompletionSource<bool> _tcsRemoteConfig = new TaskCompletionSource<bool>();
        private static readonly Dictionary<string, object> KeyValuePairs = new Dictionary<string, object>();

        private static string PathLocal => Path.Combine(Application.persistentDataPath, "config_local");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitFirebaseManager()
        {
#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_APP_ENABLED
            try
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    DependencyStatus dependencyStatus = task.Result;
                    if (dependencyStatus == DependencyStatus.Available)
                    {
                        AppLogger.Log("[FirebaseManager] Firebase Dependencies Available.");
                        InitializeRemoteConfig();
                        InitializeAnalytics();
                        IsFirebaseInitialized = true;
                    }
                    else
                    {
                        AppLogger.LogError($"[FirebaseManager] Could not resolve Firebase dependencies: {dependencyStatus}");
                        InitializeFallbackLocalConfig();
                    }
                });
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseManager] Exception checking dependencies: {ex.Message}");
                InitializeFallbackLocalConfig();
            }
#else
            AppLogger.Log("[FirebaseManager] Running in Mock/Local fallback mode (Firebase SDK not compiled).");
            InitializeFallbackLocalConfig();
            IsFirebaseInitialized = true;
            IsRemoteConfigInitialized = true;
#endif
        }

        private static void InitializeFallbackLocalConfig()
        {
            LoadDataDefault();
            IsRemoteConfigInitialized = true;
        }

        private static void LoadDataDefault()
        {
            KeyValuePairs.Clear();

            // 1. Tải cấu hình mặc định từ Resources
            string platformSuffix = Application.platform == RuntimePlatform.Android ? "_Android" : (Application.platform == RuntimePlatform.IPhonePlayer ? "_iOS" : "");
            TextAsset data = Resources.Load<TextAsset>($"RemoteConfig{platformSuffix}") ?? Resources.Load<TextAsset>("RemoteConfig");

            if (data != null && !string.IsNullOrEmpty(data.text))
            {
                try
                {
                    var parsed = ParseJsonToDict(data.text);
                    foreach (var kvp in parsed)
                    {
                        KeyValuePairs[kvp.Key] = kvp.Value;
                    }
                    AppLogger.Log($"[FirebaseManager] Đã tải {parsed.Count} key mặc định từ Resources.");
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"[FirebaseManager] Lỗi đọc JSON Resources: {ex.Message}");
                }
            }

            // 2. Nạp hoặc cập nhật từ file cache local
            try
            {
                if (File.Exists(PathLocal))
                {
                    string localContent = File.ReadAllText(PathLocal);
                    var localDict = ParseJsonToDict(localContent);
                    if (localDict.Count >= KeyValuePairs.Count)
                    {
                        foreach (var kvp in localDict)
                        {
                            KeyValuePairs[kvp.Key] = kvp.Value;
                        }
                        AppLogger.Log("[FirebaseManager] Đã nạp Remote Config từ cache local.");
                    }
                }
                else if (data != null && !string.IsNullOrEmpty(data.text))
                {
                    SaveDataLocal(data.text);
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseManager] Lỗi xử lý cache local: {ex.Message}");
            }
        }

        private static void SaveDataLocal(string jsonContent)
        {
            if (!IsEnableRemoteConfig || string.IsNullOrEmpty(jsonContent)) return;

            try
            {
                File.WriteAllText(PathLocal, jsonContent);
                AppLogger.Log("[FirebaseManager] Đã lưu cache Remote Config vào local storage.");
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseManager] Lỗi ghi file local: {ex.Message}");
            }
        }

        private static void InitializeRemoteConfig()
        {
            LoadDataDefault();

            if (!IsEnableRemoteConfig)
            {
                AppLogger.LogWarning("[FirebaseManager] Remote Config bị vô hiệu hóa bởi cấu hình.");
                IsRemoteConfigInitialized = true;
                return;
            }

#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_REMOTECONFIG_ENABLED
            try
            {
                FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(KeyValuePairs)
                    .ContinueWithOnMainThread(task =>
                    {
                        AppLogger.Log("[FirebaseManager] Remote Config Defaults đã áp dụng. Bắt đầu Fetch...");
                        FetchRemoteConfig();
                    });
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseManager] Lỗi khởi tạo Remote Config: {ex.Message}");
                IsRemoteConfigInitialized = true;
            }
#else
            IsRemoteConfigInitialized = true;
#endif
        }

        public static void FetchRemoteConfig(Action<bool> onComplete = null)
        {
#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_REMOTECONFIG_ENABLED
            try
            {
                FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(fetchTask =>
                {
                    if (fetchTask.IsCompleted && !fetchTask.IsFaulted)
                    {
                        var info = FirebaseRemoteConfig.DefaultInstance.Info;
                        if (info.LastFetchStatus == LastFetchStatus.Success)
                        {
                            FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(activateTask =>
                            {
                                AppLogger.Log("[FirebaseManager] Remote Config Fetch & Activate thành công.");
                                
                                var updatedDict = new Dictionary<string, object>();
                                foreach (var item in FirebaseRemoteConfig.DefaultInstance.AllValues)
                                {
                                    updatedDict[item.Key] = item.Value.StringValue;
                                    KeyValuePairs[item.Key] = item.Value.StringValue;
                                }

                                SaveDataLocal(DictToJson(updatedDict));
                                IsRemoteConfigInitialized = true;
                                onComplete?.Invoke(true);
                            });
                            return;
                        }
                    }

                    AppLogger.LogWarning("[FirebaseManager] Fetch Remote Config không thành công hoặc dùng cache.");
                    IsRemoteConfigInitialized = true;
                    onComplete?.Invoke(false);
                });
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseManager] Lỗi FetchAsync: {ex.Message}");
                IsRemoteConfigInitialized = true;
                onComplete?.Invoke(false);
            }
#else
            IsRemoteConfigInitialized = true;
            onComplete?.Invoke(true);
#endif
        }

        private static void InitializeAnalytics()
        {
#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_ANALYTICS_ENABLED
            try
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                string signupMethod = Application.platform == RuntimePlatform.Android ? "Google" : (Application.platform == RuntimePlatform.IPhonePlayer ? "Apple" : "Unity");
                FirebaseAnalytics.SetUserProperty(FirebaseAnalytics.UserPropertySignUpMethod, signupMethod);
                FirebaseAnalytics.SetSessionTimeoutDuration(new TimeSpan(0, 30, 0));
                AppLogger.Log($"[FirebaseManager] Firebase Analytics đã kích hoạt (Sign-up: {signupMethod}).");
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseManager] Lỗi khởi tạo Analytics: {ex.Message}");
            }
#endif
        }

        public static bool TryGetValue<T>(string key, out T value)
        {
            if (KeyValuePairs.TryGetValue(key, out var rawVal))
            {
                try
                {
                    value = (T)Convert.ChangeType(rawVal, typeof(T));
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }

            value = default;
            return false;
        }

        public static T GetValue<T>(string key, T defaultValue = default)
        {
#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_REMOTECONFIG_ENABLED
            if (IsRemoteConfigInitialized && Application.internetReachability != NetworkReachability.NotReachable)
            {
                try
                {
                    var configValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
                    if (configValue.Source != ValueSource.StaticValue)
                    {
                        if (typeof(T) == typeof(string)) return (T)(object)configValue.StringValue;
                        if (typeof(T) == typeof(int)) return (T)(object)(int)configValue.LongValue;
                        if (typeof(T) == typeof(long)) return (T)(object)configValue.LongValue;
                        if (typeof(T) == typeof(float)) return (T)(object)(float)configValue.DoubleValue;
                        if (typeof(T) == typeof(double)) return (T)(object)configValue.DoubleValue;
                        if (typeof(T) == typeof(bool)) return (T)(object)configValue.BooleanValue;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogWarning($"[FirebaseManager] Lỗi lấy key '{key}' từ SDK: {ex.Message}");
                }
            }
#endif
            if (KeyValuePairs.TryGetValue(key, out var localVal))
            {
                try
                {
                    return (T)Convert.ChangeType(localVal, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        public static Task WaitForFirebaseInitialized(CancellationToken cancellationToken = default)
        {
            if (_isFirebaseInitialized) return Task.CompletedTask;
            cancellationToken.Register(() => _tcsFirebase.TrySetCanceled());
            return _tcsFirebase.Task;
        }

        public static Task WaitForFirebaseRemoteConfig(CancellationToken cancellationToken = default)
        {
            if (_isRemoteConfigInitialized) return Task.CompletedTask;
            cancellationToken.Register(() => _tcsRemoteConfig.TrySetCanceled());
            return _tcsRemoteConfig.Task;
        }

        #region JSON Helper (Không bắt buộc phụ thuộc external DLL ở runtime)
        private static Dictionary<string, object> ParseJsonToDict(string json)
        {
            var dict = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(json)) return dict;

            // Đơn giản hóa parser key-value string/primitive an toàn
            json = json.Trim();
            if (json.StartsWith("{") && json.EndsWith("}"))
            {
                json = json.Substring(1, json.Length - 2);
                string[] entries = json.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var entry in entries)
                {
                    int colonIdx = entry.IndexOf(':');
                    if (colonIdx > 0)
                    {
                        string k = entry.Substring(0, colonIdx).Trim().Trim('"', '\'');
                        string v = entry.Substring(colonIdx + 1).Trim().Trim('"', '\'');
                        if (!string.IsNullOrEmpty(k))
                        {
                            dict[k] = v;
                        }
                    }
                }
            }
            return dict;
        }

        private static string DictToJson(Dictionary<string, object> dict)
        {
            var sb = new System.Text.StringBuilder("{\n");
            int count = 0;
            foreach (var kvp in dict)
            {
                sb.Append($"  \"{kvp.Key}\": \"{kvp.Value}\"");
                if (++count < dict.Count) sb.Append(",");
                sb.Append("\n");
            }
            sb.Append("}");
            return sb.ToString();
        }
        #endregion
    }
}
