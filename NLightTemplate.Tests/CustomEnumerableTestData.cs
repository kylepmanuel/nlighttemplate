using System.Collections;
using System.Collections.Generic;

namespace NLightTemplate.Tests
{
    /// <summary>
    /// A user-defined <see cref="IEnumerable{T}"/> that is NOT a <see cref="List{T}"/>
    /// used to validate that foreach enumeration works against arbitrary custom collection implementations (README roadmap item).
    /// </summary>
    public class CustomCollection<T>(params T[] items) : IEnumerable<T>
    {
        private readonly T[] _items = items;

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _items)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class WidgetHolder
    {
        public string Title { get; set; }
        public CustomCollection<Widget> Widgets { get; set; }
    }

    public class Widget
    {
        public string Name { get; set; }
        public int Qty { get; set; }
        public CustomCollection<Tag> Tags { get; set; }
    }

    public class Tag
    {
        public string Label { get; set; }
    }
}
