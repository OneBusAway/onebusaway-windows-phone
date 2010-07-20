using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class Agency
    {
        [DataMember()]
        public string id { get; set; }
        [DataMember()]
        public string name { get; set; }

        public override string ToString()
        {
            return string.Format("Agency: name='{0}'", name);
        }
    }
}
