# SampSharp.OpenMp.Cef

Managed C# bindings for [omp-cef](https://github.com/Pandreex/omp-cef) on open.mp x64,
for gamemodes running on the SampSharp open.mp host. Lets C# code create and drive CEF
browsers on the client — fullscreen HUD overlays, browsers painted onto world object
textures, world-anchored 2D surfaces — and exchange events with the JavaScript running
inside them.

## Architecture

open.mp loads three independent components; this repository provides the middle one plus
the C# bindings on top of it.

```
┌──────────────────────────────────────────────────────────────────────┐
│  C# gamemode                                                         │
│     ICefService, ICefEventHandler                                    │
└──────────────────────────────────────────────────────────────────────┘
                               │   P/Invoke
                               ▼
┌──────────────────────────────────────────────────────────────────────┐
│  SampSharp.Cef.dll  (this repo, native/)                             │
│     pure C-API shim: C exports + function-pointer callbacks          │
│     queryExtension<ICefComponent>() at onInit                        │
└──────────────────────────────────────────────────────────────────────┘
                               │   direct C++ virtual calls
                               ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Cef.dll  (omp-cef fork, provides ICefComponent)                     │
└──────────────────────────────────────────────────────────────────────┘
```

The shim deliberately has **no link-time dependency on `SampSharp.dll`**. It is loaded by
open.mp on its own and only ever talks to `Cef.dll`; the managed side reaches it by
P/Invoke. That keeps the CEF bridge working across SampSharp host upgrades.

## Runtime dependencies

| Component           | Where from                                                  |
|---------------------|-------------------------------------------------------------|
| `Cef.dll` / `.so`   | omp-cef fork — provides the `ICefComponent` extension        |
| `SampSharp.Cef.dll` | Built from `native/` in this repository                      |
| `SampSharp.dll`     | `SampSharp/src/sampsharp-component/` — hosts the .NET runtime |
| .NET 10 runtime     | System-wide                                                  |

All three DLLs go in the server's `components/` directory.

If `Cef.dll` is not loaded at server start, the bridge degrades quietly: every method on
`ICefService` becomes a no-op and `IsAvailable` returns `false`. Check it before assuming a
browser exists rather than relying on exceptions.

## Wiring

```csharp
// ConfigureServices — before AddSystemsInAssembly
services.AddCef();

// ECS builder
builder.EnableCefEvents();
```

`AddCef` registers `ICefService` and the event system that attaches the native callbacks at
startup. Order matters: the service first, then the event system.

## Surface

- **Browsers** — screen-space, world-texture (painted onto an object's texture) and
  world-anchored 2D; create, destroy, show/hide, focus, reload, attach to objects
- **JavaScript bridge** — `RegisterEvent` declares an event with a typed signature that JS
  can emit back to the server; `EmitEvent` pushes into a specific browser. Arguments are
  carried by `CefArg` / `CefArgType`
- **Audio** — mute, audio mode, distance-based settings for world browsers
- **Client chrome** — toggle individual HUD components, the spawn screen, the chat input;
  clear chat; query whether chat input is open
- **Input** — key capture on/off, per-virtual-key enable, `ExitGame`
- **Resources** — `AddResource(name)` registers `scriptfiles/cef/<name>` for clients to
  download. Call it during startup, not per-player

Plugin presence is per-player: `PlayerHasPlugin` tells you whether that client actually has
the CEF plugin installed, which is not the same as the server having `Cef.dll`.

## Building

Two artifacts, built separately:

```bash
# managed bindings
dotnet build SampSharp.OpenMp.Cef.csproj

# native shim
cmake -B build -S . -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
```

Both need the SampSharp repository checked out alongside this one — the csproj references
`SampSharp.OpenMp.Core` and `SampSharp.OpenMp.Entities` by relative path, and CMake takes
the open.mp SDK from `SampSharp/external/sdk`. Either nesting works: SampSharp as a direct
sibling, or one level up (`src/SampSharp` with this repo under `src/submodules/`). Override
with `-DOMP_SDK_DIR=<path>` if your layout differs.

## License

Apache-2.0 -- see [LICENSE](LICENSE). That covers the bindings in this
repository; omp-cef itself is licensed separately by its authors.

---

Powered by [vs-rp.org](https://vs-rp.org)
