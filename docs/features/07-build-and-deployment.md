# Build & Deployment

## Project Structure

```
isma-ui-avalonia-2/
├── isma-ui-dotnet.slnx                    # Solution file (4 projects)
├── Directory.Build.props                   # Global properties (LangVersion, Nullable, etc.)
├── Directory.Packages.props               # Central package version management
├── run.sh                                  # Launcher script
├── dotnet-tools.json                       # .NET CLI tools
├── src/
│   ├── ISMA.Domain/                       # Domain models, DTOs, contracts, conversion
│   ├── ISMA.ViewModels/                   # MVVM ViewModels, converters, services
│   ├── ISMA.Infrastructure/               # Server communication, file storage, chart viewer
│   └── ISMA.App/                          # UI views, controls, app entry point, DI
└── tests/
    └── ISMA.Tests/                        # Unit tests
```

## Assembly Configuration

### ISMA.Domain

**File:** `src/ISMA.Domain/ISMA.Domain.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <!-- No external dependencies — pure C# -->
</Project>
```

Pure C# assembly with no external dependencies. Contains domain models, DTOs, service interfaces, and conversion logic.

### ISMA.ViewModels

**File:** `src/ISMA.ViewModels/ISMA.ViewModels.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ISMA.Domain\ISMA.Domain.csproj" />
    <ProjectReference Include="..\ISMA.Infrastructure\ISMA.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" />
  </ItemGroup>
</Project>
```

Dependencies: `ISMA.Domain`, `ISMA.Infrastructure`, `CommunityToolkit.Mvvm`.

### ISMA.Infrastructure

**File:** `src/ISMA.Infrastructure/ISMA.Infrastructure.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ISMA.Domain\ISMA.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Grpc.Net.Client" />
    <PackageReference Include="System.Text.Json" />
  </ItemGroup>
</Project>
```

Dependencies: `ISMA.Domain`, `Grpc.Net.Client`, `System.Text.Json`.

### ISMA.App

**File:** `src/ISMA.App/ISMA.App.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ISMA.Domain\ISMA.Domain.csproj" />
    <ProjectReference Include="..\ISMA.ViewModels\ISMA.ViewModels.csproj" />
    <ProjectReference Include="..\ISMA.Infrastructure\ISMA.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
    <PackageReference Include="Avalonia.Fonts.Inter" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="AvaloniaEdit" />
    <PackageReference Include="CommunityToolkit.Mvvm" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  </ItemGroup>
  <ItemGroup>
    <AvaloniaResource Include="Styles\GlobalStyles.axaml" />
    <AvaloniaResource Include="Assets\**" />
    <EmbeddedResource Include="Highlighting\LISMA.xshd" />
  </ItemGroup>
</Project>
```

Dependencies: All other assemblies, AvaloniaUI, AvaloniaEdit, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection.

## Global Build Properties

**File:** `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```

**File:** `Directory.Packages.props`

Central package version management — all package versions are defined in one place and referenced by all projects.

## Building

```bash
# Build all projects
dotnet build isma-ui-dotnet.slnx

# Build specific project
dotnet build src/ISMA.Domain/ISMA.Domain.csproj
dotnet build src/ISMA.ViewModels/ISMA.ViewModels.csproj
dotnet build src/ISMA.Infrastructure/ISMA.Infrastructure.csproj
dotnet build src/ISMA.App/ISMA.App.csproj

# Build and run
dotnet run --project src/ISMA.App/ISMA.App.csproj

# Run tests
dotnet test tests/ISMA.Tests/ISMA.Tests.csproj
```

## Running

### Prerequisites

The application requires an `appsettings.json` configuration file with paths to the ISMA server and Grin chart viewer:

```json
{
  "Server": {
    "ScriptPath": "/path/to/isma-server"
  },
  "Grin": {
    "ScriptPath": "/path/to/grin"
  }
}
```

Configuration resolution priority:
1. `appsettings.json` (embedded resource, loaded via `IConfiguration`)
2. Environment variable `ISMA_SERVER_SCRIPT` (fallback for server path)
3. Environment variable `ISMA_GRIN_SCRIPT` (fallback for Grin path)

### Via run.sh

The provided `run.sh` script sets up the environment and launches the application:

```bash
./run.sh
```

### Via dotnet run

```bash
dotnet run --project src/ISMA.App/ISMA.App.csproj
```

### Configuration File Location

The `appsettings.json` is embedded as an Avalonia resource and loaded at startup via `Microsoft.Extensions.Configuration`. The path resolution happens in `App.axaml.cs::ConfigureServiceCollection()`:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

