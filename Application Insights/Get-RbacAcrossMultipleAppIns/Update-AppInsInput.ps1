# Import the required modules
Import-Module Az.Accounts
Import-Module Az.ApplicationInsights

$Path = "params.config"
$params = Get-Content $Path | Out-String | ConvertFrom-StringData

# Set the subscription ID
$subscriptionId = $params.subscriptionId

# Login to your Azure account
Connect-AzAccount

# Select the subscription you want to work with
Select-AzSubscription -SubscriptionId $subscriptionId

# Get all instances of Application Insights
$AppInsights = Get-AzApplicationInsights

# Create an empty array to store the output
$Output = @()

# Initialize a counter variable
$Counter = 0

# Loop through each Application Insights instance
foreach ($AI in $AppInsights) {
    $Counter++

    # Calculate the progress percentage
    $Progress = ($Counter / $AppInsights.Count) * 100

    # Show progress as a percentage
    Write-Progress -Activity "Processing Application Insights instances" -Status "Processing $($AI.Name)" -PercentComplete $Progress

    # Get the RBAC settings for the current Application Insights instance
    $RBAC = Get-AzRoleAssignment -Scope $AI.Id

    # Serialize the RBAC values into a JSON string
    $RBACJson = $RBAC | ConvertTo-Json -Compress

    # Create a custom object to store the relevant information
    $Obj = New-Object -TypeName PSObject
    $Obj | Add-Member -MemberType NoteProperty -Name "Name" -Value $AI.Name
    $Obj | Add-Member -MemberType NoteProperty -Name "ResourceGroup" -Value $AI.ResourceGroupName
    $Obj | Add-Member -MemberType NoteProperty -Name "RBAC" -Value $RBACJson

    # Add the custom object to the output array
    $Output += $Obj
}

# Export the output array to a CSV file in the same folder as the script
$Output | Export-Csv -Path ".\ApplicationInsightsRBAC.csv" -NoTypeInformation -Delimiter ';' -UseQuotes AsNeeded