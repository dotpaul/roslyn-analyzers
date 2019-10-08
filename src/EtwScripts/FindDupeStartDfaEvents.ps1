Param(
    $data
)

$startsObserved = @{};

$data | Where-Object { $_.EventName -eq 'FcaEventSource/StartDfa' } | ForEach-Object {
    $key = [String]::Join(':', $_.RestData['analysisType'], $_.RestData['target'], $_.RestData['analysisContextHashCode']);
    if ($startsObserved.ContainsKey($key)) {
        $startsObserved[$key];
        $_;
    }
    else {
        $startsObserved[$key] = $_;
    }
}