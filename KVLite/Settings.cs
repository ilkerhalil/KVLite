namespace PommaLabs.KVLite.Properties
{
    /// <summary>
    ///   Contains all KVLite settings.
    /// </summary>
    public sealed partial class Settings
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        public Settings()
        {
            // To add event handlers for saving and changing settings, uncomment the lines below:
            // 
            // this.SettingChanging += this.SettingChangingEventHandler;
            // 
            // this.SettingsSaving += this.SettingsSavingEventHandler;
        }

        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            // Add code to handle the SettingChangingEvent event here.
        }

        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Add code to handle the SettingsSaving event here.
        }
    }
}