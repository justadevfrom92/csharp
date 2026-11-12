namespace PublishDemo.Models;

// Facts about HOW the app is running - they change with the publish mode, which is the point of the demo.
public record RuntimeInfoModel(
    string Framework,
    string OperatingSystem,
    string ProcessArchitecture,
    bool IsNativeAot,
    string ExecutablePath);
