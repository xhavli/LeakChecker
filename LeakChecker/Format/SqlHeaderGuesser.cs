using LeakChecker.Content;

namespace LeakChecker.Format;

public static class SqlHeaderGuesser
{
    private enum MatchPolicy
    {
        Exact,
        Token,
        Substring
    }

    private record HeaderKeyword(string Keyword, MatchPolicy Policy);

    private static readonly Dictionary<ItemEnum, List<HeaderKeyword>> Synonyms = new()
    {
        {
            ItemEnum.IpV4, [
                new("ip", MatchPolicy.Token),
                new("ip4", MatchPolicy.Substring),
                new("ipv4", MatchPolicy.Substring),
                new("ipv4addr", MatchPolicy.Substring),
                new("ipv4-addr", MatchPolicy.Substring),
                new("ipv4_addr", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.IpV6, [
                new("ip6", MatchPolicy.Substring),
                new("ipv6", MatchPolicy.Substring),
                new("ipv6addr", MatchPolicy.Substring),
                new("ipv6-addr", MatchPolicy.Substring),
                new("ipv6_addr", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Mac, [
                new("mac", MatchPolicy.Token),
                new("macaddr", MatchPolicy.Substring),
                new("macaddress", MatchPolicy.Substring),
                new("mac-address", MatchPolicy.Substring),
                new("mac_address", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.TimeStamp, [
                new("at", MatchPolicy.Token),
                new("ts", MatchPolicy.Token),
                new("time", MatchPolicy.Substring),
                new("timestamp", MatchPolicy.Substring),
                new("date", MatchPolicy.Substring),
                new("datetime", MatchPolicy.Substring),
                new("date-time", MatchPolicy.Substring),
                new("date_time", MatchPolicy.Substring),
                new("dateofbirth", MatchPolicy.Substring),  //TODO create DoB?
                new("date-of-birth", MatchPolicy.Substring),
                new("date_of_birth", MatchPolicy.Substring),
                new("created", MatchPolicy.Token),
                new("modified", MatchPolicy.Token),
                new("updated", MatchPolicy.Token),
                new("insert", MatchPolicy.Token),
                new("insertdate", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Location, [
                new("location", MatchPolicy.Token),
                new("latitude", MatchPolicy.Token),
                new("longitude", MatchPolicy.Token),
                new("latlng", MatchPolicy.Token),
                new("address", MatchPolicy.Token),
                new("city", MatchPolicy.Token),
                new("region", MatchPolicy.Token),
                new("country", MatchPolicy.Token),
                new("state", MatchPolicy.Token),
                new("land", MatchPolicy.Token),
            ]
        },
        {
            ItemEnum.PhoneNumber, [
                new("phone", MatchPolicy.Token),
                new("phonenumber", MatchPolicy.Substring),
                new("phone-number", MatchPolicy.Substring),
                new("phone_number", MatchPolicy.Substring),
                new("telephone", MatchPolicy.Substring),
                new("telephonenum", MatchPolicy.Substring),
                new("telephone-num", MatchPolicy.Substring),
                new("telephone_num", MatchPolicy.Substring),
                new("mobile", MatchPolicy.Token),
                new("mobilephone", MatchPolicy.Substring),
                new("mobile-phone", MatchPolicy.Substring),
                new("mobile_phone", MatchPolicy.Substring),
                new("cell", MatchPolicy.Token),
            ]
        },
        {
            ItemEnum.Email, [
                new("mail", MatchPolicy.Substring),
                new("mailaddr", MatchPolicy.Substring),
                new("mail-addr", MatchPolicy.Substring),
                new("mail_addr", MatchPolicy.Substring),
                new("email", MatchPolicy.Token),
                new("e-mail", MatchPolicy.Substring),
                new("e_mail", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Iban, [
                new("iban", MatchPolicy.Token),
                new("bankaccount", MatchPolicy.Substring),
                new("bank-account", MatchPolicy.Substring),
                new("bank_account", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Gender, [new("gender", MatchPolicy.Substring),]
        },
        {
            ItemEnum.Name, [
                new("name", MatchPolicy.Token),
                new("firstname", MatchPolicy.Token),
                new("lastname", MatchPolicy.Token),
                new("surname", MatchPolicy.Token),
                new("fullname", MatchPolicy.Token),
                new("givenname", MatchPolicy.Token),
            ]
        },
        {
            ItemEnum.Username, [
                new("username", MatchPolicy.Substring),
                new("user-name", MatchPolicy.Substring),
                new("user_name", MatchPolicy.Substring),
                new("user-login", MatchPolicy.Substring),
                new("user_login", MatchPolicy.Substring),
                new("user_nicename", MatchPolicy.Substring),
                new("nick", MatchPolicy.Substring),
                new("nickname", MatchPolicy.Substring),
                new("nick-name", MatchPolicy.Substring),
                new("nick_name", MatchPolicy.Substring),
                new("alias", MatchPolicy.Substring),
                new("displayname", MatchPolicy.Substring),
                new("display-name", MatchPolicy.Substring),
                new("display_name", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Password, [
                new("password", MatchPolicy.Substring),
                new("passwd", MatchPolicy.Token),
                new("pass", MatchPolicy.Token),
                new("pwd", MatchPolicy.Token),
                new("user_pass", MatchPolicy.Exact),
                new("userpassword", MatchPolicy.Substring),
                new("user-password", MatchPolicy.Substring),
                new("user_password", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Hash, [
                new("hash", MatchPolicy.Substring),
                new("hash", MatchPolicy.Token),
                new("md5", MatchPolicy.Token),
                new("sha1", MatchPolicy.Token),
                new("sha256", MatchPolicy.Token),
                new("sha512", MatchPolicy.Token),
                new("crc32", MatchPolicy.Token),
                new("ripemd160", MatchPolicy.Token),
                new("whirlpool", MatchPolicy.Token),
                new("bcrypt", MatchPolicy.Token),
            ]
        },
        {
            ItemEnum.Salt, [new("salt", MatchPolicy.Token)]
        },
        {
            ItemEnum.Token, [
                new("token", MatchPolicy.Token),
                new("api_key", MatchPolicy.Exact),
                new("api_key", MatchPolicy.Substring),
                new("auth", MatchPolicy.Substring), //TODO
                new("secret", MatchPolicy.Token),
                new("access_token", MatchPolicy.Exact)
            ]
        },
        {
            ItemEnum.Web, [
                new("web", MatchPolicy.Token),
                new("website", MatchPolicy.Token),
                new("webaddr", MatchPolicy.Substring),
                new("webaddress", MatchPolicy.Substring),
                new("url", MatchPolicy.Token),
                new("link", MatchPolicy.Substring),
                new("site", MatchPolicy.Token),
            ]
        },
        {
            ItemEnum.Id, [
                new("id", MatchPolicy.Token),
                new("uid", MatchPolicy.Token),
                new("bhid", MatchPolicy.Token),
                new("userid", MatchPolicy.Substring),
                new("user-id", MatchPolicy.Exact),
                new("user_id", MatchPolicy.Exact),
                new("playerid", MatchPolicy.Substring),
                new("player-id", MatchPolicy.Exact),
                new("player_id", MatchPolicy.Exact),
                new("guid", MatchPolicy.Token)
            ]
        },
        {
            ItemEnum.Other, []
        }
    };

    /// <summary>
    /// Attempts to guess ItemEnum types from a list of SQL column headers.
    /// </summary>
    public static Dictionary<int, ItemEnum> GuessColumns(IEnumerable<string> headers)
    {
        var result = new Dictionary<int, ItemEnum>();
        int index = 0;

        foreach (string rawHeader in headers)
        {
            string header = rawHeader.Trim('`', '"', '[', ']', ' ', '\t');
            ItemEnum guessed = GuessItem(header);
            result[index++] = guessed;
        }

        return result;
    }

    public static ItemEnum GuessItem(string header)
    {
        // FIRST PASS: true semantic exact matches (ignore policy)
        foreach (var (itemType, keywords) in Synonyms)
        {
            foreach (var keyword in keywords)
            {
                if (Normalize(keyword.Keyword) == Normalize(header))
                {
                    Console.WriteLine($"{header} -> EXACT MATCH ({itemType})");
                    return itemType;
                }
            }
        }
        
        // SECOND PASS: scoring system
        var tokens = TokenizeHeader(header).ToArray();
        string lowerHeader = header.ToLowerInvariant();
        int lastIndex = tokens.Length - 1;

        var scores = new Dictionary<ItemEnum, int>();

        foreach (var (itemType, keywords) in Synonyms)
        {
            int score = 0;

            foreach (var keyword in keywords)
            {
                bool matched = false;

                switch (keyword.Policy)
                {
                    case MatchPolicy.Exact:
                        if (string.Equals(lowerHeader, keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 15;     // existing base
                            matched = true;
                        }
                        break;

                    case MatchPolicy.Token:
                        // check if header tokens contain the keyword
                        
                        // we don't know if for example created_by will be name, id or something other 
                        if (tokens.Last().Equals("by", StringComparison.OrdinalIgnoreCase))
                        {
                            score = 0;
                            break;
                        }
                        
                        int index = Array.IndexOf(tokens, keyword.Keyword.ToLowerInvariant());
                        if (index != -1)
                        {
                            score += 3; // existing base
                            matched = true;

                            if (index == lastIndex)
                                score += 5; // last token is strongest
                            else if (index == lastIndex - 1)
                                score += 3; // second-to-last token
                            else if (index == 0)
                                score += 1; // first token
                            else
                                score += 1; // middle tokens still add small bonus
                        }
                        break;

                    case MatchPolicy.Substring:
                        if (lowerHeader.Contains(keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 2; // existing base only, no positional bonus //TODO origin 1
                            matched = true;
                        }
                        break;
                }

                // Keep your existing "ends-with" accuracy bonus
                if (matched && lowerHeader.EndsWith(keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                    score += 1;
            }

            if (score > 0)
                scores[itemType] = score;
        }

        if (scores.Count == 0)
            return ItemEnum.Other;

        // Pick the item with the highest score
        var results = scores.OrderByDescending(kv => kv.Value).ToList();
        var (bestMatch, bestScore) = results.First();
        
        Console.WriteLine($"{header}");
        foreach (var res in results)
        {
            Console.WriteLine($"{res.Key}: {res.Value}");
        }
        
        return bestScore > 1 ? bestMatch : ItemEnum.Other;
    }
    
    private static IEnumerable<string> TokenizeHeader(string header)
    {
        var parts = new List<string>();
        string current = "";

        foreach (char c in header)
        {
            // Split when uppercase, underscore, hyphen, or digit boundary
            if (char.IsUpper(c) || c == '_' || c == '-' || char.IsDigit(c))
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToLowerInvariant());
                    current = "";
                }

                if (char.IsLetter(c))
                    current += char.ToLowerInvariant(c);
                else if (char.IsDigit(c))
                    parts.Add(c.ToString()); // digits become standalone tokens
            }
            else
            {
                current += char.ToLowerInvariant(c);
            }
        }

        if (current.Length > 0)
            parts.Add(current.ToLowerInvariant());

        // --- PATCH: merge ip + 4/6 and ipv + 4/6 ---
        for (int i = 0; i < parts.Count - 1; i++)
        {
            string token = parts[i];
            string next = parts[i + 1];

            if (token is "ip" or "ipv" && next is "4" or "6")
            {
                parts[i] = token + next;
                parts.RemoveAt(i + 1);
            }
        }

        return parts;
    }
    
    private static string Normalize(string s)
        => new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}