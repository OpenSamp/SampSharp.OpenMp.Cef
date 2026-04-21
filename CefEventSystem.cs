using System;
using System.Runtime.InteropServices;
using System.Text;
using SampSharp.Cef.Entities.Interop;
using SampSharp.Entities;
using SampSharp.OpenMp.Core;
using SampSharp.OpenMp.Core.Api;

namespace SampSharp.Cef.Entities;

/// <summary>
/// Мост CEF-событий в ECS. Регистрирует UnmanagedCallersOnly-методы как
/// callback'и в SampSharp.Cef.dll, которые синхронно дёргаются из Cef.dll
/// (omp-cef форк) на open.mp tick'е. На каждое событие вызывается
/// <c>IEventDispatcher.Invoke(...)</c>.
///
/// Event-имена:
///   OnCefInitialize(EntityId player, bool success, CefInitReason reason, string message)
///   OnCefReady(EntityId player)
///   OnCefBrowserCreated(EntityId player, int browserId, bool success, CefCreateStatus code, string reason)
///   OnCefDownloadStart(EntityId player)
///   OnCefDownloadFinish(EntityId player)
///   OnCefPressKey(EntityId player, int key, int scancode, int modifiers, bool down, bool repeat)
///   OnCefChatInputState(EntityId player, bool open)
///   OnCefEvent(EntityId player, int browserId, string name, CefArg[] args)
/// </summary>
internal sealed class CefEventSystem : ISystem
{
    private static IEventDispatcher? _dispatcher;
    private static IOmpEntityProvider? _entityProvider;
    private static bool _registered;
    private static readonly object _sync = new();

    public CefEventSystem(IEventDispatcher dispatcher, IOmpEntityProvider entityProvider,
        SampSharpEnvironment environment)
    {
        lock (_sync)
        {
            _dispatcher = dispatcher;
            _entityProvider = entityProvider;
            SampSharpEnvironmentAccessor.Bind(environment);
            if (_registered) return;
            RegisterCallbacks();
            _registered = true;
        }
    }

    private static unsafe void RegisterCallbacks()
    {
        CefInterop.Cef_SetCallback_Initialize(&OnInitialize);
        CefInterop.Cef_SetCallback_Ready(&OnReady);
        CefInterop.Cef_SetCallback_BrowserCreated(&OnBrowserCreated);
        CefInterop.Cef_SetCallback_DownloadStart(&OnDownloadStart);
        CefInterop.Cef_SetCallback_DownloadFinish(&OnDownloadFinish);
        CefInterop.Cef_SetCallback_PressKey(&OnPressKey);
        CefInterop.Cef_SetCallback_ChatInputState(&OnChatInputState);
        CefInterop.Cef_SetCallback_Event(&OnEvent);
    }

    private static EntityId PlayerEntity(int playerId)
    {
        if (_entityProvider is null) return default;
        try
        {
            var pool = SampSharpEnvironmentAccessor.TryGetPlayerPool();
            if (pool is null) return default;
            var player = pool.Value.Get(playerId);
            if (!player.HasValue) return default;
            return _entityProvider.GetEntity(player);
        }
        catch { return default; }
    }

    private static unsafe string Utf8(byte* p)
    {
        if (p == null) return string.Empty;
        int len = 0;
        while (p[len] != 0) len++;
        return Encoding.UTF8.GetString(p, len);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static unsafe void OnInitialize(int playerId, byte success, int reason, byte* message) =>
        _dispatcher?.Invoke("OnCefInitialize",
            PlayerEntity(playerId), success != 0, (CefInitReason)reason, Utf8(message));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static void OnReady(int playerId) =>
        _dispatcher?.Invoke("OnCefReady", PlayerEntity(playerId));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static unsafe void OnBrowserCreated(int playerId, int browserId, byte success, int code, byte* reason) =>
        _dispatcher?.Invoke("OnCefBrowserCreated",
            PlayerEntity(playerId), browserId, success != 0, (CefCreateStatus)code, Utf8(reason));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static void OnDownloadStart(int playerId) =>
        _dispatcher?.Invoke("OnCefDownloadStart", PlayerEntity(playerId));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static void OnDownloadFinish(int playerId) =>
        _dispatcher?.Invoke("OnCefDownloadFinish", PlayerEntity(playerId));

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static void OnPressKey(int playerId, int key, int scancode, int modifiers, byte down, byte repeat) =>
        _dispatcher?.Invoke("OnCefPressKey",
            PlayerEntity(playerId), key, scancode, modifiers, down != 0, repeat != 0);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static void OnChatInputState(int playerId, byte open) =>
        _dispatcher?.Invoke("OnCefChatInputState", PlayerEntity(playerId), open != 0);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static unsafe void OnEvent(int playerId, int browserId, byte* name, int argCount, CefArgMarshal* args)
    {
        string evName = Utf8(name);
        var parsed = new CefArg[argCount < 0 ? 0 : argCount];
        for (int i = 0; i < parsed.Length; i++)
        {
            var a = args[i];
            parsed[i] = (CefArgType)a.Type switch
            {
                CefArgType.String => CefArg.Str(Utf8(a.StringPtr)),
                CefArgType.Integer => CefArg.Int(a.IntValue),
                CefArgType.Float => CefArg.Float(a.FloatValue),
                CefArgType.Bool => CefArg.Bool(a.BoolValue != 0),
                _ => default,
            };
        }
        _dispatcher?.Invoke("OnCefEvent", PlayerEntity(playerId), browserId, evName, parsed);
    }
}
