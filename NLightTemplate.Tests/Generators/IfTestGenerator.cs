using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NLightTemplate.Tests.Generators
{
    public class IfTestGenerator : IEnumerable<object[]>
    {
        private readonly List<object[]> _data =
            [
                //Boolean form (unchanged behaviour)
                [new BooleanClass(true), "", ""],
                [new BooleanClass(false), "", ""],
                [new BooleanClass(true),  "{if MyBool}foo{/if MyBool}", "foo"],
                [new BooleanClass(false), "{if MyBool}foo{/if MyBool}", ""],

                //Boolean form with else
                [new BooleanClass(true),  "{if MyBool}yes{else}no{/if MyBool}", "yes"],
                [new BooleanClass(false), "{if MyBool}yes{else}no{/if MyBool}", "no"],

                //Numeric comparisons
                [new ConditionClass{ Age = 18 }, "{if Age >= 18}Adult{else}Minor{/if Age}", "Adult"],
                [new ConditionClass{ Age = 17 }, "{if Age >= 18}Adult{else}Minor{/if Age}", "Minor"],
                [new ConditionClass{ Age = 18 }, "{if Age == 18}eq{/if Age}", "eq"],
                [new ConditionClass{ Age = 18 }, "{if Age != 18}ne{else}same{/if Age}", "same"],
                [new ConditionClass{ Age = 18 }, "{if Age > 18}gt{else}not{/if Age}", "not"],
                [new ConditionClass{ Age = 18 }, "{if Age <= 18}le{/if Age}", "le"],
                [new ConditionClass{ Age = 20 }, "{if Age < 18}kid{else}grown{/if Age}", "grown"],

                //String comparisons
                [new ConditionClass{ Status = "Active" },   "{if Status == Active}on{else}off{/if Status}", "on"],
                [new ConditionClass{ Status = "Inactive" }, "{if Status == Active}on{else}off{/if Status}", "off"],
                [new ConditionClass{ Status = "Active" },   "{if Status != Active}x{else}y{/if Status}", "y"],

                //Boolean equality against a literal
                [new ConditionClass{ MyBool = true },  "{if MyBool == true}t{else}f{/if MyBool}", "t"],
                [new ConditionClass{ MyBool = true },  "{if MyBool == false}t{else}f{/if MyBool}", "f"],

                //A non-bool operator-less block is left verbatim (unknown-token behaviour)
                [new ConditionClass{ Status = "Active" }, "{if Status}z{/if Status}", "{if Status}z{/if Status}"],

                //Nested if/else on different keys
                [new ConditionClass{ Age = 18, Status = "Active" }, "{if Age >= 18}A{if Status == Active}-on{else}-off{/if Status}{else}Minor{/if Age}", "A-on"],
                [new ConditionClass{ Age = 17, Status = "Active" }, "{if Age >= 18}A{if Status == Active}-on{else}-off{/if Status}{else}Minor{/if Age}", "Minor"],

                //Nested if/else on the SAME key (resolved recursively)
                [new ConditionClass{ Age = 18 }, "{if Age >= 18}{if Age >= 65}senior{else}adult{/if Age}{/if Age}", "adult"],
                [new ConditionClass{ Age = 70 }, "{if Age >= 18}{if Age >= 65}senior{else}adult{/if Age}{/if Age}", "senior"],

                //Property-to-property comparison via the @ prefix
                [new ConditionClass{ Age = 20, Threshold = 18 }, "{if Age >= @Threshold}Y{else}N{/if Age}", "Y"],
                [new ConditionClass{ Age = 17, Threshold = 18 }, "{if Age >= @Threshold}Y{else}N{/if Age}", "N"],
                [new ConditionClass{ Status = "Active", Target = "Active" },   "{if Status == @Target}Y{else}N{/if Status}", "Y"],
                [new ConditionClass{ Status = "Active", Target = "Disabled" }, "{if Status == @Target}Y{else}N{/if Status}", "N"],
                //An unknown @property resolves to no value (no match)
                [new ConditionClass{ Age = 5 }, "{if Age == @Nope}Y{else}N{/if Age}", "N"],

                //Enum comparison by name (case-insensitive), by number, ordering, and property-to-property
                [new EnumConditionClass{ Status = Statuses.Active }, "{if Status == Active}Y{else}N{/if Status}", "Y"],
                [new EnumConditionClass{ Status = Statuses.Active }, "{if Status == active}Y{else}N{/if Status}", "Y"],
                [new EnumConditionClass{ Status = Statuses.Active }, "{if Status == 1}Y{else}N{/if Status}", "Y"],
                [new EnumConditionClass{ Status = Statuses.Active }, "{if Status == Disabled}Y{else}N{/if Status}", "N"],
                [new EnumConditionClass{ Status = Statuses.Active }, "{if Status != None}Y{else}N{/if Status}", "Y"],
                [new EnumConditionClass{ Status = Statuses.Disabled }, "{if Status >= Active}Y{else}N{/if Status}", "Y"],
                [new EnumConditionClass{ Status = Statuses.Active, Other = Statuses.Active },   "{if Status == @Other}Y{else}N{/if Status}", "Y"],
                [new EnumConditionClass{ Status = Statuses.Active, Other = Statuses.Disabled }, "{if Status == @Other}Y{else}N{/if Status}", "N"],
            ];

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class BooleanClass(bool b)
    {
        public bool MyBool { get; set; } = b;
    }

    public class ConditionClass
    {
        public int Age { get; set; }
        public string Status { get; set; }
        public bool MyBool { get; set; }
        public string Active { get; set; } = "foo";
        public int Threshold { get; set; }
        public string Target { get; set; }
    }

    public enum Statuses
    {
        None = 0,
        Active = 1,
        Disabled = 2
    }

    public class EnumConditionClass
    {
        public Statuses Status { get; set; }
        public Statuses Other { get; set; }
    }
}
