<#
Sets sail from the console without port drama.
  ./run.ps1              -> single-player client (default port 5073)
  ./run.ps1 -Server      -> multiplayer server + hub (default port 5295)
  ./run.ps1 -TakePort    -> stop whatever holds the port instead of moving berth
  ./run.ps1 -Port 6000   -> ask for a specific port
If the port is taken, finds the next free berth and says so - no stack traces.
#>
param(
    [switch]$Server,
    [switch]$TakePort,
    [int]$Port = 0
)

$project = if ($Server) { "src/SpaceSails.Server" } else { "src/SpaceSails.Client" }
if ($Port -eq 0) { $Port = if ($Server) { 5295 } else { 5073 } }

$holder = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($holder) {
    $who = try { (Get-Process -Id $holder.OwningProcess -ErrorAction Stop).ProcessName } catch { "unknown" }
    Write-Host ""
    Write-Host "  Port $Port is already taken (by '$who', PID $($holder.OwningProcess))." -ForegroundColor Yellow
    if ($TakePort) {
        Write-Host "  -TakePort: sending it to Davy Jones..." -ForegroundColor Yellow
        try { Stop-Process -Id $holder.OwningProcess -Force -ErrorAction Stop; Start-Sleep 1 }
        catch { Write-Host "  Could not stop it: $_" -ForegroundColor Red; exit 1 }
    }
    else {
        $next = 0
        foreach ($candidate in ($Port + 1)..($Port + 20)) {
            if (-not (Get-NetTCPConnection -LocalPort $candidate -State Listen -ErrorAction SilentlyContinue)) { $next = $candidate; break }
        }
        if ($next -eq 0) { Write-Host "  No free berth within 20 ports of $Port. Harbor is full, captain." -ForegroundColor Red; exit 1 }
        Write-Host "  Setting sail from port $next instead. (Use -TakePort to reclaim $Port.)" -ForegroundColor Cyan
        $Port = $next
    }
    Write-Host ""
}

Write-Host "  SpaceSails -> http://localhost:$Port" -ForegroundColor Green
dotnet run -c Release --project $project --urls "http://localhost:$Port"
