using Shiny.Obd;
using Shiny.Obd.Commands;

namespace ShinyKmlRecorder.Services;

/// <summary>
/// A mode 01 reading that can be recorded. The key is stable and is used as the column/field name in exports
/// </summary>
public record ObdParameter(
    string Key,
    string Name,
    string Unit,
    byte Pid,
    bool IsDefault,
    Func<IObdConnection, CancellationToken, Task<double>> Read
);


public static class ObdParameters
{
    public static IReadOnlyList<ObdParameter> All { get; } =
    [
        // defaults
        Create("speed", "Vehicle Speed", "km/h", StandardCommands.VehicleSpeed, x => x, true),
        Create("rpm", "Engine RPM", "rpm", StandardCommands.EngineRpm, x => x, true),
        Create("odometer", "Odometer", "km", StandardCommands.Odometer, x => x, true),
        Create("coolant_temp", "Coolant Temperature", "°C", StandardCommands.CoolantTemperature, x => x, true),
        Create("engine_load", "Calculated Engine Load", "%", StandardCommands.CalculatedEngineLoad, x => x, true),
        Create("throttle", "Throttle Position (Relative)", "%", StandardCommands.RelativeThrottlePosition, x => x, true),
        Create("fuel_level", "Fuel Level", "%", StandardCommands.FuelLevel, x => x, true),
        Create("intake_air_temp", "Intake Air Temperature", "°C", StandardCommands.IntakeAirTemperature, x => x, true),
        Create("module_voltage", "Control Module Voltage", "V", StandardCommands.ControlModuleVoltage, x => x, true),
        Create("maf", "Mass Air Flow", "g/s", StandardCommands.MassAirFlow, x => x, true),

        // optional
        Create("fuel_rate", "Engine Fuel Rate", "L/h", StandardCommands.EngineFuelRate, x => x),
        Create("oil_temp", "Engine Oil Temperature", "°C", StandardCommands.EngineOilTemperature, x => x),
        Create("ambient_air_temp", "Ambient Air Temperature", "°C", StandardCommands.AmbientAirTemperature, x => x),
        Create("barometric_pressure", "Barometric Pressure", "kPa", StandardCommands.BarometricPressure, x => x),
        Create("intake_manifold_pressure", "Intake Manifold Pressure", "kPa", StandardCommands.IntakeManifoldPressure, x => x),
        Create("timing_advance", "Timing Advance", "° BTDC", StandardCommands.TimingAdvance, x => x),
        Create("runtime", "Run Time Since Start", "s", StandardCommands.RuntimeSinceStart, x => x.TotalSeconds),
        Create("distance_since_cleared", "Distance Since Codes Cleared", "km", StandardCommands.DistanceSinceCodesCleared, x => x),
        Create("distance_mil_on", "Distance With MIL On", "km", StandardCommands.DistanceWithMilOn, x => x),
        Create("pedal_position", "Accelerator Pedal (Relative)", "%", StandardCommands.RelativeAcceleratorPedalPosition, x => x),
        Create("throttle_absolute", "Throttle Position (Absolute)", "%", StandardCommands.ThrottlePosition, x => x),
        Create("throttle_commanded", "Commanded Throttle Actuator", "%", StandardCommands.CommandedThrottleActuator, x => x),
        Create("absolute_load", "Absolute Load Value", "%", StandardCommands.AbsoluteLoadValue, x => x),
        Create("fuel_pressure", "Fuel Pressure", "kPa", StandardCommands.FuelPressure, x => x),
        Create("fuel_rail_pressure", "Fuel Rail Gauge Pressure", "kPa", StandardCommands.FuelRailGaugePressure, x => x),
        Create("ethanol", "Ethanol Fuel", "%", StandardCommands.EthanolFuelPercent, x => x),
        Create("hybrid_battery", "Hybrid Battery Life", "%", StandardCommands.HybridBatteryLife, x => x),
        Create("stft_b1", "Short Term Fuel Trim Bank 1", "%", FuelTrimCommand.ShortTermBank1(), x => x),
        Create("ltft_b1", "Long Term Fuel Trim Bank 1", "%", FuelTrimCommand.LongTermBank1(), x => x),
        Create("stft_b2", "Short Term Fuel Trim Bank 2", "%", new FuelTrimCommand(0x08), x => x),
        Create("ltft_b2", "Long Term Fuel Trim Bank 2", "%", new FuelTrimCommand(0x09), x => x),
        Create("commanded_afr", "Commanded Air-Fuel Ratio", "λ", StandardCommands.CommandedAirFuelRatio, x => x),
        Create("torque_actual", "Actual Engine Torque", "%", StandardCommands.ActualEngineTorque, x => x),
        Create("torque_demand", "Driver Demand Torque", "%", StandardCommands.DriverDemandTorque, x => x),
        Create("egr_commanded", "Commanded EGR", "%", StandardCommands.CommandedEgr, x => x),
        Create("egr_error", "EGR Error", "%", StandardCommands.EgrError, x => x),
        Create("injection_timing", "Fuel Injection Timing", "°", StandardCommands.FuelInjectionTiming, x => x)
    ];


    public static IReadOnlyList<ObdParameter> Defaults { get; } = All.Where(x => x.IsDefault).ToList();


    public static ObdParameter? Find(string key) => All.FirstOrDefault(x => x.Key == key);


    static ObdParameter Create<T>(string key, string name, string unit, ObdCommand<T> command, Func<T, double> convert, bool isDefault = false)
        => new(key, name, unit, command.Pid, isDefault, async (connection, ct) => convert(await connection.Execute(command, ct)));
}
