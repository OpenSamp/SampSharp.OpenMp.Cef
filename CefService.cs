using System;
using System.Text;
using SampSharp.Cef.Entities.Interop;
using SampSharp.Entities;
using SampSharp.Entities.SAMP;

namespace SampSharp.Cef.Entities;

/// <summary>
/// Реализация <see cref="ICefService"/>, пробрасывающая вызовы в Cef.dll
/// через C-exports в SampSharp.Cef.dll (<see cref="CefInterop"/>).
/// </summary>
public sealed class CefService : ICefService
{
    public bool IsAvailable => CefInterop.Cef_IsAvailable();

    public bool PlayerHasPlugin(Player player) => PlayerHasPlugin(PlayerToId(player));
    public bool PlayerHasPlugin(int playerId) => CefInterop.Cef_PlayerHasPlugin(playerId);

    public void AddResource(string resourceName) => CefInterop.Cef_AddResource(resourceName ?? "");

    public void CreateBrowser(Player player, int browserId, string url, bool focused, bool controlsChat = true)
        => CefInterop.Cef_CreateBrowser(PlayerToId(player), browserId, url ?? "", focused, controlsChat);

    public void CreateWorldBrowser(Player player, int browserId, string url, string textureName, float width, float height)
        => CefInterop.Cef_CreateWorldBrowser(PlayerToId(player), browserId, url ?? "", textureName ?? "", width, height);

    public void CreateWorld2DBrowser(Player player, int browserId, string url,
        float worldX, float worldY, float worldZ,
        float width = 0f, float height = 0f, float offsetZ = 0f, float pivotX = 0f, float pivotY = 0f)
        => CefInterop.Cef_CreateWorld2DBrowser(PlayerToId(player), browserId, url ?? "",
            worldX, worldY, worldZ, width, height, offsetZ, pivotX, pivotY);

    public void SetWorld2DBrowserPos(Player player, int browserId, float x, float y, float z)
        => CefInterop.Cef_SetWorld2DBrowserPos(PlayerToId(player), browserId, x, y, z);

    public void SetBrowserVisible(Player player, int browserId, bool visible)
        => CefInterop.Cef_SetBrowserVisible(PlayerToId(player), browserId, visible);

    public void DestroyBrowser(Player player, int browserId)
        => CefInterop.Cef_DestroyBrowser(PlayerToId(player), browserId);

    public unsafe void RegisterEvent(string name, string callback, params CefArgType[] signature)
    {
        int n = signature?.Length ?? 0;
        if (n == 0)
        {
            CefInterop.Cef_RegisterEvent(name ?? "", callback ?? "", 0, null);
            return;
        }
        Span<byte> buf = stackalloc byte[n];
        for (int i = 0; i < n; i++) buf[i] = (byte)signature![i];
        fixed (byte* p = buf) CefInterop.Cef_RegisterEvent(name ?? "", callback ?? "", n, p);
    }

    public unsafe void EmitEvent(Player player, int browserId, string name, params CefArg[] args)
    {
        int n = args?.Length ?? 0;
        if (n == 0)
        {
            CefInterop.Cef_EmitEvent(PlayerToId(player), browserId, name ?? "", 0, null);
            return;
        }

        // Each string needs a pinned UTF-8 buffer alive during the native call.
        // Compute total UTF-8 bytes first, allocate one big native-free scratch block.
        int utf8Total = 0;
        for (int i = 0; i < n; i++)
        {
            if (args![i].Type == CefArgType.String)
            {
                utf8Total += Encoding.UTF8.GetByteCount(args[i].StringValue ?? "") + 1; // +NUL
            }
        }

        Span<byte> stringBuf = utf8Total <= 1024 ? stackalloc byte[utf8Total] : new byte[utf8Total];
        Span<CefArgMarshal> marshalBuf = stackalloc CefArgMarshal[n];

        fixed (byte* sp0 = stringBuf)
        fixed (CefArgMarshal* mp = marshalBuf)
        {
            int cursor = 0;
            for (int i = 0; i < n; i++)
            {
                var a = args![i];
                var m = default(CefArgMarshal);
                m.Type = (byte)a.Type;
                switch (a.Type)
                {
                    case CefArgType.String:
                    {
                        string s = a.StringValue ?? "";
                        int written = Encoding.UTF8.GetBytes(s, stringBuf.Slice(cursor));
                        stringBuf[cursor + written] = 0;
                        m.StringPtr = sp0 + cursor;
                        cursor += written + 1;
                        break;
                    }
                    case CefArgType.Integer:
                        m.IntValue = a.IntValue;
                        break;
                    case CefArgType.Float:
                        m.FloatValue = a.FloatValue;
                        break;
                    case CefArgType.Bool:
                        m.BoolValue = a.BoolValue ? (byte)1 : (byte)0;
                        break;
                }
                mp[i] = m;
            }
            CefInterop.Cef_EmitEvent(PlayerToId(player), browserId, name ?? "", n, mp);
        }
    }