string? serverPath = configuration["Server:ScriptPath"];
string? grinPath = configuration["Grin:ScriptPath"];
string? lspPath = configuration["Lsp:ScriptPath"];
```

## Dependency Injection

All services and ViewModels are registered in `App.axaml.cs::ConfigureServiceCollection()`:

```csharp
public static void ConfigureServiceCollection(IServiceCollection services)
{
    // Infrastructure services
    services.AddSingleton<ISimulationServerFacade>(sp => /* manager + socket handler + logger */);
    services.AddSingleton<LspProcessManager>();
    services.AddSingleton<ILspTransport>(sp => sp.GetRequiredService<LspProcessManager>().Start());
    services.AddSingleton<LspClient>();
    services.AddSingleton<ISyntaxHighlighter, LspSyntaxHighlighter>();

    // Domain services
    services.AddSingleton<IProjectService, ProjectService>();
    services.AddSingleton<IProjectFileService, ProjectFileService>();
    services.AddSingleton<IErrorListViewModel, ErrorListViewModel>();
    services.AddSingleton<ISimulationServiceViewModel, SimulationServiceViewModel>();
    services.AddSingleton<ISimulationResultService, SimulationResultService>();
    services.AddSingleton<ISimulationParametersStoreService, SimulationParametersService>();
    services.AddSingleton<IPreferencesProvider, PreferencesProvider>();
    services.AddSingleton<IDialogService, DialogService>();
    services.AddSingleton<IEditorPlatformService, EditorPlatformService>();

    // ViewModels
    services.AddSingleton<MainWindowViewModel>();

    // Views (resolved on-demand by Avalonia ViewLocator)
    services.AddSingleton<MainWindow>();
}
```

All registrations use `AddSingleton()` (singleton) lifecycle. Views are resolved on-demand by the Avalonia `ViewLocator` which matches view types to view model types by convention.

## Assets

### Fonts

**Location:** `src/ISMA.App/Assets/Fonts/`

| Font | File | Usage |
|------|------|-------|
| Inter | `Inter-Regular.ttf` | Primary UI font |
| Inter | `Inter-Medium.ttf` | Semi-bold UI text |
| Inter | `Inter-SemiBold.ttf` | Bold UI text |
| Consolas | `Consolas.ttf` | Code editor font |

Embedded via `<AvaloniaResource>` in the project file.

### Syntax Highlighting

**Location:** `src/ISMA.App/Highlighting/LISMA.xshd`

Embedded resource providing client-side regex-based syntax highlighting fallback when the server is unavailable. Defines highlighting spans for:
- Keywords (`state`, `from`, `if`, `else`, `for`, etc.)
- Comments (`//` and `/* */`)
- Numbers (integers and decimals)
- Text/strings

### Icons

Icons use Material Design glyphs via `Avalonia.EmbeddedIcon`. The toolbar and menu bar reference icons by their Material symbol names (e.g., `add_circle_outline`, `save`, `folder_open`).

## Platform Detection

**File:** `src/ISMA.App/Program.cs`

```csharp
public static AppBuilder BuildAvaloniaApp(PlatformDetection platform)
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
```

- `UsePlatformDetect()` — applies platform-specific defaults (window decorations, high DPI, etc.)
- `WithInterFont()` — registers the Inter font family as the default
- Logging goes to `Trace` (visible in debug output)

## Testing

### Unit Tests

**Location:** `tests/ISMA.Tests/`

```bash
dotnet test tests/ISMA.Tests/ISMA.Tests.csproj
```

Tests cover:
- Domain model serialization/deserialization
- Blueprint-to-LISMA conversion correctness
- Result simplification algorithms
- ViewModel logic

### Test Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Moq" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ISMA.Domain\ISMA.Domain.csproj" />
    <ProjectReference Include="..\..\src\ISMA.ViewModels\ISMA.ViewModels.csproj" />
  </ItemGroup>
</Project>
```

## Deployment

### Single-File Publish

```bash
dotnet publish src/ISMA.App/ISMA.App.csproj -c Release -r linux-x64 --self-contained -o ./publish
```

This produces a self-contained executable that includes the .NET runtime and all dependencies.

### Configuration for Production

For production deployments, include `appsettings.json` in the publish output:

```xml
<ItemGroup>
  <None Include="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Cross-Platform

The application targets `net9.0` and uses platform-detect features in Avalonia. It should work on:
- **Linux** — primary development platform
- **macOS** — supported via Avalonia
- **Windows** — supported via Avalonia

Unix domain socket communication requires the target platform to support Unix sockets (all three platforms).

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Server not found | Verify `Server:ScriptPath` in `appsettings.json` points to a valid ISMA server executable |
| Grin not launching | Verify `Grin:ScriptPath` in `appsettings.json` points to a valid Grin launcher script |
| Syntax highlighting not working (server) | Check server connectivity; falls back to embedded XSHD highlighting |
| Build fails on non-Linux | Ensure .NET 9 SDK is installed; Unix socket support is available on all platforms |
| DI resolution fails | Verify all services are registered in `ConfigureServiceCollection()` |
