$harmonyRoot = "C:\microchip\harmony\v3\csp\devices"
$mzDevices = Get-ChildItem -Path $harmonyRoot -Directory | Where-Object { $_.Name -like "PIC32MZ*" }

foreach ($device in $mzDevices) {
    $periphXml = Join-Path $device.FullName "peripherals.xml"
    if (Test-Path $periphXml) {
        [xml]$xml = Get-Content $periphXml
        $xml.peripherals.peripheral | ForEach-Object {
            $name = $_.name
            $_.instance | ForEach-Object {
                $plib = $_.plib
                Write-Output "$($device.Name): $name → $plib"
            }
        }
    }
}