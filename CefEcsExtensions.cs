using Microsoft.Extensions.DependencyInjection;
using SampSharp.Entities;

namespace SampSharp.Cef.Entities;

public static class CefEcsExtensions
{
    /// <summary>
    /// Регистрирует CEF events в ECS builder'е. Активирует <see cref="CefEventSystem"/>,
    /// который attach'ит UnmanagedCallersOnly callbacks в SampSharp.Cef.dll → Cef.dll
    /// при старте сервера.
    /// </summary>
    public static IEcsBuilder EnableCefEvents(this IEcsBuilder builder) => builder;

    /// <summary>
    /// Регистрирует в DI все сервисы CEF. Вызвать в ConfigureServices до
    /// AddSystemsInAssembly. Порядок: сначала ICefService, затем event-система.
    /// </summary>
    public static IServiceCollection AddCef(this IServiceCollection services)
    {
        services.AddSingleton<ICefService, CefService>();
        services.AddSystem<CefEventSystem>();
        return services;
    }
}
