using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xb.Type
{
    public class Reflection
    {
        #region "static"

        private static ConcurrentDictionary<string, Xb.Type.Reflection> _cache
            = new ConcurrentDictionary<string, Reflection>();

        public static Xb.Type.Reflection Get(string typeFullName)
        {
            var exists = Reflection._cache.FirstOrDefault(p => p.Key == typeFullName);
            if (exists.Value != null)
                return exists.Value;

            try
            {
                var newRef = new Xb.Type.Reflection(System.Type.GetType(typeFullName));
                Reflection._cache.GetOrAdd(typeFullName, newRef);

                return newRef;
            }
            catch (Exception)
            {
                throw new Exception($"Type not found: {typeFullName}");
            }
        }

        public static Xb.Type.Reflection Get(System.Type type)
        {
            return Reflection.Get(type.FullName);
        }

        #endregion

        public class Property
        {
            public static Property Get(PropertyInfo info)
            {
                return new Property(info);
            }


            /// <summary>
            /// Property IO operator
            /// </summary>
            /// <remarks>
            /// 参考：ほぼコピペ。
            /// http://d.hatena.ne.jp/machi_pon/20090821/1250813986
            /// </remarks>
            internal interface IAccessor
            {
                bool HasGetter { get; }

                bool HasSetter { get; }

                /// <summary>
                /// Get property value
                /// </summary>
                /// <param name="instance"></param>
                /// <returns></returns>
                object GetValue(object instance);

                /// <summary>
                /// Set property value
                /// </summary>
                /// <param name="instance"></param>
                /// <param name="value"></param>
                void SetValue(object instance, object value);
            }

            /// <summary>
            /// Property IO operator
            /// </summary>
            /// <typeparam name="TTarget"></typeparam>
            /// <typeparam name="TProperty"></typeparam>
            /// <remarks>
            /// 参考：ほぼコピペ。
            /// http://d.hatena.ne.jp/machi_pon/20090821/1250813986
            /// </remarks>
            internal sealed class Accessor<TTarget, TProperty> : IAccessor
            {
                private readonly Func<TTarget, TProperty> _getter;
                private readonly Action<TTarget, TProperty> _setter;

                public bool HasGetter { get; private set; }
                public bool HasSetter { get; private set; }

                /// <summary>
                /// Constructor
                /// </summary>
                /// <param name="getter"></param>
                /// <param name="setter"></param>
                public Accessor(Func<TTarget, TProperty> getter, Action<TTarget, TProperty> setter)
                {
                    this._getter = getter;
                    this._setter = setter;

                    this.HasGetter = (this._getter != null);
                    this.HasSetter = (this._setter != null);
                }

                /// <summary>
                /// Get property value
                /// </summary>
                /// <param name="instance"></param>
                /// <returns></returns>
                public object GetValue(object instance)
                {
                    return this._getter((TTarget)instance);
                }

                /// <summary>
                /// Set property value
                /// </summary>
                /// <param name="instance"></param>
                /// <param name="value"></param>
                public void SetValue(object instance, object value)
                {
                    this._setter((TTarget)instance, (TProperty)value);
                }
            }

            /// <summary>
            /// PropertyInfo
            /// </summary>
            public PropertyInfo Info { get; private set; }

            /// <summary>
            /// Property Type
            /// </summary>
            public System.Type Type { get; private set; }

            /// <summary>
            /// Base-Type of nullable type (same "Type" value if non-nullable)
            /// </summary>
            public System.Type UnderlyingType { get; private set; }

            /// <summary>
            /// Enable to set value or not
            /// </summary>
            public bool IsSettable { get; private set; }

            /// <summary>
            /// Enable to get value or not
            /// </summary>
            public bool IsGettable { get; private set; }

            /// <summary>
            /// Is nullable value or not
            /// </summary>
            public bool IsNullable { get; private set; }

            /// <summary>
            /// Is value-type or not
            /// </summary>
            public bool IsValueType { get; private set; }

            /// <summary>
            /// is basicly reference type (String. DateTime, Timespam)
            /// </summary>
            public bool IsBasiclyRefType { get; private set; }

            /// <summary>
            /// Is basicly type or not
            /// </summary>
            public bool IsBasicType { get; private set; }

            /// <summary>
            /// Property Accessor
            /// </summary>
            private IAccessor _accessor;


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="info"></param>
            private Property(PropertyInfo info)
            {
                this.Info = info;

                // 参考：ほぼコピペ。
                // http://d.hatena.ne.jp/machi_pon/20090821/1250813986

                Delegate getter = null, setter = null;

                var getterMethod = info.GetGetMethod();
                if (getterMethod != null)
                {
                    var getterType = typeof(Func<,>).MakeGenericType(info.DeclaringType, info.PropertyType);
                    try
                    {
                        getter = getterMethod.CreateDelegate(getterType);
                    }
                    catch (Exception)
                    {
                    }

                }

                var setterMethod = info.GetSetMethod();
                if (setterMethod != null)
                {
                    var setterType = typeof(Action<,>).MakeGenericType(info.DeclaringType, info.PropertyType);
                    try
                    {
                        setter = (info.GetSetMethod()).CreateDelegate(setterType);
                    }
                    catch (Exception)
                    {
                    }
                }

                System.Type accessorType = typeof(Accessor<,>).MakeGenericType(info.DeclaringType, info.PropertyType);
                this._accessor = (IAccessor)Activator.CreateInstance(accessorType, getter, setter);


                this.Type = info.PropertyType;
                this.IsGettable = this._accessor.HasGetter;
                this.IsSettable = this._accessor.HasSetter;

                var typeInfo = this.Type.GetTypeInfo();
                this.IsNullable = (typeInfo.IsGenericType
                                   && this.Type.GetGenericTypeDefinition() == typeof(Nullable<>));
                this.UnderlyingType = (this.IsNullable)
                    ? Nullable.GetUnderlyingType(this.Type)
                    : this.Type;
                this.IsValueType = typeInfo.IsValueType;

                this.IsBasiclyRefType = (this.Type == typeof(string)
                                         || this.Type == typeof(DateTime)
                                         || this.Type == typeof(TimeSpan));

                this.IsBasicType = (this.IsValueType || this.IsBasiclyRefType);
            }

            /// <summary>
            /// Getter
            /// </summary>
            /// <param name="instance"></param>
            /// <returns></returns>
            public object Get(object instance)
            {
                return this._accessor.GetValue(instance);
            }

            /// <summary>
            /// Getter
            /// </summary>
            /// <typeparam name="TType"></typeparam>
            /// <param name="instance"></param>
            /// <returns></returns>
            public TType Get<TType>(object instance)
            {

                return (TType)this._accessor.GetValue(instance);
            }

            /// <summary>
            /// Setter
            /// </summary>
            /// <param name="instance"></param>
            /// <param name="value"></param>
            public void Set(object instance, object value)
            {
                this._accessor.SetValue(instance, value);
            }

            /// <summary>
            /// Setter
            /// </summary>
            /// <typeparam name="TType"></typeparam>
            /// <param name="instance"></param>
            /// <param name="value"></param>
            public void Set<TType>(object instance, TType value)
            {
                this._accessor.SetValue(instance, value);
            }
        }


        public System.Type Type { get; private set; }

        public ConcurrentDictionary<string, Xb.Type.Reflection.Property> Properties { get; private set; }
            = new ConcurrentDictionary<string, Xb.Type.Reflection.Property>();

        public System.Type[] Interfaces { get; private set; }

        public ConstructorInfo[] Constructors { get; private set; }

        public PropertyInfo[] PropertyInfos { get; private set; }

        public MethodInfo[] MethodInfos { get; private set; }

        public EventInfo[] EventInfos { get; private set; }

        public FieldInfo[] FieldInfos { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        private Reflection(System.Type type)
        {
            this.Type = type;

            this.Interfaces = type.GetInterfaces();

            this.PropertyInfos = type.GetProperties();

            foreach (var property in this.PropertyInfos)
            {
                var xbProp = Property.Get(property);
                this.Properties.GetOrAdd(property.Name, xbProp);
            }

            this.Constructors = type.GetConstructors();
            this.MethodInfos = type.GetMethods();
            this.EventInfos = type.GetEvents();
            this.FieldInfos = type.GetFields();
        }

        public bool HasInterface(System.Type type)
            => this.Interfaces.Any(t => t == type);

        public bool HasProperty(string name)
            => this.Properties.ContainsKey(name);

        public Xb.Type.Reflection.Property GetProperty(string propertyName)
        {
            if (!this.HasProperty(propertyName))
                return null;

            if (this.Properties.TryGetValue(propertyName, out Property property))
                return property;

            return null;
        }
    }
}
