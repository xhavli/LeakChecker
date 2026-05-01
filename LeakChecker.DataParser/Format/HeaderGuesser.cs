using LeakChecker.Common.Enums;

namespace LeakChecker.DataParser.Format;

public static class HeaderGuesser
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
            ItemEnum.Ipv4, [
                new("ip", MatchPolicy.Token),
                new("ip4", MatchPolicy.Substring),
                new("ipv4", MatchPolicy.Substring),
                new("ipv4addr", MatchPolicy.Substring),
                new("ipv4-addr", MatchPolicy.Substring),
                new("ipv4_addr", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Ipv6, [
                new("ip6", MatchPolicy.Substring),
                new("ipv6", MatchPolicy.Substring),
                new("ipv6addr", MatchPolicy.Substring),
                new("ipv6-addr", MatchPolicy.Substring),
                new("ipv6_addr", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.MacAddress, [
                new("mac", MatchPolicy.Token),
                new("macaddr", MatchPolicy.Substring),
                new("macaddress", MatchPolicy.Substring),
                new("mac-address", MatchPolicy.Substring),
                new("mac_address", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Timestamp, [
                new("at", MatchPolicy.Token),
                new("ts", MatchPolicy.Token),
                new("time", MatchPolicy.Substring),
                new("timestamp", MatchPolicy.Substring),
                new("date", MatchPolicy.Substring),
                new("datetime", MatchPolicy.Substring),
                new("date-time", MatchPolicy.Substring),
                new("date_time", MatchPolicy.Substring),
                new("dateofbirth", MatchPolicy.Substring),
                new("date-of-birth", MatchPolicy.Substring),
                new("date_of_birth", MatchPolicy.Substring),
                new("created", MatchPolicy.Token),
                new("modified", MatchPolicy.Token),
                new("modifiedon", MatchPolicy.Token),
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
                new("gps", MatchPolicy.Token),
                new("position", MatchPolicy.Token),
                new("coordinates", MatchPolicy.Substring),
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
                new("phno", MatchPolicy.Token),
                new("phone", MatchPolicy.Token),
                new("phonenumber", MatchPolicy.Substring),
                new("phone-number", MatchPolicy.Substring),
                new("phone_number", MatchPolicy.Substring),
                new("telephone", MatchPolicy.Substring),
                new("telephonenum", MatchPolicy.Substring),
                new("telephone-num", MatchPolicy.Substring),
                new("telephone_num", MatchPolicy.Substring),
                new("mobile", MatchPolicy.Token),
                new("mobilenum", MatchPolicy.Substring),
                new("mobile-num", MatchPolicy.Substring),
                new("mobile_num", MatchPolicy.Substring),
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
                new("iban", MatchPolicy.Substring),
                new("bankaccount", MatchPolicy.Substring),
                new("bank-account", MatchPolicy.Substring),
                new("bank_account", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Gender, [
                new("gender", MatchPolicy.Substring),
                new("usergender", MatchPolicy.Exact),
                new("user-gender", MatchPolicy.Exact),
                new("user_gender", MatchPolicy.Exact),
                new("sex", MatchPolicy.Token),
                new("usersex", MatchPolicy.Exact),
                new("user-sex", MatchPolicy.Exact),
                new("user_sex", MatchPolicy.Exact),
            ]
        },
        {
            ItemEnum.Name, [
                new("name", MatchPolicy.Token),
                new("firstname", MatchPolicy.Token),
                new("firstname", MatchPolicy.Substring),
                new("lastname", MatchPolicy.Token),
                new("lastname", MatchPolicy.Substring),
                new("realname", MatchPolicy.Substring),
                new("legalname", MatchPolicy.Substring),
                new("surname", MatchPolicy.Token),
                new("fullname", MatchPolicy.Token),
                new("givenname", MatchPolicy.Token),
                new("person", MatchPolicy.Token),
                new("customername", MatchPolicy.Substring),
                new("user-name", MatchPolicy.Substring),
                new("user_name", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Username, [
                new("username", MatchPolicy.Substring),
                new("accountname", MatchPolicy.Substring),
                new("user-login", MatchPolicy.Substring),
                new("user_login", MatchPolicy.Substring),
                new("user_nicename", MatchPolicy.Substring),
                new("userdisplayname", MatchPolicy.Substring),
                new("user_displayname", MatchPolicy.Substring),
                new("user_display_name", MatchPolicy.Substring),
                new("nick", MatchPolicy.Substring),
                new("nickname", MatchPolicy.Substring),
                new("nick-name", MatchPolicy.Substring),
                new("nick_name", MatchPolicy.Substring),
                new("nicename", MatchPolicy.Substring),
                new("displayname", MatchPolicy.Substring),
                new("display-name", MatchPolicy.Substring),
                new("display_name", MatchPolicy.Substring),
                new("alias", MatchPolicy.Substring),
                new("loginname", MatchPolicy.Substring),
                new("login_name", MatchPolicy.Substring),
                new("screenname", MatchPolicy.Substring),
                new("screen_name", MatchPolicy.Substring),
                new("forum_user", MatchPolicy.Substring),
                new("forumnickname", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Password, [
                new("pwd", MatchPolicy.Token),
                new("pass", MatchPolicy.Token),
                new("passwd", MatchPolicy.Token),
                new("password", MatchPolicy.Substring),
                new("passcode", MatchPolicy.Substring),
                new("passwordval", MatchPolicy.Substring),
                new("password-val", MatchPolicy.Substring),
                new("password_val", MatchPolicy.Substring),
                new("passwordvalue", MatchPolicy.Substring),
                new("password-value", MatchPolicy.Substring),
                new("password_value", MatchPolicy.Substring),
                new("passwordkey", MatchPolicy.Substring),
                new("user-pwd", MatchPolicy.Substring),
                new("user_pwd", MatchPolicy.Substring),
                new("userpass", MatchPolicy.Substring),
                new("user_pass", MatchPolicy.Substring),
                new("user-passwd", MatchPolicy.Substring),
                new("user_passwd", MatchPolicy.Substring),
                new("userpassword", MatchPolicy.Substring),
                new("user-password", MatchPolicy.Substring),
                new("user_password", MatchPolicy.Substring),
                new("usersecret", MatchPolicy.Substring),
                new("user-secret", MatchPolicy.Substring),
                new("user_secret", MatchPolicy.Substring),
                new("loginsecret", MatchPolicy.Substring),
                new("login-secret", MatchPolicy.Substring),
                new("login_secret", MatchPolicy.Substring),
                new("gamepin", MatchPolicy.Substring),
                new("gamepassword", MatchPolicy.Substring),
                new("secret", MatchPolicy.Token),
            ]
        },
        {
            ItemEnum.Salt, [
                new("salting", MatchPolicy.Substring),
                new("salt", MatchPolicy.Token),
                new("saltvalue", MatchPolicy.Substring),
            ]
        },
        {
            ItemEnum.Token, [
                new("token", MatchPolicy.Token),
                new("key", MatchPolicy.Token),
                new("api_key", MatchPolicy.Exact),
                new("api_key", MatchPolicy.Substring),
                // new("secret", MatchPolicy.Token),
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
    /// Attempts to guess ItemEnum types from a list of headers.
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
        if (string.IsNullOrWhiteSpace(header))
        {
            Console.WriteLine("Header have empty label");
            return ItemEnum.Null;
        }
        
        // FIRST PASS: true semantic exact matches (ignore policy)
        foreach (var (itemType, keywords) in Synonyms)
        {
            foreach (var keyword in keywords)
            {
                if (string.Equals(keyword.Keyword, header, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Console.WriteLine($"{header} -> EXACT MATCH ({itemType})");
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
                                score += 2; // middle tokens still add small bonus
                        }
                        break;

                    case MatchPolicy.Substring:
                        if (lowerHeader.Contains(keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            if (lowerHeader.EndsWith(keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                score += 3;
                            } 
                            else if (lowerHeader.StartsWith(keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                score += 1;
                            }
                            else
                            {
                                score += 2; // existing base only, no positional bonus
                            }
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
        
        return bestScore > 1 ? bestMatch : ItemEnum.Other;
    }
    
    private static List<string> TokenizeHeader(string header)
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

    public static Dictionary<int, ItemEnum> BindGuessed(Dictionary<int, ItemEnum> schema, Dictionary<int, ItemEnum> guessed)
    {
        foreach (var (index, guess) in guessed)
        {
            if (!schema.TryGetValue(index, out ItemEnum existing) || 
                existing == ItemEnum.Other || existing == ItemEnum.Null)
            {
                schema[index] = guess;
            }
        }

        return schema;
    }
}