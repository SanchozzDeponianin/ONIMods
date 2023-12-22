using Newtonsoft.Json;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Options;

namespace RoverRefueling
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile(IndentOutput: true, SharedConfigLocation: true)]
    [RestartRequired]
    internal sealed class RoverRefuelingOptions : BaseOptions<RoverRefuelingOptions>
    {
        [JsonProperty]
        [Option]
        [Limit(15, 60)]
        public float charge_time { get; set; } = 30;

        // создание самого ровера фактически бесплатно - его потом можно разобрать
        // для запуска, например, ракетой с малым керосиновым двигателем можно создать одного ровера,
        // требуется 90 керосина и 45 оксилита на взлёт и посадку, ну и микроконтроль.
        // 150 керосина на перезарядку думаю норм.
        [JsonProperty]
        [Option]
        [Limit(50, 500)]
        public float fuel_mass_per_charge { get; set; } = 150;

        [JsonProperty]
        [Option]
        public bool fuel_cargo_bay_enable { get; set; } = true;

        [JsonProperty]
        [Option]
        public bool fuel_cargo_bay_fill_enable { get; set; } = false;
    }
}
