namespace Records.Variations;

// Allowed only because UnsealedVehicleVariation isn't sealed. "record X : BusModel" would be a compile error.
public record ElectricBusVariation(string Line, int BatteryPercent) : UnsealedVehicleVariation(Line);
