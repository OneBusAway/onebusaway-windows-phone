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
using System.Collections.ObjectModel;

namespace OneBusAway.WP7.ViewModel
{
    public enum MainPagePivots : int
    {
        LastUsed = -100,
        Routes = 0,
        Stops = 1,
        Recents = 2,
        Favorites = 3
    };

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

        public ObservableCollection<MainPagePivots> MainPagePivotOptions
        {
            get
            {
                // Enum.GetValues() isn't supported, so guess I have to hardcode this
                ObservableCollection<MainPagePivots> list = new ObservableCollection<MainPagePivots>()
                {
                    MainPagePivots.LastUsed,
                    MainPagePivots.Routes,
                    MainPagePivots.Stops,
                    MainPagePivots.Favorites,
                    MainPagePivots.Recents
                };

                return list;
            }
        }

        public MainPagePivots SelectedMainPagePivot
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains("DefaultMainPagePivot") == true)
                {
                    return (MainPagePivots)IsolatedStorageSettings.ApplicationSettings["DefaultMainPagePivot"];
                }
                else
                {
                    // Default to LastUsed
                    return MainPagePivots.LastUsed;
                }
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings["DefaultMainPagePivot"] = value;
                IsolatedStorageSettings.ApplicationSettings.Save();
                OnPropertyChanged("SelectedPageOption");
            }
        }

        public void Clear()
        {
            this.appDataModel.DeleteAllFavorites(FavoriteType.Recent);
            this.busServiceModel.ClearCache();
        }
    }
}
