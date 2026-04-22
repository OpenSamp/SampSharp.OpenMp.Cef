using SampSharp.Entities;
using SampSharp.Entities.SAMP;

namespace SampSharp.Cef.Entities;

/// <summary>
/// Клиентский API к Cef.dll. Реализация живёт в <see cref="CefService"/>,
/// которая вызывает C-exports в SampSharp.Cef.dll → ICefComponent extension в Cef.dll.
///
/// Если Cef.dll не загружен, все методы — no-op. Проверить через <see cref="IsAvailable"/>.
/// </summary>
public interface ICefService
{
    /// <summary>Подгружен ли Cef.dll и установил ли bridge.</summary>
    bool IsAvailable { get; }

    // ----- Session --------------------------------------------------------

    bool PlayerHasPlugin(Player player);
    bool PlayerHasPlugin(int playerId);

    /// <summary>
    /// Регистрирует ресурс (директория <c>scriptfiles/cef/name</c>) для скачивания клиентами.
    /// Вызывать на OnGameModeInit / IEcsStartup.
    /// </summary>
    void AddResource(string resourceName);

    // ----- Browser lifecycle ---------------------------------------------

    void CreateBrowser(Player player, int browserId, string url, bool focused, bool controlsChat = true);
    void CreateWorldBrowser(Player player, int browserId, string url, string textureName, float width, float height);
    void CreateWorld2DBrowser(Player player, int browserId, string url,
        float worldX, float worldY, float worldZ,
        float width = 0f, float height = 0f, float offsetZ = 0f, float pivotX = 0f, float pivotY = 0f);
    void SetWorld2DBrowserPos(Player player, int browserId, float worldX, float worldY, float worldZ);

    void SetBrowserVisible(Player player, int browserId, bool visible);
    void DestroyBrowser(Player player, int browserId);

    // ----- Events ---------------------------------------------------------

    /// <summary>
    /// Регистрирует событие, которое JS сможет эмитить обратно на сервер.
    /// <paramref name="callback"/> нужен для совместимости с Pawn-диспатчем;
    /// в чистом C#-проекте можно передавать пустую строку и слушать
    /// <see cref="ICefEventHandler.OnEvent"/>.
    /// </summary>
    void RegisterEvent(string name, string callback, params CefArgType[] signature);

    /// <summary>Отправляет событие в JS конкретного браузера.</summary>
    void EmitEvent(Player player, int browserId, string name, params CefArg[] args);

    // ----- Browser utilities ---------------------------------------------

    void ReloadBrowser(Player player, int browserId, bool ignoreCache = false);
    void FocusBrowser(Player player, int browserId, bool focused);
    void EnableDevTools(Player player, int browserId, bool enabled);

    void AttachBrowserToObject(Player player, int browserId, int objectId);
    void DetachBrowserFromObject(Player player, int browserId, int objectId);

    // ----- Audio ----------------------------------------------------------

    void SetBrowserMuted(Player player, int browserId, bool muted);
    void SetBrowserAudioMode(Player player, int browserId, CefAudioMode mode);
    void SetBrowserAudioSettings(Player player, int browserId, float maxDistance, float referenceDistance);

    // ----- HUD / chat -----------------------------------------------------

    void ToggleHudComponent(Player player, CefHudComponent component, bool toggle);
    void ToggleSpawnScreen(Player player, bool toggle);
    void ClearChat(Player player);
    void ToggleChatInput(Player player, bool toggle);
    bool IsChatInputOpen(Player player);

    // ----- Key capture ----------------------------------------------------

    void SetKeyCapture(Player player, bool enabled);
    void EnableKey(Player player, int virtualKey, bool enabled);

    // ----- Misc -----------------------------------------------------------

    void ExitGame(Player player);
}
