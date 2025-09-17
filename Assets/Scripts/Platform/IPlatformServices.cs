namespace IdleFramework.Platform
{
    /// <summary>
    /// Abstract platform integrations used by the template.
    /// </summary>
    public interface IPlatformServices
    {
        /// <summary>
        /// Unlocks an achievement by identifier.
        /// </summary>
        void UnlockAchievement(string id);

        /// <summary>
        /// Reports a numeric stat.
        /// </summary>
        void ReportStat(string id, double value);

        /// <summary>
        /// Displays the platform specific achievement UI.
        /// </summary>
        void ShowAchievements();

        /// <summary>
        /// Triggers a cloud save upload/download.
        /// </summary>
        void RequestCloudSaveSync();

        /// <summary>
        /// Opens an in-app purchase store page.
        /// </summary>
        void ShowStorePage(string productId);

        /// <summary>
        /// Shows an advertisement if supported.
        /// </summary>
        void ShowAd(string placementId);
    }
}
