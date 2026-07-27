using System;
using Microsoft.Extensions.DependencyInjection;
using NLightTemplate;

namespace Sample.DependencyInjection
{
    class Program
    {
        static void Main()
        {
            // 1. Register services. NLightTemplate takes NO dependency on any DI container —
            //    you register the instance renderer yourself.
            var services = new ServiceCollection();
            services.AddSingleton<ITemplateRenderer, TemplateRenderer>(); // default { } tokens
            // ...or alternately
            // services.AddSingleton<ITemplateRenderer>(new TemplateRenderer());
            services.AddSingleton<MembershipNotificationService>();

            // ...to use custom tokens, register an IStringTemplateConfiguration and the container injects it into
            // TemplateRenderer (which falls back to defaults when none is registered):
            // services.AddSingleton<IStringTemplateConfiguration>(_ =>
            //     StringTemplateConfiguration.Create(c => c.OpenToken("<%").CloseToken("%>").ForeachToken("fe")));
            // services.AddSingleton<ITemplateRenderer, TemplateRenderer>();

            using var provider = services.BuildServiceProvider();

            // 2. Resolve a service that receives ITemplateRenderer via constructor injection.
            var notifications = provider.GetRequiredService<MembershipNotificationService>();

            Console.WriteLine(notifications.BuildWelcome(new Customer
            {
                Name = "John Doe",
                Age = 20,
                Level = MembershipLevel.Gold,
                Points = 1200,
                RewardThreshold = 1000
            }));

            Console.WriteLine(new string('-', 48));

            Console.WriteLine(notifications.BuildWelcome(new Customer
            {
                Name = "Jane Minor",
                Age = 16,
                Level = MembershipLevel.Standard,
                Points = 250,
                RewardThreshold = 1000
            }));
        }
    }

    /// <summary>
    /// A service that depends on <see cref="ITemplateRenderer"/> through constructor injection not static calls.
    /// </summary>
    public class MembershipNotificationService(ITemplateRenderer renderer)
    {
        private readonly ITemplateRenderer _renderer = renderer;

        // Demonstrates: enum comparison by name ({if Level == Gold}), property-to-property ({if Points >= @RewardThreshold}),
        // numeric comparison, and {else} branches.
        private const string Template =
@"Hi {Name}!
{if Level == Gold}Gold member perks unlocked.{else}Upgrade to Gold for more perks.{/if Level}
{if Points >= @RewardThreshold}You've earned a reward!{else}Earn {RewardThreshold} points total for your next reward.{/if Points}
{if Age >= 18}Full account access.{else}Some features are restricted for members under 18.{/if Age}";

        public string BuildWelcome(Customer customer) => _renderer.Render(Template, customer);
    }

    public enum MembershipLevel { None = 0, Standard = 1, Gold = 2 }

    public class Customer
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public MembershipLevel Level { get; set; }
        public int Points { get; set; }
        public int RewardThreshold { get; set; }
    }
}
