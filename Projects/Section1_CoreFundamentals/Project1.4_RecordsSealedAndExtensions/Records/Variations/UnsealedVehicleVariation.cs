namespace Records.Variations;

// NOT sealed: anyone can inherit (ElectricBusVariation below). A switch on "UnsealedVehicleVariation" also
// matches every sub-kind, which may not be what the switch's author expected.
public record UnsealedVehicleVariation(string Line);
