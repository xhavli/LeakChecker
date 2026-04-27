using LeakChecker.Common.Enums;

namespace LeakChecker.DataParser.Format;

public static class CredentialAssigner
{
    public static Dictionary<int, ItemEnum> Assign(Dictionary<int, ItemEnum> schema)
    {
        if (schema.Count == 0) return schema;

        // If both already exist, nothing to do
        if (schema.ContainsValue(ItemEnum.Username) &&
            schema.ContainsValue(ItemEnum.Password))
            return schema;
        
        bool hasUsername = schema.ContainsValue(ItemEnum.Username);
        bool hasPassword = schema.ContainsValue(ItemEnum.Password);
        int othersContains = schema.Values.Count(v => v == ItemEnum.Other);
        
        // Too complicated leak with a lot of data, probably not containing usernames or passwords
        if (othersContains > 4) return schema;  //TODO initially 2 but in case something cant be detected

        // Copy to allow mutation
        var result = new Dictionary<int, ItemEnum>(schema); //TODO handle this

        // Assign USERNAME if missing and index 0 is Other
        if (!hasUsername &&
            result.TryGetValue(0, out var firstAttr) &&
            firstAttr == ItemEnum.Other)
        {
            result[0] = ItemEnum.Username;
            hasUsername = true;
        }

        // Assign PASSWORD if missing: first `Other` at index > 0
        if (!hasPassword)
        {
            foreach (var kvp in result.OrderBy(x => x.Key))
            {
                if (kvp.Key == 0) continue;                 // index 0 handled above
                if (kvp.Value != ItemEnum.Other) continue;  // only convert Other

                result[kvp.Key] = ItemEnum.Password;
                hasPassword = true;
                break;
            }
        }

        return result;
    }
}