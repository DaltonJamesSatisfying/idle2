#if UNITY_ANDROID || UNITY_IOS
using UnityEngine;

namespace IdleFramework.Platform
{
    /// <summary>
    /// Mobile platform stub logging all calls.
    /// </summary>
    public sealed class MobileServicesStub : IPlatformServices
    {
        public void UnlockAchievement(string id)
        {
            Debug.Log($"[Mobile] UnlockAchievement: {id}");
        }

        public void ReportStat(string id, double value)
        {
            Debug.Log($"[Mobile] ReportStat {id}={value}");
        }

        public void ShowAchievements()
        {
            Debug.Log("[Mobile] ShowAchievements");
        }

        public void RequestCloudSaveSync()
        {
            Debug.Log("[Mobile] Cloud save sync requested");
        }

        public void ShowStorePage(string productId)
        {
            Debug.Log($"[Mobile] ShowStorePage: {productId}");
        }

        public void ShowAd(string placementId)
        {
            Debug.Log($"[Mobile] ShowAd placeholder: {placementId}");
        }
    }
}
#endif
