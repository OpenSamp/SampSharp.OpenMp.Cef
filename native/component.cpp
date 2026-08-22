// SampSharp.Cef — open.mp component that bridges omp-cef (exposed as ICefComponent
// IExtension) to managed .NET (via C-exports + function-pointer callbacks).
//
// Architecture:
//   open.mp loads 3 components independently:
//     - Cef.dll             (omp-cef fork, provides ICefComponent extension)
//     - SampSharp.dll       (hosts the .NET runtime + gamemode)
//     - SampSharp.Cef.dll   (THIS) — pure C-API shim
//
//   At onInit this component queryComponent's Cef.dll and registers a C++ event handler.
//   Managed side (SampSharp.OpenMp.Cef.csproj) P/Invokes into this DLL's exports.
//
//   No dependency on SampSharp.dll — entirely decoupled.

#include <sdk.hpp>

#include "cef-api.hpp"

namespace
{
    // Component identity. Different from both Cef.dll and SampSharp.dll UIDs.
    constexpr UID kSampSharpCefUID = UID(0x537353437366ff01ULL); // "SsSCf\xFF\x01"

    ICefComponent* g_cef = nullptr;

    // ---- Managed callback function pointers ----
    using FnInitialize      = void (*)(int /*pid*/, unsigned char /*success*/, int /*reason*/, const char* /*message*/);
    using FnReady           = void (*)(int);
    using FnBrowserCreated  = void (*)(int /*pid*/, int /*browser*/, unsigned char /*success*/, int /*code*/, const char* /*reason*/);
    using FnPlayerOnly      = void (*)(int);
    using FnPressKey        = void (*)(int /*pid*/, int /*key*/, int /*scan*/, int /*mod*/, unsigned char /*down*/, unsigned char /*repeat*/);
    using FnChatInputState  = void (*)(int /*pid*/, unsigned char /*open*/);
    using FnCefEvent        = void (*)(int /*pid*/, int /*browser*/, const char* /*name*/, int /*argc*/, const CefArg* /*args*/);

    FnInitialize     cb_initialize     = nullptr;
    FnReady          cb_ready          = nullptr;
    FnBrowserCreated cb_browserCreated = nullptr;
    FnPlayerOnly     cb_downloadStart  = nullptr;
    FnPlayerOnly     cb_downloadFinish = nullptr;
    FnPressKey       cb_pressKey       = nullptr;
    FnChatInputState cb_chatInputState = nullptr;
    FnCefEvent       cb_cefEvent       = nullptr;

    class Handler : public ICefEventHandler
    {
    public:
        void onCefInitialize(int p, bool s, int r, const char* m) override
            { if (cb_initialize) cb_initialize(p, s ? 1u : 0u, r, m ? m : ""); }
        void onCefReady(int p) override
            { if (cb_ready) cb_ready(p); }
        void onCefBrowserCreated(int p, int b, bool s, int c, const char* r) override
            { if (cb_browserCreated) cb_browserCreated(p, b, s ? 1u : 0u, c, r ? r : ""); }
        void onCefDownloadStart(int p) override
            { if (cb_downloadStart) cb_downloadStart(p); }
        void onCefDownloadFinish(int p) override
            { if (cb_downloadFinish) cb_downloadFinish(p); }
        void onCefPressKey(int p, int k, int sc, int mod, bool down, bool rep) override
            { if (cb_pressKey) cb_pressKey(p, k, sc, mod, down ? 1u : 0u, rep ? 1u : 0u); }
        void onCefChatInputState(int p, bool open) override
            { if (cb_chatInputState) cb_chatInputState(p, open ? 1u : 0u); }
        void onCefEvent(int p, int b, const char* name, int argc, const CefArg* args) override
            { if (cb_cefEvent) cb_cefEvent(p, b, name ? name : "", argc, args); }
    };

    Handler g_handler;
    bool g_handlerRegistered = false;

    class SampSharpCefComponent final : public IComponent
    {
    public:
        PROVIDE_UID(kSampSharpCefUID)

        StringView componentName() const override { return "SampSharp.Cef"; }
        SemanticVersion componentVersion() const override { return SemanticVersion(1, 0, 0, 0); }

