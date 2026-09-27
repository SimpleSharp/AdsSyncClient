using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace AdsSync.Mapping
{
    /// <summary>
    /// Klasse, die Methoden für das Mashalling bereitstellt
    /// </summary>
    internal static class Marshalling
    {
        #region constants
        /// <summary> Defines the default string length in TwinCAT </summary>
        private const int DefaultTwinCatStringByteLength = 80;
        /// <summary> Defines the byte for the count of chars in a string </summary>
        private const int StringLengthByte = 1;
        #endregion

        #region public methods
        /// <summary>
        /// Provides helper methods for converting .NET objects into dynamically generated classes with fields suitable 
        /// for interop marshalling.
        /// </summary>
        /// <remarks>
        /// This class creates runtime types based on the public properties of a managed object. Each property is 
        /// converted into a public field and assigned a corresponding <see cref="MarshalAsAttribute"/>.
        ///
        /// Primitive types, strings and <see cref="ObservableCollection{T}"/> instances are supported. Arrays 
        /// and observable collections are represented as fixed-size inline arrays.
        ///
        /// The generated types use sequential layout with a one-byte packing size and can  be used for structured data 
        /// exchange with an ADS client.
        /// </remarks>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="originalClass"/> is <see langword="null"/>. </exception>
        public static object PropertyClassToMarshalledFieldClass(object originalClass)
        {
            ArgumentNullException.ThrowIfNull(originalClass);
            //Create the type builder used to generate the runtime class
            TypeBuilder typeBuilder = CreateTypeBuilder();
            //Retrieve and order the source properties before creating the corresponding fields
            PropertyInfo[] propertyInfos = originalClass.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            propertyInfos = [.. propertyInfos.OrderBy(p => p.MetadataToken)];
            int fieldOffset = new();
            for (int i1 = propertyInfos.GetLowerBound(0); i1 <= propertyInfos.GetUpperBound(0); i1++)
            {
                object propertyValue = propertyInfos[i1].GetValue(originalClass)!;
                //Create a public field with the same name and the corresponding field type
                FieldBuilder fieldBuilder = typeBuilder.DefineField(propertyInfos[i1].Name, GetTypeByObject(propertyValue), FieldAttributes.Public);
                //Determine the MarshalAsAttribute constructor arguments required for the field type
                FieldInfo[] namedFields = GetArgumentsByObject(propertyValue);
                //Determine the values of the MarshalAsAttribute arguments required for the field type
                object[] fieldValues = GetArgumentValuesByObject(propertyValue);
                //Create the MarshalAsAttribute for the generated field
                CustomAttributeBuilder customBuilder = new(
                    typeof(MarshalAsAttribute).GetConstructor([typeof(UnmanagedType)])!,
                    [fieldBuilder.FieldType.TypeToUnmanagedType()],
                    namedFields,
                    fieldValues);
                //Apply the marshalling attribute and set the field offset                          
                fieldBuilder.SetCustomAttribute(customBuilder);
                fieldBuilder.SetOffset(fieldOffset);
                fieldOffset++;
            }
            //Apply the marshalling attribute and set the field offset
            Type classType = typeBuilder.CreateType();
            return Activator.CreateInstance(classType)!;
        }

        /// <summary>
        /// Assigns the values from a source object to the fields of a marshalled field object. Observable 
        /// collections are converted to arrays before their values are assigned.
        /// </summary>
        /// <param name="fieldClass"> The object whose fields receive the values </param>
        /// <param name="propertyClass"> The source object from which the values are read </param>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="fieldClass"/> or <paramref name="propertyClass"/>
        /// is <see langword="null"/>. </exception>
        public static void UpdateFieldClassValuesFromProperties(object fieldClass, object propertyClass)
        {
            ArgumentNullException.ThrowIfNull(fieldClass);
            ArgumentNullException.ThrowIfNull(propertyClass);
            foreach (FieldInfo fieldInfo in fieldClass.GetType().GetFields())
            {
                object valueProperty = propertyClass.GetType().GetProperty(fieldInfo.Name)!.GetValue(propertyClass)!;
                if (valueProperty.IsObservableCollection())
                {
                    ICollection collection = (ICollection)valueProperty;
                    Array array = Array.CreateInstance(collection.GetType().GetGenericArguments().Single(), collection.Count);
                    collection.CopyTo(array, 0);
                    fieldInfo.SetValue(fieldClass, array);
                }
                else
                {
                    fieldInfo.SetValue(fieldClass, valueProperty);
                }
            }
        }

        #endregion

        #region private & protected methods
        /// <summary>
        /// Creates a new typebuilder.
        /// </summary>
        /// <returns> Returns the typebuilder. </returns>
        private static TypeBuilder CreateTypeBuilder()
        {
            var assemblyName = new AssemblyName("MarshalAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
            TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicClass",
                                                               TypeAttributes.Public | TypeAttributes.SequentialLayout,
                                                               typeof(object),
                                                               PackingSize.Size1);
            return typeBuilder;
        }

        /// <summary>
        /// Gets the type of an object.
        /// </summary>
        /// <param name="value"> The value whose type is to be determined </param>
        /// <returns> Returns the array type if its an ObservableCollection; otherwise it will return "GetType()". </returns>
        private static Type GetTypeByObject(object value)
        {
            if (value.IsObservableCollection())
            {
                ICollection collection = (ICollection)value;
                Array array = Array.CreateInstance(collection.GetType().GetGenericArguments().Single(), collection.Count);
                return array.GetType();
            }
            else
            {
                return value.GetType();
            }
        }

        /// <summary>
        /// Returns the MarshalAsAttribute fields required for the specified value type.
        /// </summary>
        /// <param name="value"> The value for which the arguments are to be determined </param>
        /// <returns> Returns the arguments. </returns>
        private static FieldInfo[] GetArgumentsByObject(object value)
        {
            FieldInfo[] namedFields;
            if (value.GetType().IsArray || value.IsObservableCollection())
            {
                namedFields = [typeof(MarshalAsAttribute).GetField("SizeConst")!, typeof(MarshalAsAttribute).GetField("ArraySubType")!];
            }
            else
            {
                namedFields = [typeof(MarshalAsAttribute).GetField("SizeConst")!];
            }
            return namedFields;
        }

        /// <summary>
        /// Determines the values required for the marshalling arguments based on the type and value of the specified object
        /// </summary>
        /// <param name="value"> The value used to determine the required marshalling arguments </param>
        /// /// <returns> An array containing the values required to configure the field's marshalling attributes. </returns>
        private static object[] GetArgumentValuesByObject(object value)
        {
            if (value is string)
            {
                return [DefaultTwinCatStringByteLength + StringLengthByte];
            }
            else if (value.IsObservableCollection())
            {
                ICollection collection = (ICollection)value;
                Array array = Array.CreateInstance(collection.GetType().GetGenericArguments().Single(), collection.Count);
                return [array.Length, array.GetType().GetElementType()!.TypeToUnmanagedType()];
            }
            else if (value.GetType().IsArray)
            {
                Array array = (Array)value;
                return [array.Length, array.GetType().GetElementType()!.TypeToUnmanagedType()];
            }
            else
            {
                return [0];
            }
        }

        /// <summary>
        /// Creates a value type containing a fixed-size array field of the specified element type. The array field is 
        /// decorated with <see cref="MarshalAsAttribute"/> using <see cref="UnmanagedType.ByValArray"/> for interop marshaling.
        /// </summary>
        /// <remarks>
        /// The creation of this class is necessary because common array types 
        /// </remarks>
        /// <param name="elementType"> The type of the elements in the array </param>
        /// <param name="elementCount"> The number of elements in the fixed-size array (must be zero or greater) </param>
        /// <returns> A dynamically generated value type containing a public fixed-size array field named <c>Values</c>. </returns>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="elementType"/> is <see langword="null"/>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when <paramref name="elementCount"/> is negative. </exception>
        public static Type CreateExpandedArrayType(Type elementType, int elementCount)
        {
            ArgumentNullException.ThrowIfNull(elementType);
            ArgumentOutOfRangeException.ThrowIfNegative(elementCount);
            TypeBuilder typeBuilder = CreateTypeBuilder();
            FieldBuilder fieldBuilder = typeBuilder.DefineField("Values", elementType.MakeArrayType(), FieldAttributes.Public);
            ConstructorInfo marshalAsConstructor = typeof(MarshalAsAttribute).GetConstructor([typeof(UnmanagedType)])!;
            CustomAttributeBuilder customAttribute = new(
                marshalAsConstructor,
                [UnmanagedType.ByValArray],
                [typeof(MarshalAsAttribute).GetField(nameof(MarshalAsAttribute.SizeConst))!,
                 typeof(MarshalAsAttribute).GetField(nameof(MarshalAsAttribute.ArraySubType))!],
                 [elementCount, elementType.TypeToUnmanagedType()]);
            fieldBuilder.SetCustomAttribute(customAttribute);
            return typeBuilder.CreateTypeInfo().AsType();
        }
        #endregion
    }
}
