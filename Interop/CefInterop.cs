using System.Runtime.InteropServices;

namespace SampSharp.Cef.Entities.Interop;

/// <summary>
/// P/Invoke-биндинги к C-exports в SampSharp.Cef.dll, которые через
/// <c>ICefComponent</c> IExtension форвардят вызовы в Cef.dll (omp-cef форк).
///
/// Если Cef.dll не загружен, все Cef_* возвращают 0/false. Проверить
/// через <see cref="Cef_IsAvailable"/>.
/// </summary>
internal static partial class CefInterop
{
    // SampSharp.Cef.dll — наш собственный open.mp-компонент-мост, загружается open.mp
    // сервером из env/components/. LoadLibrary находит модули по короткому имени.
    private const string Lib = "SampSharp.Cef";

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool Cef_IsAvailable();

    // ----- Session status -----------------------------------------------------------

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool Cef_PlayerHasPlugin(int playerId);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void Cef_AddResource(string resourceName);

    // ----- Browser lifecycle --------------------------------------------------------

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void Cef_CreateBrowser(int playerId, int browserId, string url,
        [MarshalAs(UnmanagedType.I1)] bool focused,
        [MarshalAs(UnmanagedType.I1)] bool controlsChat);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void Cef_CreateWorldBrowser(int playerId, int browserId, string url,
        string textureName, float width, float height);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void Cef_CreateWorld2DBrowser(int playerId, int browserId, string url,
        float worldX, float worldY, float worldZ,
        float width, float height, float offsetZ, float pivotX, float pivotY);

    [LibraryImport(Lib)]
    internal static partial void Cef_SetWorld2DBrowserPos(int playerId, int browserId,
        float worldX, float worldY, float worldZ);

    [LibraryImport(Lib)]
    internal static partial void Cef_SetBrowserVisible(int playerId, int browserId,
        [MarshalAs(UnmanagedType.I1)] bool visible);

    [LibraryImport(Lib)]
    internal static partial void Cef_DestroyBrowser(int playerId, int browserId);

    // ----- Events -------------------------------------------------------------------
    // FFI: plain pointer to array of ints and CefArg structs. We build the buffer in
    // managed code and pin via fixed statement, no marshaller magic required.

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static unsafe partial void Cef_RegisterEvent(string name, string callback,
        int typeCount, byte* types);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    internal static unsafe partial void Cef_EmitEvent(int playerId, int browserId, string name,
        int argCount, CefArgMarshal* args);

    // ----- Browser utilities --------------------------------------------------------

    [LibraryImport(Lib)]
    internal static partial void Cef_ReloadBrowser(int playerId, int browserId,
        [MarshalAs(UnmanagedType.I1)] bool ignoreCache);

    [LibraryImport(Lib)]
    internal static partial void Cef_FocusBrowser(int playerId, int browserId,
        [MarshalAs(UnmanagedType.I1)] bool focused);

    [LibraryImport(Lib)]
    internal static partial void Cef_EnableDevTools(int playerId, int browserId,
        [MarshalAs(UnmanagedType.I1)] bool enabled);

    [LibraryImport(Lib)]
    internal static partial void Cef_AttachBrowserToObject(int playerId, int browserId, int objectId);

    [LibraryImport(Lib)]
    internal static partial void Cef_DetachBrowserFromObject(int playerId, int browserId, int objectId);

    // ----- Audio --------------------------------------------------------------------

    [LibraryImport(Lib)]
    internal static partial void Cef_SetBrowserMuted(int playerId, int browserId,
        [MarshalAs(UnmanagedType.I1)] bool muted);

    [LibraryImport(Lib)]
    internal static partial void Cef_SetBrowserAudioMode(int playerId, int browserId, int mode);

    [LibraryImport(Lib)]
    internal static partial void Cef_SetBrowserAudioSettings(int playerId, int browserId,
        float maxDistance, float referenceDistance);

    // ----- HUD / chat ---------------------------------------------------------------

