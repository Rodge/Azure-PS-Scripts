$path = "params.config"
$params = Get-Content $path | Out-String | ConvertFrom-StringData

$subscriptionId = $params.subscriptionId
$resourceGroupName = $params.resourceGroupName
$workspaceName = $params.logAnalyticsWorkspaceName

# Login to your Azure account if necessary
if (-not (Get-AzContext)) {
    Connect-AzAccount
}

# Set the subscription context
Select-AzSubscription -SubscriptionId $subscriptionId

# Get the Log Analytics workspace
$workspace = Get-AzOperationalInsightsWorkspace -ResourceGroupName $resourceGroupName -Name $workspaceName

# Get the RBAC settings for the workspace
$rbacSettings = Get-AzRoleAssignment -Scope $workspace.ResourceId

# Display the RBAC settings
$rbacSettings | Format-Table

# Write it to a semi-colon delimited CSV file
$rbacSettings | Export-Csv -Path ".\LogAnalyticsWorkspaceRBAC.csv" -NoTypeInformation -Delimiter ';' -UseQuotes AsNeeded