        void onLoad(ICore* c) override { core_ = c; }

        void onInit(IComponentList* components) override
        {
            if (!components) return;
            IComponent* cefComp = components->queryComponent(kCefComponentUID);
            if (!cefComp)
            {
                if (core_) core_->logLn(LogLevel::Warning,
                    "SampSharp.Cef: Cef.dll not loaded; all Cef_* calls will be no-op");
                return;
            }
            g_cef = queryExtension<ICefComponent>(cefComp);
            if (!g_cef)
            {
                if (core_) core_->logLn(LogLevel::Warning,
                    "SampSharp.Cef: Cef.dll loaded but ICefComponent extension missing; use the OpenSamp omp-cef fork");
                return;
            }
            g_cef->addEventHandler(&g_handler);
            g_handlerRegistered = true;
            if (core_) core_->printLn("SampSharp.Cef: bound to Cef.dll ICefComponent extension");
        }

        void free() override
        {
            if (g_cef && g_handlerRegistered)
            {
                g_cef->removeEventHandler(&g_handler);
                g_handlerRegistered = false;
            }
            g_cef = nullptr;
            delete this;
        }

        void reset() override {}

    private:
        ICore* core_ = nullptr;
    };

    SampSharpCefComponent* g_componentInstance = nullptr;
}

COMPONENT_ENTRY_POINT()
{
    if (!g_componentInstance) g_componentInstance = new SampSharpCefComponent();
    return g_componentInstance;
}

// ============================================================================
// C-exports: called from managed C# via P/Invoke (CefInterop.cs).
// ============================================================================

extern "C" SDK_EXPORT bool __CDECL Cef_IsAvailable() { return g_cef != nullptr; }

extern "C" SDK_EXPORT bool __CDECL Cef_PlayerHasPlugin(int p) { return g_cef && g_cef->playerHasPlugin(p); }
extern "C" SDK_EXPORT void __CDECL Cef_AddResource(const char* name) { if (g_cef) g_cef->addResource(name ? name : ""); }

extern "C" SDK_EXPORT void __CDECL Cef_CreateBrowser(int p, int b, const char* url,
    bool focused, bool controlsChat)
{ if (g_cef) g_cef->createBrowser(p, b, url ? url : "", focused, controlsChat); }

extern "C" SDK_EXPORT void __CDECL Cef_CreateWorldBrowser(int p, int b, const char* url,
    const char* tex, float w, float h)
{ if (g_cef) g_cef->createWorldBrowser(p, b, url ? url : "", tex ? tex : "", w, h); }

extern "C" SDK_EXPORT void __CDECL Cef_CreateWorld2DBrowser(int p, int b, const char* url,
    float wx, float wy, float wz, float w, float h, float oz, float px, float py)
{ if (g_cef) g_cef->createWorld2DBrowser(p, b, url ? url : "", wx, wy, wz, w, h, oz, px, py); }

extern "C" SDK_EXPORT void __CDECL Cef_SetWorld2DBrowserPos(int p, int b, float x, float y, float z)
{ if (g_cef) g_cef->setWorld2DBrowserPos(p, b, x, y, z); }

extern "C" SDK_EXPORT void __CDECL Cef_SetBrowserVisible(int p, int b, bool v)
{ if (g_cef) g_cef->setBrowserVisible(p, b, v); }

extern "C" SDK_EXPORT void __CDECL Cef_DestroyBrowser(int p, int b)
{ if (g_cef) g_cef->destroyBrowser(p, b); }

extern "C" SDK_EXPORT void __CDECL Cef_RegisterEvent(const char* name, const char* callback,
    int typeCount, const unsigned char* types)
{
    if (!g_cef) return;
    g_cef->registerEvent(name ? name : "", callback ? callback : "", typeCount,
        reinterpret_cast<const CefArgType*>(types));
}

extern "C" SDK_EXPORT void __CDECL Cef_EmitEvent(int p, int b, const char* name,
    int argCount, const CefArg* args)
{ if (g_cef) g_cef->emitEvent(p, b, name ? name : "", argCount, args); }

extern "C" SDK_EXPORT void __CDECL Cef_ReloadBrowser(int p, int b, bool ic)
{ if (g_cef) g_cef->reloadBrowser(p, b, ic); }

