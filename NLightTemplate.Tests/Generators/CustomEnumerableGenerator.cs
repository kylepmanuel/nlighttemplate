using System.Collections;
using System.Collections.Generic;

namespace NLightTemplate.Tests.Generators
{
    public class CustomEnumerableGenerator : IEnumerable<object[]>
    {
        //Object, Template, Expected
        private readonly List<object[]> _data =
            [

                //Non-empty custom collection enumerates like List<T>
                [
                    new WidgetHolder
                    {
                        Title = "Store",
                        Widgets = new CustomCollection<Widget>(
                            new Widget { Name = "A", Qty = 1 },
                            new Widget { Name = "B", Qty = 2 })
                    },
                    "{Title}: {foreach Widgets}{Name}x{Qty} {/foreach Widgets}",
                    "Store: Ax1 Bx2 "
                ],

                //Empty custom collection renders nothing for its block
                [
                    new WidgetHolder
                    {
                        Title = "Empty",
                        Widgets = new CustomCollection<Widget>()
                    },
                    "{Title}:{foreach Widgets}{Name}{/foreach Widgets}",
                    "Empty:"
                ],

                //Nested custom collections (Widgets -> Tags), including an empty inner collection
                [
                    new WidgetHolder
                    {
                        Title = "Nested",
                        Widgets = new CustomCollection<Widget>(
                            new Widget { Name = "A", Tags = new CustomCollection<Tag>(new Tag { Label = "red" }, new Tag { Label = "blue" }) },
                            new Widget { Name = "B", Tags = new CustomCollection<Tag>() })
                    },
                    "{foreach Widgets}{Name}[{foreach Tags}{Label},{/foreach Tags}]{/foreach Widgets}",
                    "A[red,blue,]B[]"
                ],
            ];

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
