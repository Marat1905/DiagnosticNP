using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DiagnosticNP.Models
{
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                Items.Add(item);
            }

            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        public void RemoveRange(IEnumerable<T> collection)
        {
            foreach (var item in collection.ToList())
            {
                Items.Remove(item);
            }

            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        public void ReplaceRange(IEnumerable<T> collection)
        {
            Items.Clear();
            AddRange(collection);
        }
    }
}