    [LibraryImport(Lib)]
    internal static partial void Cef_ToggleHudComponent(int playerId, int componentId,
        [MarshalAs(UnmanagedType.I1)] bool toggle);

    [LibraryImport(Lib)]
    internal static partial void Cef_ToggleSpawnScreen(int playerId,
        [MarshalAs(UnmanagedType.I1)] bool toggle);

    [LibraryImport(Lib)]
    internal static partial void Cef_ClearChat(int playerId);

    [LibraryImport(Lib)]
    internal static partial void Cef_ToggleChatInput(int playerId,
        [MarshalAs(UnmanagedType.I1)] bool toggle);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static partial bool Cef_IsChatInputOpen(int playerId);

    // ----- Key capture --------------------------------------------------------------

    [LibraryImport(Lib)]
    internal static partial void Cef_SetKeyCapture(int playerId,
        [MarshalAs(UnmanagedType.I1)] bool enabled);

    [LibraryImport(Lib)]
    internal static partial void Cef_EnableKey(int playerId, int key,
        [MarshalAs(UnmanagedType.I1)] bool enabled);

    // ----- Misc ---------------------------------------------------------------------

    [LibraryImport(Lib)]
    internal static partial void Cef_ExitGame(int playerId);

    // ----- Event callback registration ---------------------------------------------
    //
    // Native side exposes 8 callback setters. The C# entry points live in
    // CefEventSystem.cs — this interop layer only cares about pointer plumbing.

    [LibraryImport(Lib)] internal static unsafe partial void Cef_SetCallback_Initialize(
        delegate* unmanaged[Cdecl]<int, byte, int, byte*, void> fn);

    [LibraryImport(Lib)] internal static unsafe partial void Cef_SetCallback_Ready(
        delegate* unmanaged[Cdecl]<int, void> fn);

    [LibraryImport(Lib)] internal static unsafe partial void Cef_SetCallback_BrowserCreated(
        delegate* unmanaged[Cdecl]<int, int, byte, int, byte*, void> fn);

    [LibraryImport(Lib)] internal static unsafe partial void Cef_SetCallback_DownloadStart(
        delegate* unmanaged[Cdecl]<int, void> fn);

    [LibraryImport(Lib)] internal static unsafe partial void Cef_SetCallback_DownloadFinish(
        delegate* unmanaged[Cdecl]<int, void> fn);

    [LibraryImport(Lib)] internal static unsafe partial void Cef_SetCallback_PressKey(
        delegate* unmanaged[Cdecl]<int, int, int, int, byte, byte, void> fn);

    [LibraryImport(Lib)] internal static unsafe partial void Cef_SetCallback_ChatInputState(
        delegate* unmanaged[Cdecl]<int, byte, void> fn);

    [LibraryImport(Lib)] internal static unsafe partial void Cef_SetCallback_Event(
        delegate* unmanaged[Cdecl]<int, int, byte*, int, CefArgMarshal*, void> fn);
}

/// <summary>
/// Зеркало C-структуры <c>CefArg</c> из <c>cef_extension_api.hpp</c>. Строки
/// передаются как UTF-8 nul-terminated pointers, а не как managed string — обе
/// стороны должны держать буфер живым на время вызова.
///
/// Layout (x64, natural alignment): Type(1)+pad(7) = 8, StringPtr(8) = 8,
/// IntValue(4), FloatValue(4), BoolValue(1)+pad(7) = 8. Total = 32 bytes.
/// Explicit offsets pin this so the C++ side matches 1:1.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 32)]
internal unsafe struct CefArgMarshal
{
    [FieldOffset(0)]  public byte Type;        // 0=String, 1=Integer, 2=Float, 3=Bool
    [FieldOffset(8)]  public byte* StringPtr;  // valid when Type == String
    [FieldOffset(16)] public int IntValue;
    [FieldOffset(20)] public float FloatValue;
    [FieldOffset(24)] public byte BoolValue;
}
