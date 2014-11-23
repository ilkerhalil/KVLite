using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using PommaLabs.KVLite.Annotations;
using PommaLabs.KVLite.Properties;

namespace PommaLabs.KVLite.Core
{
    public abstract class CacheSettingsBase : INotifyPropertyChanged
    {
        private int _staticIntervalInDays = Settings.Default.DefaultStaticIntervalInDays;

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