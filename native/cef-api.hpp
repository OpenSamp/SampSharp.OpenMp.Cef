// Mirror of omp-cef/src/server/common/cef_extension_api.hpp.
// Keep in sync with the omp-cef fork; defines the ABI between Cef.dll and this component.

#pragma once

#include <component.hpp>
#include <cstdint>
#include <vector>

// IExtension UID (the component UID lives on a neighbouring slot).
constexpr UID kCefComponentUID = UID(0xD9607C6728B33464ULL);
constexpr UID kCefExtensionUID = UID(0xD9607C6728B33465ULL);

enum class CefArgType : uint8_t
{
    String = 0,
    Integer = 1,
    Float = 2,
    Bool = 3,
};

struct CefArg
{
    CefArgType type;
    const char* stringValue;
    int intValue;
    float floatValue;
    bool boolValue;
};

struct ICefEventHandler
{
    virtual ~ICefEventHandler() = default;
    virtual void onCefInitialize(int playerid, bool success, int reason, const char* message) {}
    virtual void onCefReady(int playerid) {}
    virtual void onCefBrowserCreated(int playerid, int browserId, bool success, int code, const char* reason) {}
    virtual void onCefDownloadStart(int playerid) {}
    virtual void onCefDownloadFinish(int playerid) {}
    virtual void onCefPressKey(int playerid, int key, int scancode, int modifiers, bool down, bool repeat) {}
    virtual void onCefChatInputState(int playerid, bool open) {}
    virtual void onCefEvent(int playerid, int browserId, const char* name, int argCount, const CefArg* args) {}
};

struct ICefComponent : public IExtension
{
    PROVIDE_EXT_UID(kCefExtensionUID)

    virtual bool playerHasPlugin(int playerid) = 0;
    virtual void addResource(const char* resourceName) = 0;

    virtual void createBrowser(int playerid, int browserid, const char* url,
        bool focused, bool controlsChat) = 0;
    virtual void createWorldBrowser(int playerid, int browserid, const char* url,
        const char* textureName, float width, float height) = 0;
    virtual void createWorld2DBrowser(int playerid, int browserid, const char* url,
        float worldX, float worldY, float worldZ, float width, float height,
        float offsetZ, float pivotX, float pivotY) = 0;
    virtual void setWorld2DBrowserPos(int playerid, int browserid,
        float worldX, float worldY, float worldZ) = 0;
    virtual void setBrowserVisible(int playerid, int browserid, bool visible) = 0;
    virtual void destroyBrowser(int playerid, int browserid) = 0;

    virtual void registerEvent(const char* name, const char* callback,
        int typeCount, const CefArgType* types) = 0;
    virtual void emitEvent(int playerid, int browserid, const char* name,
        int argCount, const CefArg* args) = 0;

    virtual void reloadBrowser(int playerid, int browserid, bool ignoreCache) = 0;
    virtual void focusBrowser(int playerid, int browserid, bool focused) = 0;
    virtual void enableDevTools(int playerid, int browserid, bool enabled) = 0;

    virtual void attachBrowserToObject(int playerid, int browserid, int objectid) = 0;
    virtual void detachBrowserFromObject(int playerid, int browserid, int objectid) = 0;

    virtual void setBrowserMuted(int playerid, int browserid, bool muted) = 0;
    virtual void setBrowserAudioMode(int playerid, int browserid, int mode) = 0;
    virtual void setBrowserAudioSettings(int playerid, int browserid,
        float maxDistance, float referenceDistance) = 0;

    virtual void toggleHudComponent(int playerid, int componentid, bool toggle) = 0;
    virtual void toggleSpawnScreen(int playerid, bool toggle) = 0;
    virtual void clearChat(int playerid) = 0;
    virtual void toggleChatInput(int playerid, bool toggle) = 0;
    virtual bool isChatInputOpen(int playerid) = 0;

    virtual void setKeyCapture(int playerid, bool enabled) = 0;
    virtual void enableKey(int playerid, int key, bool enabled) = 0;

    virtual void exitGame(int playerid) = 0;

    virtual void addEventHandler(ICefEventHandler* handler) = 0;
    virtual void removeEventHandler(ICefEventHandler* handler) = 0;

    void reset() override {}
};
