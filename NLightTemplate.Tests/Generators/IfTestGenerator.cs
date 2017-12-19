using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NLightTemplate.Tests.Generators
{
    public class IfTestGenerator : IEnumerable<object[]>
    {
        private readonly List<object[]> _data = new List<object[]>
            {
                new object[] {new BooleanClass(true), "", ""},
                new object[] {new BooleanClass(false), "", ""},
                new object[] {new BooleanClass(true),  "{if MyBool}foo{/if MyBool}", "foo"},
                new object[] {new BooleanClass(false), "{if MyBool}foo{/if MyBool}", ""},
            };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class BooleanClass
    {
        public BooleanClass(bool b)
        {
            MyBool = b;
        }
        public bool MyBool { get; set; }
    }
}
