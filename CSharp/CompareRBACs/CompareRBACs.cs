using CompareRBACs.Models;
using Newtonsoft.Json;

namespace CompareRBACs;

public static class CompareRBACs
{
    public static void Main(string[] args)
    {
        var csvFile = args[0];
        var csvLines = File.ReadAllLines(csvFile).ToList().Skip(1).ToList();
        var appInsRows = csvLines.Select(CreateAppInsRow).ToList();
        
        var appInsRowsByName = appInsRows
            .OrderBy(m => m.Name)
            .ToDictionary(m => m.Name);

        var unmatchedOnes = new List<Unmatched>();
        foreach (var appInsRowKVP in appInsRowsByName)
        {
            var name = appInsRowKVP.Key;
            var row = appInsRowKVP.Value;
            
            var otherRows = appInsRowsByName
                .OrderBy(m => m.Key)
                .Where(m => m.Key != name);
            
            foreach (var otherRowKVP in otherRows)
            {
                var otherRow = otherRowKVP.Value;
                
                var rowRBACOrdered = row.RBAC?.OrderBy(m => m.Key).ToList();
                var otherRowRBACOrdered = otherRow.RBAC?.OrderBy(m => m.Key).ToList();
                
                if (rowRBACOrdered == null || otherRowRBACOrdered == null)
                    continue;
                
                foreach (var rowRBAC in rowRBACOrdered)
                {
                    var matchFoundInOtherRowRBAC = otherRowRBACOrdered.Any(m => m.Key == rowRBAC.Key);
                    if (!matchFoundInOtherRowRBAC)
                    {
                        unmatchedOnes.Add(new Unmatched
                        {
                            RoleAssignmentName = rowRBAC.Key,
                            RoleDefinitionName = rowRBAC.Value.RoleDefinitionName,
                            DisplayName = rowRBAC.Value.DisplayName, 
                            SignInName = rowRBAC.Value.SignInName, 
                            Name = name,
                            OffendedAppInsName = otherRow.Name
                        });
                    }
                }
            }
        }
        
        PrintUnmatchedOnesNotIgnored(unmatchedOnes);
    }

    private static void PrintUnmatchedOnesNotIgnored(List<Unmatched> unmatchedOnes)
    {
        TryIgnoreStuff(out var ignoredNames, out var ignoredDisplayNames);

        foreach (var unmatchedOne in unmatchedOnes)
        {
            var ignored =
                ignoredDisplayNames.Any(m => unmatchedOne.DisplayName.Contains(m)) 
                ||
                ignoredNames.Any(m => unmatchedOne.Name.Contains(m));

            if (!ignored)
                Console.WriteLine(unmatchedOne.Offence);
        }
    }

    private static bool TryIgnoreStuff(out List<string> ignoredNames, out List<string> ignoredDisplayNames)
    {
        var ignoredNamesFile = "..\\..\\..\\ignoredNames.data";
        var ignoredDisplayNamesFile = "..\\..\\..\\ignoredDisplayNames.data";
        ignoredNames = new List<string>();
        ignoredDisplayNames = new List<string>();

        var ignoringStuff = false;

        // Read values from file into ignoredDisplayNames and ignoredNames
        if (File.Exists(ignoredDisplayNamesFile))
        {
            var ignoredDisplayNamesFileLines = File.ReadAllLines(ignoredDisplayNamesFile);
            if (ignoredDisplayNamesFileLines.Any())
                ignoringStuff = true;
            
            ignoredDisplayNames.AddRange(ignoredDisplayNamesFileLines);
        }

        if (File.Exists(ignoredNamesFile))
        {
            var ignoredNamesFileLines = File.ReadAllLines(ignoredNamesFile);
            if (ignoredNamesFileLines.Any())
                ignoringStuff = true;
            
            ignoredNames.AddRange(ignoredNamesFileLines);
        }

        return ignoringStuff;
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