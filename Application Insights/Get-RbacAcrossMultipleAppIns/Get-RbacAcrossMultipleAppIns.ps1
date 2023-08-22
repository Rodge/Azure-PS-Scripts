# Read from the CSV file and display its content
$CSVContent = Import-Csv -Path ".\ApplicationInsights.csv"
$CSVContent | Format-Table

# Call CompareRBACs.exe to compare the RBAC settings
$result = & "..\..\CSharp\CompareRBACs\bin\Debug\net7.0\CompareRBACs.exe" ".\ApplicationInsights.csv"
$result | Format-Table