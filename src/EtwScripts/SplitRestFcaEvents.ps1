Param(
    [Parameter(Mandatory=$true)]
    [String] $csvFile
)

$rawCsv = Import-Csv $csvFile;
$rawCsv | Where-Object { $_."Event Name".StartsWith("FcaEventSource") } | ForEach-Object {
    $restData = @{};
    $m = $_.Rest | Select-String -Pattern '(\S+)="([^"]*)"' -AllMatches;
    $m.Matches | ForEach-Object {
        $restData[$_.Groups[1].Value] = $_.Groups[2].Value;
    }
    
    [PSCustomObject]@{
        'EventName' = $_."Event Name"
        'TimeMSec' = $_."Time MSec"
        'ProcessName' = $_."Process Name"
        'Rest' = $_.Rest
        'RestData' = $restData
    }; 
}