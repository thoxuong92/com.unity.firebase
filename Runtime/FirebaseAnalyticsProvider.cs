using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Core.Logging;
using Unity.Core.Services.Analytics;

#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_ANALYTICS_ENABLED
using Firebase.Analytics;
#endif

namespace Unity.Firebase
{
    /// <summary>
    /// Adapter tích hợp Firebase Analytics với hệ thống AnalyticsService của Unity Core Framework.
    /// Cung cấp bộ hàm tracking chuẩn hóa cho Ad Formats (App Open, Banner, Interstitial, Rewarded, MRec) và Gameplay.
    /// </summary>
    public class FirebaseAnalyticsProvider : IAnalyticsProvider
    {
        public string ProviderName => "FirebaseAnalytics";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            AnalyticsService.AddProvider(new FirebaseAnalyticsProvider());
        }

        public void Initialize()
        {
            AppLogger.Log("[FirebaseAnalyticsProvider] Khởi tạo Firebase Analytics Adapter...");
        }

        public void Shutdown()
        {
            AppLogger.Log("[FirebaseAnalyticsProvider] Đóng Firebase Analytics Adapter.");
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(eventName)) return;

#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_ANALYTICS_ENABLED
            try
            {
                if (parameters == null || parameters.Count == 0)
                {
                    FirebaseAnalytics.LogEvent(eventName);
                    return;
                }

                var paramList = new List<Parameter>();
                foreach (var kvp in parameters)
                {
                    if (kvp.Value == null) continue;

                    switch (kvp.Value)
                    {
                        case int i:
                            paramList.Add(new Parameter(kvp.Key, i));
                            break;
                        case long l:
                            paramList.Add(new Parameter(kvp.Key, l));
                            break;
                        case float f:
                            paramList.Add(new Parameter(kvp.Key, f));
                            break;
                        case double d:
                            paramList.Add(new Parameter(kvp.Key, d));
                            break;
                        default:
                            paramList.Add(new Parameter(kvp.Key, kvp.Value.ToString()));
                            break;
                    }
                }

                FirebaseAnalytics.LogEvent(eventName, paramList.ToArray());
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseAnalyticsProvider] Lỗi gửi event '{eventName}': {ex.Message}");
            }
#else
            AppLogger.Log($"[FirebaseAnalytics:Mock] LogEvent: {eventName}");
#endif
        }

        public void Log(LogEventParameter parameter)
        {
            if (parameter != null)
            {
                LogEvent(parameter.Name, parameter.Params);
            }
        }

        public void SetUserProperty(string propertyName, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_ANALYTICS_ENABLED
            try
            {
                FirebaseAnalytics.SetUserProperty(propertyName, propertyValue);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseAnalyticsProvider] Lỗi SetUserProperty: {ex.Message}");
            }
#else
            AppLogger.Log($"[FirebaseAnalytics:Mock] SetUserProperty: {propertyName} = {propertyValue}");
#endif
        }

        public void SetUserId(string userId)
        {
#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || CORE_FIREBASE_ENABLED || FIREBASE_ANALYTICS_ENABLED
            try
            {
                FirebaseAnalytics.SetUserId(userId);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseAnalyticsProvider] Lỗi SetUserId: {ex.Message}");
            }
#else
            AppLogger.Log($"[FirebaseAnalytics:Mock] SetUserId: {userId}");
#endif
        }

        #region Standard Ads Tracking Events (Tương thích GitLab Service-Firebase)
        public void OnShowAppOpen(string placement)
        {
            Log(new LogEventParameter("show_app_open_ads").AddParam("placement", placement));
        }

        public void OnAppOpenClick(string placement)
        {
            Log(new LogEventParameter("show_app_open_ads_click").AddParam("placement", placement));
        }

        public void OnShowAppOpenSuccess(string placement)
        {
            Log(new LogEventParameter("show_app_open_ads_success").AddParam("placement", placement));
        }

        public void OnShowBanner()
        {
            LogEvent("show_banner_ads");
        }

        public void OnBannerClick()
        {
            LogEvent("show_banner_click");
        }

        public void OnMRecClick(string placement)
        {
            Log(new LogEventParameter("show_mrec_click").AddParam("placement", placement));
        }

        public void OnShowInterstitial(bool hasAds, string placement)
        {
            Log(new LogEventParameter("show_interstitial_ads")
                .AddParam("has_ads", hasAds.ToString())
                .AddParam("placement", placement));
        }

        public void OnInterstitialClick(string placement)
        {
            Log(new LogEventParameter("show_interstitial_ads_click").AddParam("placement", placement));
        }

        public void OnShowInterstitialSuccess(string placement)
        {
            Log(new LogEventParameter("show_interstitial_ads_success").AddParam("placement", placement));
        }

        public void OnInterstitialNumReach(int numReach)
        {
            Log(new LogEventParameter("impdau_inter_passed").AddParam("ImpdauPassed", numReach));
        }

        public void OnShowRewarded(bool hasAds, string placement)
        {
            Log(new LogEventParameter("show_rewarded_ads")
                .AddParam("has_ads", hasAds.ToString())
                .AddParam("placement", placement));
        }

        public void OnRewardedClick(string placement)
        {
            Log(new LogEventParameter("show_rewarded_ads_click").AddParam("placement", placement));
        }

        public void OnShowRewardedSuccess(string placement)
        {
            Log(new LogEventParameter("show_rewarded_ads_success").AddParam("placement", placement));
        }

        public void OnRewardedNumReach(int numReach)
        {
            Log(new LogEventParameter("impdau_rewarded_passed").AddParam("ImpdauPassed", numReach));
        }
        #endregion
    }
}
