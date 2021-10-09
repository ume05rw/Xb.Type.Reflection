using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Xb.Type
{
    public class Reflection
    {
        #region "static"

        private static readonly ConcurrentDictionary<string, Reflection> _cache
            = new ConcurrentDictionary<string, Reflection>();

        public static Reflection Get(System.Type type)
        {
            if (Reflection._cache.ContainsKey(type.FullName))
                return Reflection._cache[type.FullName];

            try
            {
                var newRef = new Reflection(type);
                Reflection._cache.GetOrAdd(type.FullName, newRef);

                return newRef;
            }
            catch (Exception ex)
            {
                throw new Exception($"Type not found: {type.FullName}", ex);
            }
        }

        public static Reflection Get(string typeFullName)
            => Reflection.Get(System.Type.GetType(typeFullName));

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
                public Accessor(
                    Func<TTarget, TProperty> getter,
                    Action<TTarget, TProperty> setter
                )
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
            /// Is basicly reference type (String. DateTime, Timespam)
            /// </summary>
            public bool IsBasiclyRefType { get; private set; }

            /// <summary>
            /// Is basicly type or not
            /// </summary>
            public bool IsBasicType { get; private set; }

            /// <summary>
            /// Property Name
            /// </summary>
            public string Name => this.Info.Name;

            /// <summary>
            /// Property Accessor
            /// </summary>
            private readonly IAccessor _accessor;


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
                    var getterType = typeof(Func<,>)
                        .MakeGenericType(info.DeclaringType, info.PropertyType);
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
                    var setterType = typeof(Action<,>)
                        .MakeGenericType(info.DeclaringType, info.PropertyType);
                    try
                    {
                        setter = (info.GetSetMethod()).CreateDelegate(setterType);
                    }
                    catch (Exception)
                    {
                    }
                }

                System.Type accessorType = typeof(Accessor<,>)
                    .MakeGenericType(info.DeclaringType, info.PropertyType);
                this._accessor = (IAccessor)Activator
                    .CreateInstance(accessorType, getter, setter);


                this.Type = info.PropertyType;
                this.IsGettable = this._accessor.HasGetter;
                this.IsSettable = this._accessor.HasSetter;

                var typeInfo = this.Type.GetTypeInfo();
                this.IsNullable = (
                    typeInfo.IsGenericType
                    && this.Type.GetGenericTypeDefinition() == typeof(Nullable<>)
                );
                this.UnderlyingType = (this.IsNullable)
                    ? Nullable.GetUnderlyingType(this.Type)
                    : this.Type;
                this.IsValueType = typeInfo.IsValueType;

                this.IsBasiclyRefType = (
                    this.Type == typeof(string)
                    || this.Type == typeof(DateTime)
                    || this.Type == typeof(TimeSpan)
                );

                this.IsBasicType = (this.IsValueType || this.IsBasiclyRefType);
            }

            /// <summary>
            /// Getter
            /// </summary>
            /// <param name="instance"></param>
            /// <returns></returns>
            public object Get(object instance)
                => this._accessor.GetValue(instance);

            /// <summary>
            /// Getter
            /// </summary>
            /// <typeparam name="TType"></typeparam>
            /// <param name="instance"></param>
            /// <returns></returns>
            public TType Get<TType>(object instance)
                => (TType)this._accessor.GetValue(instance);

            /// <summary>
            /// Setter
            /// </summary>
            /// <param name="instance"></param>
            /// <param name="value"></param>
            public void Set(object instance, object value)
                => this._accessor.SetValue(instance, value);

            /// <summary>
            /// Setter
            /// </summary>
            /// <typeparam name="TType"></typeparam>
            /// <param name="instance"></param>
            /// <param name="value"></param>
            public void Set<TType>(object instance, TType value)
                => this._accessor.SetValue(instance, value);
        }


        public System.Type Type { get; private set; }

        public ReadOnlyDictionary<string, Reflection.Property> Properties { get; private set; }

        public System.Type[] Interfaces { get; private set; }

        public ReadOnlyCollection<ConstructorInfo> Constructors { get; private set; }

        public ReadOnlyCollection<PropertyInfo> PropertyInfos { get; private set; }

        public ReadOnlyCollection<MethodInfo> MethodInfos { get; private set; }

        public ReadOnlyDictionary<MethodInfo, ReadOnlyCollection<ParameterInfo>> MethodParameters { get; private set; }

        public ReadOnlyCollection<EventInfo> EventInfos { get; private set; }

        public ReadOnlyCollection<FieldInfo> FieldInfos { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        private Reflection(System.Type type)
        {
            this.Type = type;

            this.Interfaces = type.GetInterfaces();

            this.PropertyInfos = new ReadOnlyCollection<PropertyInfo>(type.GetProperties());

            var propDic = new Dictionary<string, Property>();
            foreach (var property in this.PropertyInfos)
            {
                var xbProp = Property.Get(property);
                propDic.Add(property.Name, xbProp);
            }
            this.Properties = new ReadOnlyDictionary<string, Property>(propDic);

            this.Constructors = new ReadOnlyCollection<ConstructorInfo>(type.GetConstructors());

            this.MethodInfos = new ReadOnlyCollection<MethodInfo>(type.GetMethods());

            var methodDic = new Dictionary<MethodInfo, ReadOnlyCollection<ParameterInfo>>();
            foreach (var method in this.MethodInfos)
                methodDic.Add(method, new ReadOnlyCollection<ParameterInfo>(method.GetParameters()));

            this.MethodParameters = new ReadOnlyDictionary<MethodInfo, ReadOnlyCollection<ParameterInfo>>(methodDic);

            this.EventInfos = new ReadOnlyCollection<EventInfo>(type.GetEvents());
            this.FieldInfos = new ReadOnlyCollection<FieldInfo>(type.GetFields());
        }

        public bool HasInterface(System.Type type)
            => this.Interfaces.Any(t => t == type);

        public bool HasProperty(string name)
            => this.Properties.ContainsKey(name);

        public bool HasMethod(string name)
            => this.MethodInfos.Any(e => e.Name == name);

        public bool HasEvent(string name)
            => this.EventInfos.Any(e => e.Name == name);

        public bool HasField(string name)
            => this.FieldInfos.Any(e => e.Name == name);

        public bool TryGetPropertyValue<TType>(
            object instance,
            string propertyName,
            out TType value
        )
        {
            value = default;

            var property = this.Properties
                .Where(e => e.Value.Name == propertyName)
                .Select(e => e.Value)
                .FirstOrDefault();

            if (property == null)
                return false;

            value = property.Get<TType>(instance);

            return true;
        }

        public Reflection.Property GetProperty(string propertyName)
        {
            if (!this.HasProperty(propertyName))
                return null;

            if (this.Properties.TryGetValue(propertyName, out Property property))
                return property;

            return null;
        }



        private MethodInfo GetMethodInfo(string methodName, params object[] args)
        {
            return this.MethodInfos
                .Where(e => e.Name == methodName)
                .Where(e =>
                {
                    if (!this.MethodParameters.TryGetValue(e, out var parameters))
                        throw new Exception("うせやろ！？");

                    if (parameters.Count < args.Length)
                        return false;

                    for (var i = 0; i < parameters.Count; i++)
                    {
                        var param = parameters[i];
                        if (args.Length <= i)
                        {
                            if (!param.IsOptional)
                                return false;
                        }
                        else
                        {
                            var arg = args[i];
                            if (param.ParameterType != arg.GetType())
                                return false;
                        }
                    }

                    return true;
                })
                .FirstOrDefault();
        }

        /// <summary>
        /// Invoke Method and Get Result
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <remarks>
        /// Generics Method Not Supported.
        /// </remarks>
        public TResult InvokeMethod<TResult>(
            object instance,
            string methodName,
            params object[] args
        )
        {
            var formattedArgs = args ?? Array.Empty<object>();

            var method = this.GetMethodInfo(methodName, formattedArgs);

            if (method == null)
                throw new InvalidOperationException($"Method [{methodName}] Not Found.");

            try
            {
                return (TResult)method.Invoke(instance, formattedArgs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invoke Failure.", ex);
            }
        }

        /// <summary>
        /// Invoke Method and Get Result
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public TResult InvokeMethod<TResult>(object instance, string methodName)
            => this.InvokeMethod<TResult>(instance, methodName, Array.Empty<object>());

        /// <summary>
        /// Invoke Method without Result
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public void InvokeMethod(
            object instance,
            string methodName,
            params object[] args
        )
        {
            var formattedArgs = args ?? Array.Empty<object>();

            var method = this.GetMethodInfo(methodName, formattedArgs);

            if (method == null)
                throw new InvalidOperationException($"Method [{methodName}] Not Found.");

            try
            {
                method.Invoke(instance, formattedArgs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invoke Failure.", ex);
            }
        }


        /// <summary>
        /// Invoke Method without Result
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        /// <remarks>
        /// Generics Method Not Supported.
        /// </remarks>
        public void InvokeMethod(object instance, string methodName)
            => this.InvokeMethod(instance, methodName, Array.Empty<object>());

        ///// <summary>
        ///// Try to get the field value
        ///// </summary>
        ///// <param name="instance"></param>
        ///// <param name="fieldName"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public bool TryGetFiledValue(object instance, string fieldName, out object value)
        //{
        //    value = null;

        //    var field = this.FieldInfos
        //        .FirstOrDefault(e => e.Name == fieldName /*|| e.Name.StartsWith($"{fieldName}`") */);

        //    if (field == null)
        //        return false;

        //    value = field.GetValue(instance);

        //    return true;
        //}
    }
}
