using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Core.Logging;
using Unity.Core.Services.Ads;
using Unity.Core.Services.Tracking;

#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || WASD_FIREBASE_ENABLED || FIREBASE_ANALYTICS_ENABLED
using Firebase.Analytics;
#endif

namespace Unity.Firebase
{
    /// <summary>
    /// Adapter theo dõi doanh thu quảng cáo (Impression-level Ad Revenue) và phân bổ cho Firebase Analytics.
    /// Tự động bắt sự kiện doanh thu từ Ads Service (AppLovin MAX, AdMob) và bắn sự kiện chuẩn 'ad_impression'.
    /// </summary>
    public class FirebaseTrackingProvider : ITrackingProvider
    {
        public string ProviderName => "FirebaseTracking";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            TrackingService.AddProvider(new FirebaseTrackingProvider());
        }

        public void Initialize()
        {
            AppLogger.Log("[FirebaseTrackingProvider] Khởi tạo Firebase Tracking Adapter...");
        }

        public void Shutdown()
        {
            AppLogger.Log("[FirebaseTrackingProvider] Đóng Firebase Tracking Adapter.");
        }

        public void TrackRevenue(AdRevenueInfo revenueInfo)
        {
            if (revenueInfo == null) return;

#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || WASD_FIREBASE_ENABLED || FIREBASE_ANALYTICS_ENABLED
            try
            {
                Parameter[] adParameters = {
                    new Parameter("ad_platform", string.IsNullOrEmpty(revenueInfo.Source) ? "Applovin" : revenueInfo.Source),
                    new Parameter("ad_source", string.IsNullOrEmpty(revenueInfo.NetworkName) ? "" : revenueInfo.NetworkName),
                    new Parameter("ad_unit_name", string.IsNullOrEmpty(revenueInfo.AdUnitId) ? "" : revenueInfo.AdUnitId),
                    new Parameter("currency", string.IsNullOrEmpty(revenueInfo.Currency) ? "USD" : revenueInfo.Currency),
                    new Parameter("value", revenueInfo.Revenue),
                    new Parameter("placement", string.IsNullOrEmpty(revenueInfo.Placement) ? "" : revenueInfo.Placement),
                    new Parameter("country_code", string.IsNullOrEmpty(revenueInfo.CountryCode) ? "" : revenueInfo.CountryCode),
                    new Parameter("ad_format", string.IsNullOrEmpty(revenueInfo.Format) ? "" : revenueInfo.Format)
                };

                FirebaseAnalytics.LogEvent("ad_impression", adParameters);
                AppLogger.Log($"[FirebaseTracking] Tracked ad_impression: {revenueInfo.Revenue} {revenueInfo.Currency} ({revenueInfo.Format})");
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseTrackingProvider] Lỗi gửi ad_impression: {ex.Message}");
            }
#else
            AppLogger.Log($"[FirebaseTracking:Mock] TrackRevenue: {revenueInfo.Revenue} {revenueInfo.Currency} | Format: {revenueInfo.Format} | Network: {revenueInfo.NetworkName}");
#endif
        }

        public void TrackEvent(string eventToken, double? revenue = null, string currency = null)
        {
            if (string.IsNullOrEmpty(eventToken)) return;

            var dict = new Dictionary<string, object>();
            if (revenue.HasValue) dict["revenue"] = revenue.Value;
            if (!string.IsNullOrEmpty(currency)) dict["currency"] = currency;

#if FIREBASE_AVAILABLE || UNITY_FIREBASE_ENABLED || WASD_FIREBASE_ENABLED || FIREBASE_ANALYTICS_ENABLED
            try
            {
                if (dict.Count == 0)
                {
                    FirebaseAnalytics.LogEvent(eventToken);
                }
                else
                {
                    var paramList = new List<Parameter>();
                    if (revenue.HasValue) paramList.Add(new Parameter("value", revenue.Value));
                    if (!string.IsNullOrEmpty(currency)) paramList.Add(new Parameter("currency", currency));
                    FirebaseAnalytics.LogEvent(eventToken, paramList.ToArray());
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[FirebaseTrackingProvider] Lỗi TrackEvent: {ex.Message}");
            }
#else
            AppLogger.Log($"[FirebaseTracking:Mock] TrackEvent: {eventToken} (Revenue: {revenue} {currency})");
#endif
        }
    }
}
