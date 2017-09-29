using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NLightTemplate.Tests.Generators
{
    public class ConfiguredCustomerGenerator : IEnumerable<object[]>
    {
        private static readonly StringTemplateConfiguration cfg = new FluentStringTemplateConfiguration().OpenToken("<$").CloseToken("$>").ForeachToken("fe").ExposeConfiguration();
        private readonly List<object[]> _data = new List<object[]>
            {
                new object[] {Customer.GenerateDemo(),cfg, "", ""},
                new object[] {Customer.GenerateDemo(),cfg, "<$Id$>", "1"},
                new object[] {Customer.GenerateDemo(),cfg, "<$FirstName$>|<$LastName$>", "John|Doe"},
                new object[] {Customer.GenerateDemo(),cfg, "<$FirstName$>|<$LastName$> -<$fe Orders$> Order Id: <$Id$><$/fe Orders$>", "John|Doe - Order Id: 123 Order Id: 124" },
                new object[] {Customer.GenerateDemo(),cfg, "<$FirstName$>|<$LastName$> -<$fe Orders$> Order Id: <$Id$> <$fe Details$><$Quantity$><$Product.Name$><$/fe Details$><$/fe Orders$>", "John|Doe - Order Id: 123 1Blue Shirt2White Socks Order Id: 124 1Red Shoes4White Shirt" },
                new object[] {Customer.GenerateDemo(),cfg,
@"Thank you <$FullName$> for your recent order(s):
something unknown: <$idunno$>

<$fe Orders$>
Order <$Id$> placed at <$Placed$> and Shipped <$Shipped$>
QTY	Product		 Price SubTotal
<$fe Details$>
<$Quantity$>	<$Product.Name$>	 <$UnitPrice$> 	<$SubTotal$>
<$/fe Details$>
			Total: <$SubTotal$>
<$/fe Orders$>
<$fe Orders$>
This is the 2nd list for Order: <$Id$>
QTY	Product		 Price SubTotal
<$fe Details$>
<$Quantity$>	<$Product$>	 <$UnitPrice$> 	<$SubTotal$>
<$/fe Details$>
					Total: 	<$SubTotal$>
<$/fe Orders$>",
"Thank you John Doe for your recent order(s):\r\nsomething unknown: <$idunno$>\r\n\r\n\r\nOrder 123 placed at 9/19/2017 4:47:40 PM and Shipped \r\nQTY\tProduct\t\t Price SubTotal\r\n\r\n1\tBlue Shirt\t 12.35 \t12.35\r\n\r\n2\tWhite Socks\t 5.95 \t11.9\r\n\r\n\t\t\tTotal: 24.25\r\n\r\nOrder 124 placed at 9/19/2017 4:47:40 PM and Shipped \r\nQTY\tProduct\t\t Price SubTotal\r\n\r\n1\tRed Shoes\t 59.99 \t59.99\r\n\r\n4\tWhite Shirt\t 11.95 \t47.8\r\n\r\n\t\t\tTotal: 107.79\r\n\r\n\r\nThis is the 2nd list for Order: 123\r\nQTY\tProduct\t\t Price SubTotal\r\n\r\n1\tBlue Shirt In Stock\t 12.35 \t12.35\r\n\r\n2\tWhite Socks Unavailable\t 5.95 \t11.9\r\n\r\n\t\t\t\t\tTotal: \t24.25\r\n\r\nThis is the 2nd list for Order: 124\r\nQTY\tProduct\t\t Price SubTotal\r\n\r\n1\tRed Shoes Unavailable\t 59.99 \t59.99\r\n\r\n4\tWhite Shirt In Stock\t 11.95 \t47.8\r\n\r\n\t\t\t\t\tTotal: \t107.79\r\n" }
           };
        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
