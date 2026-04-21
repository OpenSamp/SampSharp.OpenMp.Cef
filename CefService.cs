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

    public bool PlayerHasPlugin(EntityId player) => PlayerHasPlugin(EntityIdToPlayer(player));
    public bool PlayerHasPlugin(int playerId) => CefInterop.Cef_PlayerHasPlugin(playerId);

    public void AddResource(string resourceName) => CefInterop.Cef_AddResource(resourceName ?? "");

    public void CreateBrowser(EntityId player, int browserId, string url, bool focused, bool controlsChat = true)
        => CefInterop.Cef_CreateBrowser(EntityIdToPlayer(player), browserId, url ?? "", focused, controlsChat);

    public void CreateWorldBrowser(EntityId player, int browserId, string url, string textureName, float width, float height)
        => CefInterop.Cef_CreateWorldBrowser(EntityIdToPlayer(player), browserId, url ?? "", textureName ?? "", width, height);

    public void CreateWorld2DBrowser(EntityId player, int browserId, string url,
        float worldX, float worldY, float worldZ,
        float width = 0f, float height = 0f, float offsetZ = 0f, float pivotX = 0f, float pivotY = 0f)
        => CefInterop.Cef_CreateWorld2DBrowser(EntityIdToPlayer(player), browserId, url ?? "",
            worldX, worldY, worldZ, width, height, offsetZ, pivotX, pivotY);

    public void SetWorld2DBrowserPos(EntityId player, int browserId, float x, float y, float z)
        => CefInterop.Cef_SetWorld2DBrowserPos(EntityIdToPlayer(player), browserId, x, y, z);

    public void SetBrowserVisible(EntityId player, int browserId, bool visible)
        => CefInterop.Cef_SetBrowserVisible(EntityIdToPlayer(player), browserId, visible);

    public void DestroyBrowser(EntityId player, int browserId)
        => CefInterop.Cef_DestroyBrowser(EntityIdToPlayer(player), browserId);

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

    public unsafe void EmitEvent(EntityId player, int browserId, string name, params CefArg[] args)
    {
        int n = args?.Length ?? 0;
        if (n == 0)
        {
            CefInterop.Cef_EmitEvent(EntityIdToPlayer(player), browserId, name ?? "", 0, null);
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
            CefInterop.Cef_EmitEvent(EntityIdToPlayer(player), browserId, name ?? "", n, mp);
        }
    }

    public void ReloadBrowser(EntityId player, int browserId, bool ignoreCache = false)
        => CefInterop.Cef_ReloadBrowser(EntityIdToPlayer(player), browserId, ignoreCache);

    public void FocusBrowser(EntityId player, int browserId, bool focused)
        => CefInterop.Cef_FocusBrowser(EntityIdToPlayer(player), browserId, focused);

    public void EnableDevTools(EntityId player, int browserId, bool enabled)
        => CefInterop.Cef_EnableDevTools(EntityIdToPlayer(player), browserId, enabled);

    public void AttachBrowserToObject(EntityId player, int browserId, int objectId)
        => CefInterop.Cef_AttachBrowserToObject(EntityIdToPlayer(player), browserId, objectId);

    public void DetachBrowserFromObject(EntityId player, int browserId, int objectId)
        => CefInterop.Cef_DetachBrowserFromObject(EntityIdToPlayer(player), browserId, objectId);

    public void SetBrowserMuted(EntityId player, int browserId, bool muted)
        => CefInterop.Cef_SetBrowserMuted(EntityIdToPlayer(player), browserId, muted);

    public void SetBrowserAudioMode(EntityId player, int browserId, CefAudioMode mode)
        => CefInterop.Cef_SetBrowserAudioMode(EntityIdToPlayer(player), browserId, (int)mode);

    public void SetBrowserAudioSettings(EntityId player, int browserId, float maxDistance, float referenceDistance)
        => CefInterop.Cef_SetBrowserAudioSettings(EntityIdToPlayer(player), browserId, maxDistance, referenceDistance);

    public void ToggleHudComponent(EntityId player, CefHudComponent component, bool toggle)
        => CefInterop.Cef_ToggleHudComponent(EntityIdToPlayer(player), (int)component, toggle);

    public void ToggleSpawnScreen(EntityId player, bool toggle)
        => CefInterop.Cef_ToggleSpawnScreen(EntityIdToPlayer(player), toggle);

    public void ClearChat(EntityId player) => CefInterop.Cef_ClearChat(EntityIdToPlayer(player));
    public void ToggleChatInput(EntityId player, bool toggle)
        => CefInterop.Cef_ToggleChatInput(EntityIdToPlayer(player), toggle);
    public bool IsChatInputOpen(EntityId player) => CefInterop.Cef_IsChatInputOpen(EntityIdToPlayer(player));

    public void SetKeyCapture(EntityId player, bool enabled)
        => CefInterop.Cef_SetKeyCapture(EntityIdToPlayer(player), enabled);
    public void EnableKey(EntityId player, int virtualKey, bool enabled)
        => CefInterop.Cef_EnableKey(EntityIdToPlayer(player), virtualKey, enabled);

    public void ExitGame(EntityId player) => CefInterop.Cef_ExitGame(EntityIdToPlayer(player));

    /// <summary>
    /// <see cref="EntityId"/> — это Guid; для CEF нужен игровой <c>playerid</c>,
    /// тот же, что в open.mp. Берётся из <see cref="EntityId.Handle"/> — это
    /// совпадает с id игрока при конструировании Player entity.
    /// </summary>
    private static int EntityIdToPlayer(EntityId id) => id.IsEmpty ? -1 : id.Handle;
}
