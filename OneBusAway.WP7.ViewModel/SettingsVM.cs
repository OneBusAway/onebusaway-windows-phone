using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;

namespace OneBusAway.WP7.ViewModel
{
    public class SettingsVM : AViewModel
    {
        #region Constructors
        public SettingsVM()
            : base()
        {
        }

        public SettingsVM(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
            : base(busServiceModel, appDataModel)
        {
        }
        
        #endregion

        public bool FeedbackEnabled
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains("FeedbackEnabled") == true)
                {
                    return bool.Parse(IsolatedStorageSettings.ApplicationSettings["FeedbackEnabled"].ToString());
                }
                else
                {
                    // Default to true if no user setting exists
                    return true;
                }
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings["FeedbackEnabled"] = value;
                IsolatedStorageSettings.ApplicationSettings.Save();
                OnPropertyChanged("FeedbackEnabled");
            }
        }

        public void Clear()
        {
            this.appDataModel.DeleteAllFavorites(FavoriteType.Recent);
            this.busServiceModel.ClearCache();
        }
    }
}
