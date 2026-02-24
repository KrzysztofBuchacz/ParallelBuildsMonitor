# Parallel Builds Monitor

Parallel Builds Monitor Visual Studio Extension

For Visual Studio 2017 2019 2022 2026

This extension can be installed via [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=ivson4.ParallelBuildsMonitor-18691)

For project details, options, installation instructions, and images, refer to the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=ivson4.ParallelBuildsMonitor-18691)

[Release Notes](Plugin/ReleaseNotes.txt)

## Git contents:
- `Plugin` - contain `Parallel Build Monitor` extension to be installed in Visual Studio.  
- `Example` - contain some dummy project with irrelevant code just to test `Parallel Build Monitor` plugin.  
- `Tests` - unit tests for `Plugin`.  
- `packages` and `TestResults` - dynamically created directories during build or testing.  


## How to debug this project

Uninstall Parallel Build Monitor if installed

- Open ParallelBuildsMonitor.sln
- Open project properties
- Go to Debug tab
- Click `Start External Program` and browse for `devenv.exe`
- In `Command line arguments` type `/rootsuffix Exp`
- Start Debugging

Next Visual Studio will be open. ParallelBuildMonitor will be automatically added to it. If control is not visible open it from menu `View->Other Windows->Parallel Builds Monitor`. Be aware that debugged Visual Studio is open in experimental mode and some settings may be different from your current profile.

Developed with:
```
Microsoft Visual Studio Community 2022 (64-bit)
Version 17.12+
Microsoft .NET Framework
Version 4.7.2 (Plugin) / 4.8 (Tests)
