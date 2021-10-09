using System;
using System.Diagnostics;
using System.Linq;
using Xb.Type;
using Xunit;

namespace TestXbTypeReflection
{
    public class TestSimpleClass
    {
        public string PublicFieldString;
        private int _privateFieldInt = 100;

        public int PublicPropertyInt
        {
            get => this._privateFieldInt;
            set => this._privateFieldInt = value;
        }
        public float PublicPropertyFloatReadOnly { get; }


        public TestSimpleClass()
        {
            this.PublicFieldString = "やっほう";
            this.PublicPropertyFloatReadOnly = 1.0f;
        }

        public TestSimpleClass(float floatValue)
        {
            this.PublicFieldString = "どーすか？";
            this.PublicPropertyFloatReadOnly = floatValue;
        }

        public void MethodVoid()
        {
            Debug.WriteLine("YO!!");
        }

        public void MethodVoidWithArgs(short shortValue)
        {
            Debug.WriteLine($"shortValue = {shortValue}");
        }

        public string MethodWidthResultString(
            int arg1,
            decimal arg2,
            string arg3 = null
        )
        {
            return $"arg1={arg1}, arg2={arg2}, arg3={arg3}";
        }

        public string MethodWidthResultString(
            int arg1,
            decimal arg2,
            string arg3 = null,
            double arg4 = default
        )
        {
            return $"arg1={arg1}, arg2={arg2}, arg3={arg3}, arg4={arg4}";
        }

        public string MethodWidthResultStringFullOptional(
            int arg1 = 1,
            decimal arg2 = (decimal)2.2,
            string arg3 = "optionString",
            double arg4 = (double)1.23456789
        )
        {
            return $"arg1={arg1}, arg2={arg2}, arg3={arg3}, arg4={arg4}";
        }
    }

    public class ReflectionTest
    {
        [Fact]
        public void CreateByTypeTest()
        {
            var reflection = Reflection.Get(typeof(TestSimpleClass));
            this.InnerCreateTest(reflection);
        }

        [Fact]
        public void CreateByInstanceTest()
        {
            var instance = new TestSimpleClass();
            var reflection = Reflection.Get(instance.GetType());
            this.InnerCreateTest(reflection);
        }

        [Fact]
        public void CreateByStringTest()
        {
            var instance = new TestSimpleClass();
            var reflection = Reflection.Get(instance.GetType().FullName);
            this.InnerCreateTest(reflection);
        }

        private void InnerCreateTest(Reflection reflection)
        {
            Assert.Equal(2, reflection.Constructors.Count);

            // privateフィールドは取得出来ない。
            Assert.Single(reflection.FieldInfos);

            Assert.Equal(2, reflection.PropertyInfos.Count);
            Assert.Equal(2, reflection.Properties.Count);
            Assert.True(reflection.Properties.ContainsKey("PublicPropertyInt"));
            Assert.True(reflection.Properties.ContainsKey("PublicPropertyFloatReadOnly"));

            // classのデフォルトメソッド(GetTypeなど), プロパティのGetter/Setterが入る。
            Assert.True(4 < reflection.MethodInfos.Count);

            Assert.Contains(reflection.MethodInfos, e => e.Name == "MethodWidthResultString");
            Assert.Equal(2, reflection.MethodInfos.Count(e => e.Name == "MethodWidthResultString"));

            Assert.Contains(reflection.MethodInfos, e => e.Name == "MethodVoid");
            Assert.Equal(1, reflection.MethodInfos.Count(e => e.Name == "MethodVoid"));

            Assert.Contains(reflection.MethodInfos, e => e.Name == "MethodVoidWithArgs");
            Assert.Equal(1, reflection.MethodInfos.Count(e => e.Name == "MethodVoidWithArgs"));

            Assert.Contains(reflection.MethodInfos, e => e.Name == "MethodWidthResultStringFullOptional");
            Assert.Equal(1, reflection.MethodInfos.Count(e => e.Name == "MethodWidthResultStringFullOptional"));
        }

        [Fact]
        public void PropertyAccessTest()
        {
            var reflection = Reflection.Get(typeof(TestSimpleClass));
            var instance = new TestSimpleClass();

            var prop1 = reflection.GetProperty("PublicPropertyInt");
            Assert.Equal(100, prop1.Get<int>(instance));

            prop1.Set(instance, 200);
            Assert.Equal(200, prop1.Get<int>(instance));

            var prop2 = reflection.GetProperty("PublicPropertyFloatReadOnly");
            Assert.Equal(1.0f, prop2.Get<float>(instance));

            try
            {
                prop2.Set<float>(instance, 2.0f);
                Assert.True(false);
            }
            catch (Exception)
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void GetFieldValueTest()
        {
            var instance = new TestSimpleClass();
            var reflection = Reflection.Get(instance.GetType().FullName);

            var fieldValue = reflection.GetFieldValue<string>(instance, "PublicFieldString");
            Assert.Equal("やっほう", fieldValue);
        }

        [Fact]
        public void MethodInvokeTest()
        {
            var reflection = Reflection.Get(typeof(TestSimpleClass));
            var instance = new TestSimpleClass();

            try
            {
                reflection.InvokeMethod(instance, "MethodVoid");
            }
            catch (Exception)
            {
                Assert.True(false);
            }

            try
            {
                reflection.InvokeMethod(instance, "MethodVoidWithArgs", (short)2);
            }
            catch (Exception)
            {
                Assert.True(false);
            }

            try
            {
                var result = reflection.InvokeMethod<string>(
                    instance,
                    "MethodWidthResultString",
                    3,
                    (decimal)10.4,
                    "Yo-Ho!"
                );

                Assert.Equal("arg1=3, arg2=10.4, arg3=Yo-Ho!", result);
            }
            catch (Exception)
            {
                Assert.True(false);
            }

            try
            {
                var result = reflection.InvokeMethod<string>(
                    instance,
                    "MethodWidthResultString",
                    4,
                    (decimal)18.23
                );

                Assert.Equal("arg1=4, arg2=18.23, arg3=", result);
            }
            catch (Exception)
            {
                Assert.True(false);
            }

            try
            {
                var result = reflection.InvokeMethod<string>(
                    instance,
                    "MethodWidthResultString",
                    8,
                    (decimal)12.34,
                    "StringArgだぜ",
                    (double)1.2345
                );

                Assert.Equal("arg1=8, arg2=12.34, arg3=StringArgだぜ, arg4=1.2345", result);
            }
            catch (Exception)
            {
                Assert.True(false);
            }

            try
            {
                var result = reflection.InvokeMethod<string>(
                    instance,
                    "MethodWidthResultStringFullOptional"
                );

                Assert.Equal("arg1=1, arg2=2.2, arg3=optionString, arg4=1.23456789", result);
            }
            catch (Exception)
            {
                Assert.True(false);
            }

        }
    }
}
