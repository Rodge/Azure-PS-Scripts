using CompareRBACs.Models;
using Newtonsoft.Json;

namespace CompareRBACs;

public static class CompareRBACs
{
    public static void Main(string[] args)
    {
        var applicationInsightsRbacCsvFile = args[0];
        var logAnalyticsWorkspaceRbacCsvFile = args[1];
        var appInsCsvLines = File.ReadAllLines(applicationInsightsRbacCsvFile).ToList().Skip(1).ToList();
        var logAnalyticsCsvLines = File.ReadAllLines(logAnalyticsWorkspaceRbacCsvFile).ToList().Skip(1).ToList();

        var appInsRows = appInsCsvLines.Select(CreateAppInsRow).ToList();
        CompareAppInsRbacs(appInsRows);

        var rbacsInAppInsRows = appInsRows
            .Where(m => m.RBAC != null)
            .SelectMany(m => m.RBAC!.Values)
            .DistinctBy(m => $"{m.ObjectId} {m.RoleDefinitionName}")
            .OrderBy(m => $"{m.ObjectId} {m.RoleDefinitionName}")
            .ToList();
        
        var logAnalyticsRbacRows = logAnalyticsCsvLines.Select(CreateRbacFromLogAnalyticsRow).ToList();

        CompareAppInsRbacsWithLogAnalyticsRbacs(rbacsInAppInsRows, logAnalyticsRbacRows);
    }

    private static void CompareAppInsRbacs(List<AppInsRow> appInsRows)
    {
        var leftAppInsRowsByName = appInsRows
            .OrderBy(m => m.AppInsName)
            .ToDictionary(m => m.AppInsName);

        var offences = new List<Unmatched>();
        foreach (var (leftAppInsName, leftAppInsRow) in leftAppInsRowsByName)
        {
            var rbacForLeftAppInsRow = leftAppInsRow.RBAC?
                .OrderBy(m => m.Key)
                .ToList(); // Ordered by Key being $"{ObjectId} {RoleDefinitionName}"
            
            var possiblyOffendedAppInsRows = leftAppInsRowsByName
                .Where(m => m.Key != leftAppInsName);
            
            foreach (var (_, possiblyOffendedAppInsRow) in possiblyOffendedAppInsRows)
            {
                var rbacForPossiblyOffendedAppInsRow = possiblyOffendedAppInsRow.RBAC?
                    .OrderBy(m => m.Key)
                    .ToList();
                
                if (rbacForLeftAppInsRow == null || rbacForPossiblyOffendedAppInsRow == null)
                    continue;
                
                foreach (var rbacEntryInLeftAppInsRow in rbacForLeftAppInsRow)
                {
                    var didNotCoExist = !rbacForPossiblyOffendedAppInsRow.
                        Exists(m => 
                            m.Value.ObjectId == rbacEntryInLeftAppInsRow.Value.ObjectId && 
                            m.Value.RoleDefinitionName == rbacEntryInLeftAppInsRow.Value.RoleDefinitionName);
                    
                    if (didNotCoExist)
                    {
                        offences.Add(new Unmatched
                        {
                            RoleAssignmentName = rbacEntryInLeftAppInsRow.Value.RoleAssignmentName,
                            RoleDefinitionName = rbacEntryInLeftAppInsRow.Value.RoleDefinitionName,
                            DisplayName = rbacEntryInLeftAppInsRow.Value.DisplayName, 
                            SignInName = rbacEntryInLeftAppInsRow.Value.SignInName, 
                            Name = leftAppInsName,
                            ObjectId = rbacEntryInLeftAppInsRow.Value.ObjectId,
                            OffendedAppInsName = possiblyOffendedAppInsRow.AppInsName
                        });
                    }
                }
            }
        }
        
        PrintUnmatchedOnesNotIgnored(offences);
    }

