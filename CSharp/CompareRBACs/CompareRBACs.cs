using CompareRBACs.Models;
using Newtonsoft.Json;

namespace CompareRBACs;

public static class CompareRBACs
{
    public static void Main(string[] args)
    {
        var applicationInsightsRbacCsvFile = args[0];
        var csvLines = File.ReadAllLines(applicationInsightsRbacCsvFile).ToList().Skip(1).ToList();
        var appInsRows = csvLines.Select(CreateAppInsRow).ToList();
        
        var appInsRowsByName = appInsRows
            .OrderBy(m => m.Name)
            .ToDictionary(m => m.Name);

        var unmatchedInOtherAppInsRbac = new List<Unmatched>();
        foreach (var appInsRowKVP in appInsRowsByName)
        {
            var appInsRowRBAC = appInsRowKVP.Value.RBAC?
                .OrderBy(m => m.Key)
                .ToList();
            
            var otherAppInsRowsByName = appInsRowsByName
                .Where(m => m.Key != appInsRowKVP.Key);
            
            foreach (var otherAppInsRowKVP in otherAppInsRowsByName)
            {
                var otherAppInsRowRBAC = otherAppInsRowKVP.Value.RBAC?
                    .OrderBy(m => m.Key)
                    .ToList();
                
                if (appInsRowRBAC == null || otherAppInsRowRBAC == null)
                    continue;
                
                foreach (var rbacEntry in appInsRowRBAC)
                {
                    var matchFoundInOtherRowRBAC = otherAppInsRowRBAC.
                        Exists(m => m.Key == rbacEntry.Key);
                    
                    if (!matchFoundInOtherRowRBAC)
                    {
                        unmatchedInOtherAppInsRbac.Add(new Unmatched
                        {
                            RoleAssignmentName = rbacEntry.Key,
                            RoleDefinitionName = rbacEntry.Value.RoleDefinitionName,
                            DisplayName = rbacEntry.Value.DisplayName, 
                            SignInName = rbacEntry.Value.SignInName, 
                            Name = appInsRowKVP.Key,
                            OffendedAppInsName = otherAppInsRowKVP.Value.Name
                        });
                    }
                }
            }
        }
        
        PrintUnmatchedOnesNotIgnored(unmatchedInOtherAppInsRbac);
    }

    private static void PrintUnmatchedOnesNotIgnored(List<Unmatched> unmatchedOnes)
    {
        TryIgnoreStuff(out var ignoredNames, out var ignoredDisplayNames);

        foreach (var unmatchedOne in unmatchedOnes)
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
        var name = lineSplit[0];
        var resourceGroup = lineSplit[1];
        var tmp = lineSplit[2].Replace("\"\"", "\"");
        var rbacs = tmp.Substring(1, tmp.Length - 2);
        var rbacList = JsonConvert.DeserializeObject<List<RBAC>>(rbacs);
        
        var newAppInsRow = new AppInsRow()
        {
            Name = name, 
            ResourceGroup = resourceGroup, 
            RBAC = rbacList?.ToDictionary(m => m.RoleAssignmentName)
        };
        
        return newAppInsRow;
    }
}