using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace AdsSync
{
    /// <summary>
    /// Klasse, die Methoden für das Mashalling bereitstellt
    /// </summary>
    public static class Marshallingg
    {
        #region public methods
        /// <summary>
        /// Wandelt eine übliche Klasse mit Eigenschaften zu einer gemarshallten Klasse mit Feldern um.
        /// Es kann eine Klasse mit allen normalen Datentypen sowie ObservableCollections als Alternative zu Arrays umgewandelt werden.
        /// Zudem ist es möglich, direkt anstelle einer Klasse eine ObservableCollection anzugeben (bspw. für ein Rezept). 
        /// </summary>
        /// <param name="originalClass"> Die Originalklasse </param>
        public static object PropertyClassToMarshalledFieldClass(object originalClass)
        {
            //Wenn das Objekt eine ObservableCollection ist wird eine andere Funktion ausgeführt
            if (IsTypeOfObservableCollection(originalClass))
            {
                return ObservableCollectionAsMarshalledFieldClass((IList)originalClass);
            }
            //Klassenbuilder erstellen
            TypeBuilder typeBuilder = CreateTypeBuilder();
            //Anhand der Eigenschaften der Ursprungsklasse Felder der neuen Klasse hinzufügen
            PropertyInfo[] propertyInfos = originalClass.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            propertyInfos = [.. propertyInfos.OrderBy(p => p.MetadataToken)];
            int fieldOffset = new();
            for (int i1 = propertyInfos.GetLowerBound(0); i1 <= propertyInfos.GetUpperBound(0); i1++)
            {
                object propertyValue = propertyInfos[i1].GetValue(originalClass)!;
                FieldBuilder fieldBuilder = typeBuilder.DefineField(propertyInfos[i1].Name, GetTypeByObject(propertyValue), FieldAttributes.Public);
                //Definiert die vorzugebenden Parameter, je nach Typ
                FieldInfo[] namedFields = GetArgumentsByObject(propertyValue);
                //Definiert die Typen und Länge, je nach Typ
                object[] fieldValues = GetArgumentValuesByObject(propertyValue);
                //Marshal-Attribut erstellen
                CustomAttributeBuilder customBuilder = new(
                    typeof(MarshalAsAttribute).GetConstructor([typeof(UnmanagedType)])!,
                    [TypeToUnmanagedType(fieldBuilder.FieldType)],
                    namedFields,
                    fieldValues);
                //Marshal-Attribut setzen                           
                fieldBuilder.SetCustomAttribute(customBuilder);
                fieldBuilder.SetOffset(fieldOffset);
                fieldOffset++;
            }
            //Objekt anlegen
            Type classType = typeBuilder.CreateType()!;
            return Activator.CreateInstance(classType)!;
        }

        /// <summary>
        /// Übernimmt die Werte der gemarshallten Klasse in die Originalklasse
        /// </summary>
        /// <param name="fieldClass"> Die gemarshallte Klasse </param>
        /// <param name="baseClass"> Die Originalklasse </param>
        public static void GetValuesOfMarshalledFieldClass(object fieldClass, object baseClass)
        {
            foreach (PropertyInfo propertyInfo in baseClass.GetType().GetProperties())
            {
                object oldValue = propertyInfo.GetValue(baseClass)!;
                object newValue = fieldClass.GetType().GetField(propertyInfo.Name)!.GetValue(fieldClass)!;
                if (IsTypeOfObservableCollection(oldValue))
                {
                    IList? collection = oldValue as IList;
                    Array? array = newValue as Array;
                    for (int i1 = array!.GetLowerBound(0); i1 <= array!.GetUpperBound(0); i1++)
                    {
                        collection![i1] = array.GetValue(i1);
                    }

                    propertyInfo.SetValue(baseClass, collection);
                }
                else
                {
                    propertyInfo.SetValue(baseClass, newValue);
                }
            }
        }

        /// <summary>
        /// Übernimmt die Werte der Originalklasse in die gemarshallte Klasse
        /// </summary>
        /// <param name="fieldClass"> Die gemarshallte Klasse </param>
        /// <param name="baseClass"> Die Originalklasse </param>
        public static void SetValuesOfMarshalledFieldClass(object fieldClass, object baseClass)
        {
            //Wenn das Objekt eine ObservableCollection ist wird eine andere Funktion ausgeführt
            if (IsTypeOfObservableCollection(baseClass))
            {
                SetValuesOfObservableCollectionAsFieldClass((IList)fieldClass, (IList)baseClass);
                return;
            }
            //Werte zuweisen
            foreach (FieldInfo fieldInfo in fieldClass.GetType().GetFields())
            {
                object valueProperty = baseClass.GetType().GetProperty(fieldInfo.Name)!.GetValue(baseClass)!;
                if (IsTypeOfObservableCollection(valueProperty))
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
        /// Übernimmt die Werte der Originalklasse in die gemarshallte ObservableCollection-Klasse
        /// </summary>
        /// <param name="fieldClass"> Die gemarshallte Klasse </param>
        /// <param name="baseClass"> Die Originalklasse </param>
        private static void SetValuesOfObservableCollectionAsFieldClass(IList fieldClass, IList baseClass)
        {
            PropertyInfo[] propertyInfosGet = baseClass[0]!.GetType().GetProperties();
            for (int i1 = 0; i1 < baseClass.Count; i1++)
            {
                for (int i2 = 0; i2 < propertyInfosGet.Length; i2++)
                {
                    FieldInfo firstFi =
                        (from fi in fieldClass[i1]!.GetType().GetFields()
                         where fi.Name.Equals(propertyInfosGet[i2].Name)
                         select fi).First();
                    firstFi.SetValue(fieldClass[i1], propertyInfosGet[i2].GetValue(baseClass[i1]));
                }
            }
        }

        /// <summary>
        /// Wandelt eine ObservableCollection zu einer gemarshallten Array-Klasse mit Feldern um
        /// </summary>
        private static object ObservableCollectionAsMarshalledFieldClass(IList observableCollection)
        {
            //Klassenbuilder erstellen
            TypeBuilder typeBuilder = CreateTypeBuilder();
            //Anhand eines Elements der ObservableCollection wird der Typ und damit die Eigenschaften ermittelt
            PropertyInfo[] propertyInfos = observableCollection[0]!.GetType().GetProperties();
            int fieldOffset = new();
            for (int i1 = propertyInfos.GetLowerBound(0); i1 <= propertyInfos.GetUpperBound(0); i1++)
            {
                object propertyValue = propertyInfos[i1].GetValue(observableCollection[0])!;
                FieldBuilder fieldBuilder = typeBuilder.DefineField(propertyInfos[i1].Name, GetTypeByObject(propertyValue), FieldAttributes.Public);
                //Definiert die vorzugebenden Parameter, je nach Typ
                FieldInfo[] namedFields = GetArgumentsByObject(propertyValue);
                //Definiert die Typen und Länge, je nach Typ
                object[] fieldValues = GetArgumentValuesByObject(propertyValue);
                //Marshal-Attribut erstellen
                CustomAttributeBuilder customBuilder = new(
                    typeof(MarshalAsAttribute).GetConstructor([typeof(UnmanagedType)])!,
                    [TypeToUnmanagedType(fieldBuilder.FieldType)],
                    namedFields,
                    fieldValues);
                //Marshal-Attribut setzen                           
                fieldBuilder.SetCustomAttribute(customBuilder);
                fieldBuilder.SetOffset(fieldOffset);
                fieldOffset++;
            }
            //Objekt anlegen
            Type classType = typeBuilder.CreateType()!;
            IList array = Array.CreateInstance(classType, observableCollection.Count);
            for (int i1 = 0; i1 < array.Count; i1++)
            {
                array[i1] = Activator.CreateInstance(classType);
            }
            return array;
        }

        /// <summary>
        /// Erstellt einen neuen Typenersteller
        /// </summary>
        /// <returns> Gibt den neuen Typenersteller zurück. </returns>
        private static TypeBuilder CreateTypeBuilder()
        {
            //Name der dynamischen Assembly
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
        /// Gibt anhand des Objekts (egal ob eine Sammlung oder nicht) den korrekten Typen aus
        /// </summary>
        /// <param name="value"> Das Objekt, dessen Typ ermittelt werden soll </param>
        /// <returns> Gibt den Typen zurück. </returns>
        private static Type GetTypeByObject(object value)
        {
            if (IsTypeOfObservableCollection(value))
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
        /// Gibt anhand des Objekt Argumente für die Erstellung eines Felds aus
        /// </summary>
        /// <param name="value"> Der Wert, von dem die Argumente ermittelt werden sollen </param>
        /// <returns> Gibt die Argumente zurück. </returns>
        private static FieldInfo[] GetArgumentsByObject(object value)
        {
            FieldInfo[] namedFields;
            if (value.GetType().IsArray || IsTypeOfObservableCollection(value))
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
        /// Gibt anhand des Objekt Werte für die Argumente für die Erstellung eines Felds aus
        /// </summary>
        /// <param name="value"> Der Wert, von dem die Argumente ermittelt werden sollen </param>
        public static object[] GetArgumentValuesByObject(object value)
        {
            if (value is string)
            {
                //Bei einem String wird ein Objekt mit der Zahl 81 übergeben für 80 Zeichen + 1 für das Byte, dass die Länge des Strings angibt
                return [81];
            }
            else if (IsTypeOfObservableCollection(value))
            {
                //Bei einer ObservableCollection wird die Länge des Arrays und der Typ übergeben
                ICollection collection = (ICollection)value;
                Array array = Array.CreateInstance(collection.GetType().GetGenericArguments().Single(), collection.Count);
                return [array.Length, TypeToUnmanagedType(array.GetValue(0)!.GetType())];
            }
            else if (value.GetType().IsArray)
            {
                //Bei einem Array wird ebenso die Länge des Arrays und der Typ übergeben
                return [((Array)value).Length, TypeToUnmanagedType(((Array)value).GetValue(0)!.GetType())];
            }
            else
            {
                //Alternativ wird eine 0 übergeben
                return [0];
            }
        }

        /// <summary>
        /// Gibt ob, ob es sich bei einem Objekt um eine ObservableCollection handelt
        /// </summary>
        /// <param name="thisObject"> Das Objekt, dass kontrolliert werden soll </param>
        /// <returns> Gibt TRUE zurück, wenn es sich um eine ObservableCollection handelt </returns>
        private static bool IsTypeOfObservableCollection(object thisObject)
        {
            return thisObject.GetType() is Type type && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObservableCollection<>);
        }
        /// <summary>
        /// Gibt die Größe eines UnmanagedType in Byte zurück
        /// </summary>
        /// <param name="unmanagedType"> Der Marshal-Typ </param>
        /// <returns> Größe in Byte, oder -1 wenn die Größe vom Kontext abhängt </returns>
        public static int UnmanagedTypeSize(UnmanagedType unmanagedType)
        {
            return unmanagedType switch
            {
                // Einzelne Bytes
                UnmanagedType.Bool or UnmanagedType.U1 or UnmanagedType.I1 => 1,

                // 2-Byte Typen
                UnmanagedType.I2 or UnmanagedType.U2 or UnmanagedType.BStr => 2,

                // 4-Byte Typen
                UnmanagedType.I4 or UnmanagedType.U4 or UnmanagedType.R4 => 4,

                // 8-Byte Typen
                UnmanagedType.I8 or UnmanagedType.U8 or UnmanagedType.R8 => 8,

                // Pointer (größe hängt von Architektur ab)
                UnmanagedType.SysInt or UnmanagedType.SysUInt => IntPtr.Size,
                UnmanagedType.LPStr or UnmanagedType.LPWStr or UnmanagedType.LPTStr => IntPtr.Size,
                UnmanagedType.ByValTStr => -1, // Variabel, definiert durch SizeConst

                // Arrays (größenabhängig)
                UnmanagedType.ByValArray => -1, // Variabel, definiert durch SizeConst * Elementtyp
                UnmanagedType.LPArray => IntPtr.Size, // Pointer zum Array

                // Structures
                UnmanagedType.Struct or UnmanagedType.Interface => -1, // Größte Komponente

                // Sonstige
                _ => -1 // Unbekannt oder kontextabhängig
            };
        }

        public static Type CreateExpandedArrayType(Type elementType, int elementCount)
        {
            ArgumentNullException.ThrowIfNull(elementType);

            if (elementCount < 0)
                throw new ArgumentOutOfRangeException(nameof(elementCount));

            var typeName = $"{elementType.Name}Array{elementCount}";
            var assemblyName = new AssemblyName($"DynamicStructs_{Guid.NewGuid():N}");

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            var typeBuilder = moduleBuilder.DefineType(
                typeName,
                TypeAttributes.Public |
                TypeAttributes.SequentialLayout |
                TypeAttributes.Sealed,
                typeof(ValueType));

            var fieldBuilder = typeBuilder.DefineField(
                "Values",
                elementType.MakeArrayType(),
                FieldAttributes.Public);

            var marshalAsConstructor =
                typeof(MarshalAsAttribute).GetConstructor(
                    new[] { typeof(UnmanagedType) });

            var customAttribute = new CustomAttributeBuilder(
                marshalAsConstructor!,
                new object[] { UnmanagedType.ByValArray },
                new[]
                {
            typeof(MarshalAsAttribute).GetField(
                nameof(MarshalAsAttribute.SizeConst))!,

            typeof(MarshalAsAttribute).GetField(
                nameof(MarshalAsAttribute.ArraySubType))!
                },
                new object[]
                {
            elementCount,
            TypeToUnmanagedType(elementType)
                });

            fieldBuilder.SetCustomAttribute(customAttribute);

            return typeBuilder.CreateTypeInfo()!.AsType();
        }

        /// <summary>
        /// Definiert anhand des Datentypen den Marshal-Typen
        /// </summary>
        /// <param name="type"> Der Datentyp des Objekts </param>
        /// <returns> Gibt den Marshal-Typen zurück. </returns>
        public static UnmanagedType TypeToUnmanagedType(Type type)
        {
            if (type.IsArray || type.GetInterface(nameof(ICollection)) != null)
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
                return UnmanagedType.U8;
            }
        }
        #endregion
    }
}
