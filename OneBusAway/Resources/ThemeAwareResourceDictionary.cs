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
using System.Windows.Markup;
using System.IO;
using System.Windows.Resources;

namespace OneBusAway.WP7.View.Resources
{
    public class ThemeAwareResourceDictionary : ResourceDictionary
    {
        private string _kind;
        public string Kind
        {
            get
            {
                return _kind;
            }
            set
            {
                if (_kind != value)
                {
                    _kind = value;
                    string theme;

                    if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<string>("Theme", out theme) == false)
                    {
                        theme = "OBA";
                    }

                    Uri themeFile = new Uri(string.Format("/OneBusAway.WP7.View;component/Resources/Themes/{0}.xaml",
                       theme), UriKind.RelativeOrAbsolute);

                    using(TextReader r = new StreamReader(Application.GetResourceStream(themeFile).Stream))
                    {
                        string themeXaml = r.ReadToEnd();
                        ResourceDictionary res = (ResourceDictionary)XamlReader.Load(themeXaml);
                        MergedDictionaries.Add(res);
                    }
                }
            }
        }
    }
}
