using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneBusAway.WP7.ViewModel.DataStructures
{
    public class Route
    {
        public string id { get; set; }
        public string shortName { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public Agency agency { get; set; }
        public Stop closestStop { get; set; }
        public DateTime? nextArrival { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Route)
            {
                Route otherRoute = (Route)obj;
                if (otherRoute.id == this.id)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
