#if UNITY_STANDALONE
using UnityEngine;

namespace IdleFramework.Platform
{
    /// <summary>
    /// Simple Steam platform stub that logs calls to the console.
    /// </summary>
    public sealed class SteamServicesStub : IPlatformServices
    {
        public void UnlockAchievement(string id)
        {
            Debug.Log($"[Steam] UnlockAchievement: {id}");
        }

        public void ReportStat(string id, double value)
        {
            Debug.Log($"[Steam] ReportStat {id}={value}");
        }

        public void ShowAchievements()
        {
            Debug.Log("[Steam] ShowAchievements");
        }

        public void RequestCloudSaveSync()
        {
            Debug.Log("[Steam] CloudSaveSync");
        }

        public void ShowStorePage(string productId)
        {
            Debug.Log($"[Steam] ShowStorePage: {productId}");
        }

        public void ShowAd(string placementId)
        {
            Debug.Log($"[Steam] ShowAd placeholder: {placementId}");
        }
    }
}
#endif
