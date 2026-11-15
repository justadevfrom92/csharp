using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceRegistration.BackgroundServices;
using ServiceRegistration.Jobs;
using ServiceRegistration.Services;
using ServiceRegistration.Settings;
using ServiceRegistration.Stores;

namespace ServiceRegistration.Extensions;

public static class JobServiceCollectionExtensions
{
    // builder.Services.RegisterAllExistingJobs();
    //
    // ASSEMBLY SCANNING: instead of writing one AddTransient<IJob, ...>() line per job (and forgetting one),
    // look through the compiled code for every class that implements IJob and register it.
    // A new job is then just a new class - no Program.cs change.
    //
    // An EXTENSION METHOD on IServiceCollection, returning it, so it chains like the built-in Add...() methods.
    public static IServiceCollection RegisterAllExistingJobs(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        Assembly[] toScan = assemblies.Length > 0 ? assemblies : [typeof(IJob).Assembly];

        IEnumerable<Type> jobTypes = toScan
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IJob).IsAssignableFrom(type))
            .OrderBy(type => type.Name);

        foreach (Type jobType in jobTypes)
        {
            // TryAddEnumerable: adds "IJob -> this class" only if THIS exact pair isn't registered yet.
            // So calling RegisterAllExistingJobs() twice doesn't run every job twice.
            // Transient: a fresh job object per run (inside the runner's scope).
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IJob), jobType));
        }

        // What every job needs. TryAdd...: only if the app (or a test) hasn't registered its own already.
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<JobRunStore>();
        services.TryAddSingleton<JobSchedulerService>();
        services.AddOptions<JobRunnerSettings>()
            .BindConfiguration("JobRunner")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHostedService<JobRunnerBackgroundService>(); // safe to call twice: hosted services are TryAddEnumerable'd internally
        return services;
    }
}
