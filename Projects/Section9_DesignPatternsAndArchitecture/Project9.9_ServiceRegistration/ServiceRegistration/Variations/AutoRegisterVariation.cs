namespace ServiceRegistration.Variations;

// Marks a job class for RegisterJobsWithAttribute(). Only classes that carry it are registered.
// Used as [AutoRegisterVariation] (attributes are normally named ...Attribute; this one follows the folder rule).
[AttributeUsage(AttributeTargets.Class)]
public sealed class AutoRegisterVariation : Attribute
{
}
