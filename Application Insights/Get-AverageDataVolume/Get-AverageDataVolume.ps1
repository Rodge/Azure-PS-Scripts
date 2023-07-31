$Path = "params.config"
$params = Get-Content $Path | Out-String | ConvertFrom-StringData

# Set the subscription ID
$subscriptionId = $params.subscriptionId

# Set the start and end dates for the data volume calculation
$startDate = (Get-Date).AddDays(-30)
$endDate = Get-Date

# Set the resource group name (optional)
#$resourceGroupName = $params.optionalResourceGroupName

# Authenticate with Azure
Connect-AzAccount

# Set the subscription context
Select-AzSubscription -SubscriptionId $subscriptionId

# Get all Application Insights instances in the subscription (or resource group, if specified)
if ($resourceGroupName) {
    $appInsightsResources = Get-AzResource -ResourceType "microsoft.insights/components" -ResourceGroupName $resourceGroupName
} else {
    $appInsightsResources = Get-AzResource -ResourceType "microsoft.insights/components"
}

# Calculate the data volume for each instance
$dataVolumes = $appInsightsResources | ForEach-Object {
    $resourceId = $_.ResourceId
    $yo = Get-AzMetricDefinition -ResourceId $resourceId
    Write-Output $yo

    $usage = Get-AzMetric -ResourceId $resourceId -MetricName "ingestedBytes" -StartTime $startDate -EndTime $endDate -TimeGrain ([TimeSpan]::FromDays(1))
    [PSCustomObject]@{
        ResourceName = $_.Name
        DataVolume = ($usage.Data | Measure-Object -Property Total -Sum).Sum
    }
}

# Calculate the average total data volume per day for all instances
$totalDataVolume = ($dataVolumes | Measure-Object -Property DataVolume -Sum).Sum
$averageDataVolumePerDay = $totalDataVolume / 30

# Output the results
Write-Output "Average total data volume per day (last 30 days): $averageDataVolumePerDay"