    private static void PrintUnmatchedOnesNotIgnored(List<Unmatched> unmatchedOnes)
    {
        TryIgnoreStuff(out var ignoredNames, out var ignoredDisplayNames);

        foreach (var unmatchedOne in unmatchedOnes.OrderBy(m => $"{m.ObjectId} {m.RoleDefinitionName}"))
        {
            var ignored =
                ignoredDisplayNames.Exists(m => unmatchedOne.DisplayName.Contains(m)) 
                ||
                ignoredNames.Exists(m => unmatchedOne.Name.Contains(m));

            if (!ignored)
                Console.WriteLine(unmatchedOne.Offence);
        }
    }

    private static bool TryIgnoreStuff(out List<string> ignoredNames, out List<string> ignoredDisplayNames)
    {
        var ignoredNamesFile = "..\\..\\..\\ignoredNames.data";
        var ignoredDisplayNamesFile = "..\\..\\..\\ignoredDisplayNames.data";
        
        // Read values from file into ignoredDisplayNames and ignoredNames
        ignoredDisplayNames = File.Exists(ignoredDisplayNamesFile) 
            ? File.ReadAllLines(ignoredDisplayNamesFile).ToList() 
            : new List<string>();

        ignoredNames = File.Exists(ignoredNamesFile) 
            ? File.ReadAllLines(ignoredNamesFile).ToList() 
            : new List<string>();

        return ignoredNames.Any() || ignoredDisplayNames.Any();
    }

    private static AppInsRow CreateAppInsRow(string line)
    {
        var lineSplit = line.Split(';');
        var appInsName = lineSplit[0];
        var resourceGroup = lineSplit[1];
        var tmp = lineSplit[2].Replace("\"\"", "\"");
        var rbacs = tmp.Substring(1, tmp.Length - 2);
        var rbacList = JsonConvert.DeserializeObject<List<RBAC>>(rbacs);
        
        var newAppInsRow = new AppInsRow()
        {
            AppInsName = appInsName, 
            ResourceGroup = resourceGroup, 
            RBAC = rbacList?
                .RemoveDuplicatesBy(m => $"{m.ObjectId} {m.RoleDefinitionName}")
                .ToDictionary(m => $"{m.ObjectId} has {m.RoleDefinitionName}")
        };
        
        return newAppInsRow;
    }
    
    private static RBAC CreateRbacFromLogAnalyticsRow(string line)
    {
        // RoleAssignmentName;RoleAssignmentId;Scope;DisplayName;SignInName;RoleDefinitionName;RoleDefinitionId;ObjectId;ObjectType;CanDelegate;Description;ConditionVersion;Condition
        var lineSplit = line.Split(';');
        
        return new RBAC
        {
            RoleAssignmentName = lineSplit[0], 
            RoleAssignmentId = lineSplit[1], 
            Scope = lineSplit[2], 
            DisplayName = lineSplit[3], 
            SignInName = lineSplit[4], 
            RoleDefinitionName = lineSplit[5], 
            RoleDefinitionId = lineSplit[6], 
            ObjectId = lineSplit[7], 
            ObjectType = lineSplit[8], 
            CanDelegate = lineSplit[9] == "true", 
            Description = lineSplit[10], 
            ConditionVersion = lineSplit[11], 
            Condition = lineSplit[12]
        };
    }

    private static void CompareAppInsRbacsWithLogAnalyticsRbacs(List<RBAC> appInsRbac, List<RBAC> logAnalyticsRbacs)
    {
        foreach (var appInsRbacEntry in appInsRbac)
        {
            var logAnalyticsRbacEntry = logAnalyticsRbacs
                .FirstOrDefault(m =>
                    m.ObjectId == appInsRbacEntry.ObjectId &&
                    m.RoleDefinitionName == appInsRbacEntry.RoleDefinitionName);

            if (logAnalyticsRbacEntry == null)
            {
                Console.WriteLine($"No match for ({appInsRbacEntry.ObjectId}, {appInsRbacEntry.RoleDefinitionName}) in Log Analytics workspace");
            }
        }
    }
}