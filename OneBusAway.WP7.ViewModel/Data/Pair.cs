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

namespace OneBusAway.WP7.ViewModel.Data
{
    public class Pair<K, V> : IComparable<Pair<K,V>> where  K : IComparable
    {
       public Pair(K key1, V value1) 
       {
          this.key = key1;
          this.value = value1;
       }

       public K key;
       public V value;

       public override bool Equals(Object obj) 
       {
          if(obj is Pair<K,V>) 
          {
             Pair<K,V> p = (Pair<K,V>)obj;
             return key.Equals(p.key)&&value.Equals(p.value);
          }
          return false;
       }

       public int CompareTo(Pair<K,V> p) 
       {
          int v = key.CompareTo(p.key);
          if(v==0) 
          {
             if(p.value is IComparable) 
             {
                return ((IComparable)value).CompareTo(p.value);
             }
          }
          return v;
       }
   
       public override int GetHashCode() 
       {
           return key.GetHashCode() ^ value.GetHashCode();
       }
   
       public override String ToString() 
       {
          return key+": "+value;
       }
    }
}
