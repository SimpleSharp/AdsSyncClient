using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AdsSync
{
    internal static class Extensions
    {

        /// <summary>
        /// Determines whether the specified object is an ObservableCollection .
        /// </summary>
        /// <param name="o"> The object to check </param>
        /// <returns> Returns TRUE if the object is an ObservableCollection, otherwise FALSE. </returns>
        public static bool IsObservableCollection(this object thisObject)
        {
            return thisObject.GetType() is Type type && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObservableCollection<>);
        }

        /// <summary>
        /// Determines whether the specified object is an ObservableCollection .
        /// </summary>
        /// <param name="o"> Type of the object </param>
        /// <returns> Returns TRUE if the object is an ObservableCollection, otherwise FALSE. </returns>
        public static bool IsObservableCollection(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObservableCollection<>);
        }

        /// <summary>
        /// Determines the byte size of an unmanaged data type.
        /// </summary>
        /// <param name="unmanagedType"> The unmanaged data type </param>
        /// <returns> Returns the byte size as int. Return -1 if the unmanaged type is not supported. </returns>
        public static int GetUnmanagedTypeSize(this UnmanagedType unmanagedType)
        {
            return unmanagedType switch
            {
                UnmanagedType.Bool or UnmanagedType.U1 or UnmanagedType.I1 => 1,
                UnmanagedType.I2 or UnmanagedType.U2 or UnmanagedType.BStr => 2,
                UnmanagedType.I4 or UnmanagedType.U4 or UnmanagedType.R4 => 4,
                UnmanagedType.I8 or UnmanagedType.U8 or UnmanagedType.R8 => 8,
                UnmanagedType.ByValTStr => 81,
                _ => -1
            };
        }

        /// <summary>
        /// Determines the corresponding unmanaged marshalling type for the specified managed type.
        /// </summary>
        /// <param name="type"> The managed type for which the unmanaged marshalling type is determined </param>
        /// <returns> The <see cref="UnmanagedType"/> corresponding to the specified managed type. </returns>
        /// <exception cref="NotSupportedException"> Thrown when the specified managed type is not supported for marshalling. </exception>
        public static UnmanagedType TypeToUnmanagedType(this Type type)
        {
            if (type.IsArray || type.GetInterface(nameof(ICollection)) is not null)
            {
                return UnmanagedType.ByValArray;
            }
            else if (type == typeof(bool) || type == typeof(byte))
            {
                return UnmanagedType.U1;
            }
            else if (type == typeof(short))
            {
                return UnmanagedType.I2;
            }
            else if (type == typeof(int))
            {
                return UnmanagedType.I4;
            }
            else if (type == typeof(long))
            {
                return UnmanagedType.I8;
            }
            else if (type == typeof(ushort))
            {
                return UnmanagedType.U2;
            }
            else if (type == typeof(uint))
            {
                return UnmanagedType.U4;
            }
            else if (type == typeof(ulong))
            {
                return UnmanagedType.U8;
            }
            else if (type == typeof(float))
            {
                return UnmanagedType.R4;
            }
            else if (type == typeof(double))
            {
                return UnmanagedType.R8;
            }
            else if (type == typeof(string))
            {
                return UnmanagedType.ByValTStr;
            }
            else
            {
                throw new NotSupportedException($"The managed type '{type.FullName}' is not supported for marshalling.");
            }
        }

        /// <summary> 
        /// Registers a handler that is invoked when the value of the specified property on the object instance changes.
        /// </summary>
        /// <param name="propertyInfo"> The property for which the change handler should be registered </param>
        /// <param name="objectInstance"> The object instance whose property should be monitored </param>
        /// <param name="handler"> The event handler to invoke when the property value changes </param>
        public static void ExecuteOnPropertyChanged(this PropertyInfo propertyInfo, object objectInstance, EventHandler handler)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(objectInstance);
            PropertyDescriptor property = properties.Find(propertyInfo.Name, false)!;
            property.AddValueChanged(objectInstance, handler);
        }

        /// <summary> Stops the specified handler from being invoked when the value of a property changes. </summary>
        /// <param name="propertyInfo"> The property for which the change handler should be unregistered </param>
        /// <param name="objectInstance"> The object instance that contains the property </param>
        /// <param name="handler"> The event handler to unregister from property change notifications </param>
        public static void NoLongerExecuteOnPropertyChanged(this PropertyInfo propertyInfo, object objectInstance, EventHandler handler)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(objectInstance);
            PropertyDescriptor property = properties.Find(propertyInfo.Name, false)!;
            property.RemoveValueChanged(objectInstance, handler);
        }
    }
}
