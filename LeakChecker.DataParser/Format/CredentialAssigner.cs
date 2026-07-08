using LeakChecker.Common.Enums;

namespace LeakChecker.DataParser.Format;

public static class CredentialAssigner
{
    public static Dictionary<int, ItemType> Assign(Dictionary<int, ItemType> schema)
    {
        if (schema.Count == 0) return schema;

        // If both already exist, nothing to do
        if (schema.ContainsValue(ItemType.Username) &&
            schema.ContainsValue(ItemType.Password))
            return schema;
        
        bool hasUsername = schema.ContainsValue(ItemType.Username);
        bool hasPassword = schema.ContainsValue(ItemType.Password);
        int othersContains = schema.Values.Count(v => v == ItemType.Other);
        
        // Too complicated leak with a lot of data, probably not containing usernames or passwords
        if (othersContains > 4) return schema;  //TODO initially 2 but in case something cant be detected

        // Copy to allow mutation
        var result = new Dictionary<int, ItemType>(schema); //TODO handle this

        // Assign USERNAME if missing and index 0 is Other
        if (!hasUsername &&
            result.TryGetValue(0, out var firstAttr) &&
            firstAttr == ItemType.Other)
        {
            result[0] = ItemType.Username;
            hasUsername = true;
        }

        // Assign PASSWORD if missing: first `Other` at index > 0
        if (!hasPassword)
        {
            foreach (var kvp in result.OrderBy(x => x.Key))
            {
                if (kvp.Key == 0) continue;                 // index 0 handled above
                if (kvp.Value != ItemType.Other) continue;  // only convert Other

                result[kvp.Key] = ItemType.Password;
                hasPassword = true;
                break;
            }
        }

        return result;
    }
}