extern "C" SDK_EXPORT void __CDECL Cef_FocusBrowser(int p, int b, bool f)
{ if (g_cef) g_cef->focusBrowser(p, b, f); }

extern "C" SDK_EXPORT void __CDECL Cef_EnableDevTools(int p, int b, bool e)
{ if (g_cef) g_cef->enableDevTools(p, b, e); }

extern "C" SDK_EXPORT void __CDECL Cef_AttachBrowserToObject(int p, int b, int o)
{ if (g_cef) g_cef->attachBrowserToObject(p, b, o); }

extern "C" SDK_EXPORT void __CDECL Cef_DetachBrowserFromObject(int p, int b, int o)
{ if (g_cef) g_cef->detachBrowserFromObject(p, b, o); }

extern "C" SDK_EXPORT void __CDECL Cef_SetBrowserMuted(int p, int b, bool m)
{ if (g_cef) g_cef->setBrowserMuted(p, b, m); }

extern "C" SDK_EXPORT void __CDECL Cef_SetBrowserAudioMode(int p, int b, int m)
{ if (g_cef) g_cef->setBrowserAudioMode(p, b, m); }

extern "C" SDK_EXPORT void __CDECL Cef_SetBrowserAudioSettings(int p, int b, float md, float rd)
{ if (g_cef) g_cef->setBrowserAudioSettings(p, b, md, rd); }

extern "C" SDK_EXPORT void __CDECL Cef_ToggleHudComponent(int p, int c, bool t)
{ if (g_cef) g_cef->toggleHudComponent(p, c, t); }

extern "C" SDK_EXPORT void __CDECL Cef_ToggleSpawnScreen(int p, bool t)
{ if (g_cef) g_cef->toggleSpawnScreen(p, t); }

extern "C" SDK_EXPORT void __CDECL Cef_ClearChat(int p)
{ if (g_cef) g_cef->clearChat(p); }

extern "C" SDK_EXPORT void __CDECL Cef_ToggleChatInput(int p, bool t)
{ if (g_cef) g_cef->toggleChatInput(p, t); }

extern "C" SDK_EXPORT bool __CDECL Cef_IsChatInputOpen(int p)
{ return g_cef && g_cef->isChatInputOpen(p); }

extern "C" SDK_EXPORT void __CDECL Cef_SetKeyCapture(int p, bool e)
{ if (g_cef) g_cef->setKeyCapture(p, e); }

extern "C" SDK_EXPORT void __CDECL Cef_EnableKey(int p, int k, bool e)
{ if (g_cef) g_cef->enableKey(p, k, e); }

extern "C" SDK_EXPORT void __CDECL Cef_ExitGame(int p)
{ if (g_cef) g_cef->exitGame(p); }

// ============================================================================
// Callback-registration exports — called from C# at startup (CefEventSystem.cs).
// ============================================================================

extern "C" SDK_EXPORT void __CDECL Cef_SetCallback_Initialize(FnInitialize fn)         { cb_initialize     = fn; }
extern "C" SDK_EXPORT void __CDECL Cef_SetCallback_Ready(FnReady fn)                   { cb_ready          = fn; }
extern "C" SDK_EXPORT void __CDECL Cef_SetCallback_BrowserCreated(FnBrowserCreated fn) { cb_browserCreated = fn; }
extern "C" SDK_EXPORT void __CDECL Cef_SetCallback_DownloadStart(FnPlayerOnly fn)      { cb_downloadStart  = fn; }
extern "C" SDK_EXPORT void __CDECL Cef_SetCallback_DownloadFinish(FnPlayerOnly fn)     { cb_downloadFinish = fn; }
extern "C" SDK_EXPORT void __CDECL Cef_SetCallback_PressKey(FnPressKey fn)             { cb_pressKey       = fn; }
extern "C" SDK_EXPORT void __CDECL Cef_SetCallback_ChatInputState(FnChatInputState fn) { cb_chatInputState = fn; }
extern "C" SDK_EXPORT void __CDECL Cef_SetCallback_Event(FnCefEvent fn)                { cb_cefEvent       = fn; }
