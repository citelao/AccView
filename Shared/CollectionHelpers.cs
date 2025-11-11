using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class CollectionHelpers
    {
        /// <summary>
        /// Update an observable collection to match a new list of values, using the provided equality function.
        ///
        /// Useful if the observable collection is bound to a UI element, and you must modify it on the UI thread.
        /// </summary>
        /// <typeparam name="T">Type to compare</typeparam>
        /// <param name="observableCollection">Observable collection to modify</param>
        /// <param name="newValues">New values to insert</param>
        /// <param name="equalityFunc">Comparison function</param>
        public void UpdateObservableCollection<T>(ObservableCollection<T> observableCollection, IList<T> newValues, Func<T, T, bool> equalityFunc)
            where T : class
        {
            for (var i = 0; i < newValues.Count; i++)
            {
                var newChild = newValues[i];
                var currentChild = (i < observableCollection.Count) ? observableCollection[i] : null;
                if (currentChild != null && equalityFunc(currentChild, newChild))
                {
                    // Same element, do nothing.
                    continue;
                }
                else
                {
                    // Different element, insert the new one here.
                    observableCollection.Insert(i, newChild);
                }
            }

            // Remove any extra children.
            while (observableCollection.Count > newValues.Count)
            {
                observableCollection.RemoveAt(observableCollection.Count - 1);
            }
        }
    }
}
