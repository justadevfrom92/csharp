using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceRegistration.Jobs;

namespace ServiceRegistration.Variations;

// Four other ways to do what RegisterAllExistingJobs (original: scan for every class implementing IJob) does.
// They differ in WHICH classes they find, and in what you must remember when you add a new job.
public static class JobRegistrationVariation
{
    // MANUAL: one line per job. Nothing is "magic" - but a new job does nothing until someone adds its line here.
    public static IServiceCollection RegisterJobsByHand(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IJob, CleanupTempFilesJob>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IJob, RefreshPricesJob>());
        return services; // SendDailyReportJob "forgotten" on purpose: the tests show it never runs
    }

    // A GENERIC helper: still one line per job, but shorter and the compiler checks TJob really is an IJob.
    public static IServiceCollection AddJob<TJob>(this IServiceCollection services) where TJob : class, IJob
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IJob, TJob>());
        return services;
    }

    // NAMING CONVENTION: every class whose name ends in "Job". Finds classes that DON'T implement IJob too
    // (they'd fail when resolved as IJob), and misses a real job named e.g. "NightlyCleanup".
    public static List<Type> FindJobsByName(Assembly assembly)
    {
        return assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("Job")).OrderBy(type => type.Name).ToList();
    }

    // ATTRIBUTE: only classes marked [AutoRegisterVariation]. Opt-in: nothing is registered by accident, but
    // forgetting the attribute is the same mistake as forgetting the manual line.
    public static IServiceCollection RegisterJobsWithAttribute(this IServiceCollection services, Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(IJob).IsAssignableFrom(type) && type.GetCustomAttribute<AutoRegisterVariation>() != null))
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IJob), type));
        }
        return services;
    }
}
