# Get average data volume for workspace-based Application Insights

Resources in chosen subscription, using config parameters from params.config (see example in params.config.example)
```powershell
.\Get-AverageDataVolume.ps1
```
This script also depends on C# being compiled from `..\..\CSharp\CompareRBACs` (might have to adjust a path if not on Windows).