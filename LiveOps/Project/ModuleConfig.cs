using Madbox.LiveOps.CloudCode.FetchData;
using Madbox.LiveOps.CloudCode.FetchData.Unity;
using Madbox.LiveOps.CloudCode.GameModules;
using Madbox.LiveOps.CloudCode.Modules.Gold;
using Madbox.LiveOps.CloudCode.Modules.Global;
using Madbox.LiveOps.CloudCode.Modules.Level;
using Madbox.LiveOps.CloudCode.Modules.Tutorial;
using Madbox.LiveOps.CloudCode.Response;
using Madbox.LiveOps.CloudCode.Services.PingPong;
using Madbox.LiveOps.CloudCode.Signal;
using Microsoft.Extensions.DependencyInjection;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

/// <summary>
/// Cloud Code DI: Signal, ModuleRequestHandler, PingPong, game modules, GameModulesController.
/// </summary>
public sealed class ModuleConfig : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        IGameApiClient gameApiClient = GameApiClient.Create();
        config.Dependencies.AddSingleton(gameApiClient);

        config.Dependencies.AddScoped<IPlayerData, UnityPlayerData>();
        config.Dependencies.AddScoped<IGameState, UnityGameState>();
        config.Dependencies.AddScoped<IRemoteConfig, LocalOnlyRemoteConfig>();

        config.Dependencies.AddScoped<SignalModule>();
        config.Dependencies.AddScoped<ModuleRequestHandler>();

        config.Dependencies.AddScoped<PingPongService>();

        RegisterModule<GoldModule>(config);
        RegisterModule<GoldConfigModule>(config);
        RegisterModule<LevelModule>(config);
        RegisterModule<LevelConfigModule>(config);
        RegisterModule<TutorialModule>(config);
        RegisterModule<TutorialConfigModule>(config);
        RegisterModule<GlobalConfigModule>(config);

        config.Dependencies.AddScoped<GameModulesController>();
    }

    private static void RegisterModule<T>(ICloudCodeConfig config) where T : class, IGameModule
    {
        config.Dependencies.AddScoped<IGameModule, T>();
        config.Dependencies.AddScoped<T>();
    }
}
