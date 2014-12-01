using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using PommaLabs.KVLite.Annotations;
using PommaLabs.KVLite.Properties;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for cache settings. Contains settings shared among different caches.
    /// </summary>
    public abstract class CacheSettingsBase : INotifyPropertyChanged
    {
        private int _staticIntervalInDays = Settings.Default.DefaultStaticIntervalInDays;

        /// <summary>
        ///   How many days static values will last.
        /// </summary>
        public int StaticIntervalInDays
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return _staticIntervalInDays;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
                _staticIntervalInDays = value;
                OnPropertyChanged("StaticIntervalInDays");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}