    public void ReloadBrowser(Player player, int browserId, bool ignoreCache = false)
        => CefInterop.Cef_ReloadBrowser(PlayerToId(player), browserId, ignoreCache);

    public void FocusBrowser(Player player, int browserId, bool focused)
        => CefInterop.Cef_FocusBrowser(PlayerToId(player), browserId, focused);

    public void EnableDevTools(Player player, int browserId, bool enabled)
        => CefInterop.Cef_EnableDevTools(PlayerToId(player), browserId, enabled);

    public void AttachBrowserToObject(Player player, int browserId, int objectId)
        => CefInterop.Cef_AttachBrowserToObject(PlayerToId(player), browserId, objectId);

    public void DetachBrowserFromObject(Player player, int browserId, int objectId)
        => CefInterop.Cef_DetachBrowserFromObject(PlayerToId(player), browserId, objectId);

    public void SetBrowserMuted(Player player, int browserId, bool muted)
        => CefInterop.Cef_SetBrowserMuted(PlayerToId(player), browserId, muted);

    public void SetBrowserAudioMode(Player player, int browserId, CefAudioMode mode)
        => CefInterop.Cef_SetBrowserAudioMode(PlayerToId(player), browserId, (int)mode);

    public void SetBrowserAudioSettings(Player player, int browserId, float maxDistance, float referenceDistance)
        => CefInterop.Cef_SetBrowserAudioSettings(PlayerToId(player), browserId, maxDistance, referenceDistance);

    public void ToggleHudComponent(Player player, CefHudComponent component, bool toggle)
        => CefInterop.Cef_ToggleHudComponent(PlayerToId(player), (int)component, toggle);

    public void ToggleSpawnScreen(Player player, bool toggle)
        => CefInterop.Cef_ToggleSpawnScreen(PlayerToId(player), toggle);

    public void ClearChat(Player player) => CefInterop.Cef_ClearChat(PlayerToId(player));
    public void ToggleChatInput(Player player, bool toggle)
        => CefInterop.Cef_ToggleChatInput(PlayerToId(player), toggle);
    public bool IsChatInputOpen(Player player) => CefInterop.Cef_IsChatInputOpen(PlayerToId(player));

    public void SetKeyCapture(Player player, bool enabled)
        => CefInterop.Cef_SetKeyCapture(PlayerToId(player), enabled);
    public void EnableKey(Player player, int virtualKey, bool enabled)
        => CefInterop.Cef_EnableKey(PlayerToId(player), virtualKey, enabled);

    public void ExitGame(Player player) => CefInterop.Cef_ExitGame(PlayerToId(player));

    // -1 = no player / NPC, иначе native playerid через IdProvider.
    private static int PlayerToId(Player? player) => player is { IsComponentAlive: true } ? player.Id : -1;
}

/// <summary>
/// Статические хелперы для extension-методов и других мест, где нет доступа к DI.
/// Тонкий слой поверх <see cref="CefService"/> — не держит состояния,
/// зовёт <see cref="Interop.CefInterop"/> напрямую.
/// </summary>
public static class CefGlobal
{
    /// <summary>Загружен ли Cef.dll в открытом режиме.</summary>
    public static bool IsAvailable => Interop.CefInterop.Cef_IsAvailable();

    /// <summary>Установил ли клиент CEF-плагин (handshake прошёл).</summary>
    public static bool PlayerHasPlugin(int playerId) => Interop.CefInterop.Cef_PlayerHasPlugin(playerId);
}
