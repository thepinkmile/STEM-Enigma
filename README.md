# STEM-Enigma
A repository containing data, designs and software for an Enigma Machine.

## Interactive 3D Enigma

The existing [Code Breakers application](Software/APP-CodeBreakers/src/CodeBreakers/README.md)
uses Avalonia and TinyFFR to animate rotor gears, trace electrical connections,
and switch between assembled and exploded views. Original part models and
their generator are included.

With the .NET 10 SDK installed:

```powershell
Set-Location Software\APP-CodeBreakers\src
dotnet run --project CodeBreakers.Desktop
```
