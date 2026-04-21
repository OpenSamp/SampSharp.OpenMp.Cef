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

    bool PlayerHasPlugin(EntityId player);
    bool PlayerHasPlugin(int playerId);

    /// <summary>
    /// Регистрирует ресурс (директория <c>scriptfiles/cef/name</c>) для скачивания клиентами.
    /// Вызывать на OnGameModeInit / IEcsStartup.
    /// </summary>
    void AddResource(string resourceName);

    // ----- Browser lifecycle ---------------------------------------------

    void CreateBrowser(EntityId player, int browserId, string url, bool focused, bool controlsChat = true);
    void CreateWorldBrowser(EntityId player, int browserId, string url, string textureName, float width, float height);
    void CreateWorld2DBrowser(EntityId player, int browserId, string url,
        float worldX, float worldY, float worldZ,
        float width = 0f, float height = 0f, float offsetZ = 0f, float pivotX = 0f, float pivotY = 0f);
    void SetWorld2DBrowserPos(EntityId player, int browserId, float worldX, float worldY, float worldZ);

    void SetBrowserVisible(EntityId player, int browserId, bool visible);
    void DestroyBrowser(EntityId player, int browserId);

    // ----- Events ---------------------------------------------------------

    /// <summary>
    /// Регистрирует событие, которое JS сможет эмитить обратно на сервер.
    /// <paramref name="callback"/> нужен для совместимости с Pawn-диспатчем;
    /// в чистом C#-проекте можно передавать пустую строку и слушать
    /// <see cref="ICefEventHandler.OnEvent"/>.
    /// </summary>
    void RegisterEvent(string name, string callback, params CefArgType[] signature);

    /// <summary>Отправляет событие в JS конкретного браузера.</summary>
    void EmitEvent(EntityId player, int browserId, string name, params CefArg[] args);

    // ----- Browser utilities ---------------------------------------------

    void ReloadBrowser(EntityId player, int browserId, bool ignoreCache = false);
    void FocusBrowser(EntityId player, int browserId, bool focused);
    void EnableDevTools(EntityId player, int browserId, bool enabled);

    void AttachBrowserToObject(EntityId player, int browserId, int objectId);
    void DetachBrowserFromObject(EntityId player, int browserId, int objectId);

    // ----- Audio ----------------------------------------------------------

    void SetBrowserMuted(EntityId player, int browserId, bool muted);
    void SetBrowserAudioMode(EntityId player, int browserId, CefAudioMode mode);
    void SetBrowserAudioSettings(EntityId player, int browserId, float maxDistance, float referenceDistance);

    // ----- HUD / chat -----------------------------------------------------

    void ToggleHudComponent(EntityId player, CefHudComponent component, bool toggle);
    void ToggleSpawnScreen(EntityId player, bool toggle);
    void ClearChat(EntityId player);
    void ToggleChatInput(EntityId player, bool toggle);
    bool IsChatInputOpen(EntityId player);

    // ----- Key capture ----------------------------------------------------

    void SetKeyCapture(EntityId player, bool enabled);
    void EnableKey(EntityId player, int virtualKey, bool enabled);

    // ----- Misc -----------------------------------------------------------

    void ExitGame(EntityId player);
}
