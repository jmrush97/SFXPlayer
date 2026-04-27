# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade SFXPlayer\SFXPlayer.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                | Current Version | New Version | Description                                      |
|:--------------------------------------------|:---------------:|:-----------:|:-------------------------------------------------|
| Microsoft.Win32.Registry                    | 5.0.0           |             | Functionality included with .NET 8.0 framework   |
| System.Buffers                              | 4.5.1           |             | Functionality included with .NET 8.0 framework   |
| System.Drawing.Common                       | 7.0.0           | 8.0.26      | Recommended for .NET 8.0                         |
| System.Memory                               | 4.5.5           |             | Functionality included with .NET 8.0 framework   |
| System.Numerics.Vectors                     | 4.5.0           |             | Functionality included with .NET 8.0 framework   |
| System.Runtime.CompilerServices.Unsafe      | 6.0.0           | 6.1.2       | Recommended for .NET 8.0                         |
| System.Security.AccessControl               | 6.0.0           | 6.0.1       | Recommended for .NET 8.0                         |
| System.Security.Principal.Windows           | 5.0.0           |             | Functionality included with .NET 8.0 framework   |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### SFXPlayer\SFXPlayer.csproj modifications

Project conversion:
  - Project file needs to be converted from legacy format to SDK-style project format

Project properties changes:
  - Target framework should be changed from `net472` to `net8.0-windows`

NuGet packages changes:
  - Microsoft.Win32.Registry should be removed (functionality included with .NET 8.0 framework)
  - System.Buffers should be removed (functionality included with .NET 8.0 framework)
  - System.Drawing.Common should be updated from `7.0.0` to `8.0.26` (*recommended for .NET 8.0*)
  - System.Memory should be removed (functionality included with .NET 8.0 framework)
  - System.Numerics.Vectors should be removed (functionality included with .NET 8.0 framework)
  - System.Runtime.CompilerServices.Unsafe should be updated from `6.0.0` to `6.1.2` (*recommended for .NET 8.0*)
  - System.Security.AccessControl should be updated from `6.0.0` to `6.0.1` (*recommended for .NET 8.0*)
  - System.Security.Principal.Windows should be removed (functionality included with .NET 8.0 framework)
