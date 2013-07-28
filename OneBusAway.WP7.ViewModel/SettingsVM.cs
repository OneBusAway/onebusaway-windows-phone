/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
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

        public bool UseLocation
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains("UseLocation") == true)
                {
                    return bool.Parse(IsolatedStorageSettings.ApplicationSettings["UseLocation"].ToString());
                }
                else
                {
                    // Default to true if no user setting exists
                    return true;
                }
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings["UseLocation"] = value;
                IsolatedStorageSettings.ApplicationSettings.Save();
                OnPropertyChanged("UseLocation");
            }
        }

        public bool UseNativeTheme
        {
            get
            {
                string theme;

                if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<string>("Theme", out theme) == false)
                {
                    return false; // defaults to the OBA theme
                }
                return (theme == "Native");
            }
            set
            {
                if (value)
                {
                    IsolatedStorageSettings.ApplicationSettings["Theme"] = "Native";
                }
                else
                {
                    IsolatedStorageSettings.ApplicationSettings["Theme"] = "OBA";
                }
                IsolatedStorageSettings.ApplicationSettings.Save();
                OnPropertyChanged("UseNativeTheme");
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
