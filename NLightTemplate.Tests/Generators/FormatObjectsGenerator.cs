using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NLightTemplate.Tests.Generators
{
    public class FormatObjectsGenerator : IEnumerable<object[]>
    {
        private readonly NumericTypes numericTypes = new NumericTypes();
        private readonly DateTypes dateTypes = new DateTypes();

        //obj, input, expected
        private List<object[]> _data =>
            typeof(NumericTypes).GetTypeInfo().DeclaredProperties.Where(p => p.GetMethod?.IsPublic ?? false)
                .SelectMany(prop =>
                    prop.GetCustomAttribute<FormatsAttribute>().Formats
                    .Select(fmt => new object[] { numericTypes, $"{{{prop.Name}:{fmt}}}", string.Format($"{{0:{fmt}}}", prop.GetValue(numericTypes)) })
                ).Concat(typeof(NumericTypes).GetTypeInfo().DeclaredProperties.Where(p => p.GetMethod?.IsPublic ?? false)
                .SelectMany(prop =>
                    prop.GetCustomAttribute<FormatsAttribute>().Formats
                    .Select(fmt => new object[] { numericTypes, $"{{{prop.Name},10:{fmt}}}", string.Format($"{{0,10:{fmt}}}", prop.GetValue(numericTypes)) })
                )).Concat(typeof(DateTypes).GetTypeInfo().DeclaredProperties.Where(p => p.GetMethod?.IsPublic ?? false)
                .SelectMany(prop =>
                    prop.GetCustomAttribute<FormatsAttribute>().Formats
                    .Select(fmt => new object[] { dateTypes, $"{{{prop.Name}:{fmt}}}", string.Format($"{{0:{fmt}}}", prop.GetValue(dateTypes)) })
                )).ToList();

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public class NumericTypes
        {
            [Formats(new[] { "C3", "D4", "e1", "E2", "F1", "G", "N1", "P0", "X4", "0000.0000" })]
            public byte NumericTypeByte { get; set; } = Convert.ToByte(new Random().Next(1, byte.MaxValue));
            [Formats(new[] { "C3", "D4", "e1", "E2", "F1", "G", "N1", "P0", "X4", "0000.0000" })]
            public sbyte NumericTypeSByte { get; set; } = Convert.ToSByte(new Random().Next(1, sbyte.MaxValue));
            [Formats(new[] { "G", "C", "D8", "E4", "e3", "F", "N", "P", "X", "0000.0000" })]
            public short NumericTypeShort { get; set; } = Convert.ToInt16(new Random().Next(1, short.MaxValue));
            [Formats(new[] { "G", "C", "D8", "E4", "e3", "F", "N", "P", "X", "0000.0000" })]
            public ushort NumericTypeUShort { get; set; } = Convert.ToUInt16(new Random().Next(1, ushort.MaxValue));
            [Formats(new[] { "G", "C", "D8", "E4", "e3", "F", "N", "P", "X", "0000.0000" })]
            public int NumericTypeInt { get; set; } = new Random().Next(1, int.MaxValue);
            [Formats(new[] { "G", "C", "D8", "E4", "e3", "F", "N", "P", "X", "0000.0000" })]
            public uint NumericTypeUInt { get; set; } = Convert.ToUInt32(new Random().Next(1, int.MaxValue));
            [Formats(new[] { "G", "C", "D8", "E4", "e3", "F", "N", "P", "X", "0000.0000" })]
            public long NumericTypeLong { get; set; } = new Random().Next() * 1000L;
            [Formats(new[] { "G", "C", "D8", "E4", "e3", "F", "N", "P", "X", "0000.0000" })]
            public ulong NumericTypeULong { get; set; } = Convert.ToUInt64(new Random().Next() * 100L);
            [Formats(new[] { "G", "C", "E04", "F", "N", "P", "0,0.000", "#,#.00#;(#,#.00#)" })]
            public decimal NumericTypeDecimal { get; set; } = new Random().Next() * 1.1M;
            [Formats(new[] { "C", "E", "e", "F", "G", "N", "P", "R", "#,000.000", "0.###E-000", "000,000,000,000.00###" })]
            public double NumericTypeDouble { get; set; } = new Random().NextDouble();
            [Formats(new[] { "C", "E", "e", "F", "G", "N", "P", "R", "#,000.000", "0.###E-000", "000,000,000,000.00###" })]
            public float NumericTypeFloat { get; set; } = new Random().Next() * 1.1F;

        }

        public class DateTypes
        {
            [Formats(new[] { "d", "D", "f", "F", "g", "G", "m", "o", "R", "s", "t", "T", "u", "U", "y",
                            "h:mm:ss.ff t", "d MMM yyyy", "HH:mm:ss.f", "dd MMM HH:mm:ss", @"\Mon\t\h\: M", "HH:mm:ss.ffffzzz" })]
            public DateTime DateTypeDateTime { get; set; } = DateTime.Now;
            [Formats(new[] { "d", "D", "f", "F", "g", "G", "m", "o", "R", "s", "t", "T", "u", "y",
                            "h:mm:ss.ff t", "d MMM yyyy", "HH:mm:ss.f", "dd MMM HH:mm:ss", @"\Mon\t\h\: M", "HH:mm:ss.ffffzzz" })]
            public DateTimeOffset DateTypeDateTimeOffset { get; set; } = DateTime.Now;
            [Formats(new[] { "c", "g", "G", @"hh\:mm\:ss", "%m' min.'" })]
            public TimeSpan DateTypeTimeSpan { get; set; } = DateTime.Now.TimeOfDay;
            [Formats(new[] { "g", "G", "x", "X", "d", "D", "f", "F" })]
            public DayOfWeek DateTypeDayOfWeek { get; set; } = DateTime.Now.DayOfWeek;
        }

        public class FormatsAttribute : Attribute
        {
            public string[] Formats { get; set; }
            public FormatsAttribute(string[] formats)
            {
                Formats = formats;
            }
        }
    }
}
