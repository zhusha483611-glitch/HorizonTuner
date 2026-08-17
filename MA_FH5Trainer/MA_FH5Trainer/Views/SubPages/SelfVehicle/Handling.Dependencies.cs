using HorizonTuner.Services.Handling.Abstractions;

namespace HorizonTuner.Views.SubPages.SelfVehicle;

public partial class Handling
{
    private readonly IMemoryWriter _memoryWriter;
    private readonly IGamepadReader _gamepadReader;
    private readonly IHandlingAutoConfigStore _configStore;
    private readonly ICarCheatsFacade _carCheats;
    private readonly IMiscCheatsFacade _miscCheats;
}
