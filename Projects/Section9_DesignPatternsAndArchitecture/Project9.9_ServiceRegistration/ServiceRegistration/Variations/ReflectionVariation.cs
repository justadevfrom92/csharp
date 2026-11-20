using System.Reflection;
using ServiceRegistration.Jobs;

namespace ServiceRegistration.Variations;

// Reflection = code that reads TYPES at run time: their properties, methods, and the classes inside a .dll.
// RegisterAllExistingJobs (original) already scans for IJob classes; these are the other everyday uses.
// Slower than normal code (see 5.3 ReflectionCostVariation) and not checked by the compiler: a typo in a
// name is only found when the code runs.
public static class ReflectionVariation
{
    // GetProperties: the public instance properties, in the order they're declared.
    public static List<string> ListPropertyNames(Type type)
    {
        return type.GetProperties().Select(property => property.Name).ToList();
    }

    // GetMethods with DeclaredOnly: only methods written in this class (without it you'd also get
    // ToString, GetHashCode... from object, plus the hidden get_Price/set_Price property methods).
    public static List<string> ListMethodNames(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToList();
    }

    // PropertyInfo.GetValue: read a property whose name is only known at run time (a string).
    public static object? ReadProperty(object target, string propertyName)
    {
        PropertyInfo property = FindProperty(target, propertyName);
        return property.GetValue(target);
    }

    // PropertyInfo.SetValue: write it. Also works on "init" properties - reflection skips that compiler rule.
    public static void WriteProperty(object target, string propertyName, object? value)
    {
        PropertyInfo property = FindProperty(target, propertyName);
        property.SetValue(target, value);
    }

    // Call a PRIVATE method. Tests and serializers do this; normal code shouldn't (private can change any time).
    // An exception thrown INSIDE the method arrives wrapped in a TargetInvocationException.
    public static object? CallPrivateMethod(object target, string methodName, params object?[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new ArgumentException($"No private method '{methodName}' on {target.GetType().Name}.");
        }
        return method.Invoke(target, arguments);
    }

    // PLUGINS: load a .dll by its file path and find the job classes inside it - the jobs aren't
    // known when this code is compiled. Assembly.LoadFrom keeps the file loaded until the app stops.
    public static List<Type> FindJobsInFile(string dllPath)
    {
        Assembly assembly = Assembly.LoadFrom(dllPath);
        return assembly.GetTypes()
            .Where(type => typeof(IJob).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
            .OrderBy(type => type.Name)
            .ToList();
    }

    private static PropertyInfo FindProperty(object target, string propertyName)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName);
        if (property == null)
        {
            throw new ArgumentException($"No property '{propertyName}' on {target.GetType().Name}.");
        }
        return property;
    }
}
