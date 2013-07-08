using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns the first elements value as type T.
        /// </summary>
        public static T GetFirstElementValue<T>(this XElement element, string childNodeName)
        {
            var childNode = element.Descendants(childNodeName).FirstOrDefault();

            if (childNode == null)
            {
                return default(T);
            }

            try
            {
                return (T)Convert.ChangeType(childNode.Value.Trim(), typeof(T), CultureInfo.CurrentCulture);
            }
            catch
            {
                return default(T);
            }
        }
    }
}
