# BepInEx.Renderer

BepInEx packages for modding Resonite.

## Packages

- **BepInExRenderer** — BepInEx 5 (Mono, LTS) for Resonite's Unity renderer. Dependency for renderer mods.
- **BepInExRenderer.IL2CPP** — BepInEx 6 BE (IL2CPP) for Resonite. Dependency for IL2CPP renderer mods.

Both packages are built from the latest upstream releases automatically.

## Building

```bash
dotnet tool restore
dotnet cake              # Build both packages
dotnet cake --target=BuildMono    # Build mono only
dotnet cake --target=BuildIL2CPP  # Build IL2CPP only
```

## Links

- [Thunderstore (Mono)](https://thunderstore.io/c/resonite/p/ResoniteModding/BepInExRenderer/)
- [Thunderstore (IL2CPP)](https://thunderstore.io/c/resonite/p/ResoniteModding/BepInExRenderer.IL2CPP/)
- [RenderiteHook](https://github.com/ResoniteModding/RenderiteHook)
