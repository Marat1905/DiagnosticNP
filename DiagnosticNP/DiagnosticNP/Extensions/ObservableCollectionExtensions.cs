﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DiagnosticNP.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            if (items == null) return;

            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
}