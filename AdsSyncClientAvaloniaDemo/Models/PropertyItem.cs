using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System;
using Avalonia;

namespace AdsSyncClientAvaloniaDemo.Models
{
    /// <summary>
    /// Represents a property of a source object and exposes its current value.
    /// </summary>
    public partial class PropertyItem : ObservableObject
    {
        #region properties
        /// <summary> Gets or sets the current value of the property </summary>
        [ObservableProperty] private object? value;
        /// <summary> The name of the property </summary>
        public string Name => property.Name;
        #endregion

        #region fields
        /// <summary> Source object that contains the property </summary>
        private readonly object source;
        /// <summary> The property descriptor representing the property </summary>
        private readonly PropertyDescriptor property;
        /// <summary> The source object cast as <see cref="INotifyPropertyChanged"/>, used to receive property change notifications </summary>
        private readonly INotifyPropertyChanged? notifySource;
        /// <summary> The collection currently being observed for collection change notifications </summary>
        private INotifyCollectionChanged? observedCollection;
        /// <summary> Synchronization object used to protect access to the observed collection </summary>
        private readonly object collectionLock = new();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyItem"/>
        /// </summary>
        /// <param name="source"> The source object that contains the property </param>
        /// <param name="property"> The property descriptor representing the property </param>
        public PropertyItem(object source, PropertyDescriptor property)
        {
            this.source = source;
            this.property = property;
            notifySource = source as INotifyPropertyChanged;
            if (notifySource is not null && !IsObservableCollection(source))
            {
                notifySource.PropertyChanged += SourcePropertyChanged;
            }
            UpdateValue(true);
        }
        #endregion

        #region private methods
        /// <summary>
        /// Determines whether the specified object is an ObservableCollection .
        /// </summary>
        /// <param name="o"> The object to check </param>
        /// <returns> Returns TRUE if the object is an ObservableCollection, otherwise FALSE. </returns>
        public static bool IsObservableCollection(object thisObject)
        {
            return thisObject.GetType() is Type type && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObservableCollection<>);
        }

        /// <summary>
        /// Updates the property value when the source object changes
        /// </summary>
        private void SourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == property.Name)
            {
                UpdateValue(false);
            }
        }

        /// <summary>
        /// Updates the property value when the observed collection changes
        /// </summary>
        private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateValue(false);

        /// <summary>
        /// Updates the property value when the source value changes
        /// </summary>
        private void SourceValueChanged(object? sender, EventArgs e) => UpdateValue(false);

        /// <summary>
        /// Retrieves the current property value and updates the exposed value. Collections are converted into a comma-separated string.
        /// </summary>
        /// <param name="firstTime"> Indicates whether this is the initial value update </param>
        private void UpdateValue(bool firstTime)
        {
            if (!firstTime)
            {
                UnsubscribeFromCollection();
            }
            object? currentValue = property.GetValue(source);
            object? newValue;
            if (currentValue is IEnumerable collection && currentValue is not string)
            {
                newValue = string.Join(",", collection.Cast<object?>().Select(item => item?.ToString()));
            }
            else
            {
                newValue = currentValue;
            }
            Application.Current.Dispatcher.Invoke(() => Value = newValue);
            SubscribeToCollection(currentValue);
        }

        /// <summary>
        /// Subscribes to collection change notifications if the specified value implements <see cref="INotifyCollectionChanged"/>.
        /// </summary>
        /// <param name="value"> The value that may represent an observable collection </param>
        private void SubscribeToCollection(object? value)
        {
            if (value is not INotifyCollectionChanged collection)
            {
                return;
            }
            lock (collectionLock)
            {
                observedCollection = collection;
                observedCollection.CollectionChanged += CollectionChanged;
            }
        }

        /// <summary>
        /// Unsubscribes from collection change notifications.
        /// </summary>
        private void UnsubscribeFromCollection()
        {
            INotifyCollectionChanged? collection;

            lock (collectionLock)
            {
                collection = observedCollection;
                observedCollection = null;
            }

            if (collection is not null)
            {
                collection.CollectionChanged -= CollectionChanged;
            }
        }
        #endregion
    }
}
