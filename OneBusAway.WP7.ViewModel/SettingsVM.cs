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

        public void Clear()
        {
            this.appDataModel.DeleteAllFavorites(FavoriteType.Recent);
            this.busServiceModel.ClearCache();
        }
    }
}
