using System.Buffers.Text;
using System.Text.Json;

namespace LeakChecker.Content.Detection.ItemParsing;

public static class HashParser
{
    private static readonly HttpClient Client = new();
    private const string BaseUrl = "https://hashes.com/en/api/identifier?hash=";

    public static async Task<(bool isHash, ItemEnum hashType)> TryParse(string token)
    {
        string requestUrl = BaseUrl + Uri.EscapeDataString(token);  //Return one best algorithm
        string requestUrlExtended = requestUrl + "&extended=true";  //Offers more possible algorithms sorted by its popularity

        var response = await Client.GetStringAsync(requestUrlExtended);

        var jsonDoc = JsonDocument.Parse(response);
        var root = jsonDoc.RootElement;

        bool isHash = root.GetProperty("success").GetBoolean();
        if (isHash)
        {
            var algorithms = root.GetProperty("algorithms").EnumerateArray();
            
            string hashName = algorithms.First().ToString();
            // isSalted = hashName.Contains("salt", StringComparison.InvariantCultureIgnoreCase);
            
            if (hashName.Replace(" ", "").Contains("Base64", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!Base64.IsValid(token, out _)) return (false, ItemEnum.Null);
            }
            
            ItemEnum hashType = HashTypeMap.GetValueOrDefault(hashName.Trim(), ItemEnum.Null);
            Console.WriteLine($"Token {token} detected as hash type: {hashName}, item enum: {hashType}");
            return (isHash, hashType);

            
            // Console.WriteLine($"Possible hash algorithms for '{token}':");
            // foreach (var algorithm in algorithms)
            // {
            //     Console.WriteLine($"- {algorithm.GetString()}");
            // }
        }
        else
        {
            // string? message = root.GetProperty("message").GetString();
            // Console.WriteLine($"Token is not hash, token: '{token}'");
        }
        
        return (false, ItemEnum.Null);
    }

    private static readonly Dictionary<string, ItemEnum> HashTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // MD4
        ["md4"] = ItemEnum.MD4,
        
        // MD5
        ["md5"] = ItemEnum.MD5,
        ["half md5"] = ItemEnum.HALF_MD5,
        ["md5(md5($plaintext))"] = ItemEnum.MD5_MD5_PLAINTEXT,
        ["md5(sha1($plaintext))"] = ItemEnum.MD5_SHA1_PLAINTEXT,
        ["md5($plaintext.$salt)"] = ItemEnum.MD5_PLAINTEXT_SALT,
        ["md5($salt.$plaintext)"] = ItemEnum.MD5_SALT_PLAINTEXT,
        ["apache $apr1$ md5, md5apr1, md5 (apr)"] = ItemEnum.MD5_APR1,
        ["md5(md5($plaintext).md5($salt))"] = ItemEnum.MD5_MD5_PLAINTEXT_MD5_SALT,
        ["md5($salt.md5($plaintext.$salt))"] = ItemEnum.MD5_SALT_MD5_PLAINTEXT_SALT,
        ["md5(sha1($salt).md5($plaintext))"] = ItemEnum.MD5_SHA1_SALT_MD5_PLAINTEXT,
        ["md5($salt.sha1($salt.$plaintext))"] = ItemEnum.MD5_SALT_SHA1_SALT_PLAINTEXT,
        ["md5crypt, md5 (unix), cisco-ios $1$ (md5), cisco-ios $1$ (md5)"] = ItemEnum.MD5_CRYPT,
        ["md5(sha1($plaintext).md5($plaintext).sha1($plaintext))"] = ItemEnum.MD5_SHA1_COMBO,

        // LANMAN / NT
        ["lm"] = ItemEnum.LM,
        ["ntlm"] = ItemEnum.NTLM,

        // MYSQL
        ["mysql323"] = ItemEnum.MYSQL323,
        ["mysql4.1/mysql5"] = ItemEnum.MYSQL41_5,

        // SHA1
        ["sha1"] = ItemEnum.SHA1,
        ["sha1($salt.$plaintext)"] = ItemEnum.SHA1_SALT_PLAINTEXT,
        ["sha1($plaintext.$salt)"] = ItemEnum.SHA1_PLAINTEXT_SALT,
        ["sha1(utf16-le($plaintext))"] = ItemEnum.SHA1_UTF16LE_PLAINTEXT, // Hashcat Hash-Name "sha1(utf16le($plaintext))"
        ["sha1(md5($plaintext.$salt))"] = ItemEnum.SHA1_MD5_PLAINTEXT_SALT,
        ["sha1(utf16le($plaintext).$salt)"] = ItemEnum.SHA1_UTF16LE_PLAINTEXT_SALT,
        ["sha1($salt.utf16le($plaintext))"] = ItemEnum.SHA1_SALT_UTF16LE_PLAINTEXT,
        ["nsldap, sha-1(base64), netscape ldap sha"] = ItemEnum.SHA1_BASE64_NSLDAP,
        ["nsldaps, ssha-1(base64), netscape ldap ssha"] = ItemEnum.SSHA1_BASE64_NSLDAPS,
        ["sha1($salt.sha1($plaintext.$salt))"] = ItemEnum.SHA1_SALT_SHA1_PLAINTEXT_SALT,
        ["sha1.substr(0, 32)"] = ItemEnum.SHA1_SUBSTR32,

        // MSSQL
        ["mssql (2005)"] = ItemEnum.MSSQL2005,
        ["mssql (2012, 2014)"] = ItemEnum.MSSQL2012_2014,

        // SHA2
        ["sha224"] = ItemEnum.SHA224,
        ["sha256"] = ItemEnum.SHA256,
        ["sha256(md5($plaintext))"] = ItemEnum.SHA256_MD5_PLAINTEXT,
        ["sha256($plaintext.$salt)"] = ItemEnum.SHA256_PLAINTEXT_SALT,
        ["sha256($salt.$plaintext)"] = ItemEnum.SHA256_SALT_PLAINTEXT,
        ["sha256crypt $5$, sha256 (unix)"] = ItemEnum.SHA256_CRYPT,
        ["sha256($salt.$plaintext.$salt)"] = ItemEnum.SHA256_SALT_PLAINTEXT_SALT,
        ["sha256(sha256($plaintext).$salt)"] = ItemEnum.SHA256_SHA256_PLAINTEXT_SALT,
        ["sha384"] = ItemEnum.SHA384,
        ["sha512"] = ItemEnum.SHA512,
        ["sha512crypt $6$, sha512 (unix)"] = ItemEnum.SHA512_CRYPT,
        ["sha512($plaintext.$salt)"] = ItemEnum.SHA512_PLAINTEXT_SALT,
        ["sha512($salt.$plaintext)"] = ItemEnum.SHA512_SALT_PLAINTEXT,

        // HMAC
        ["hmac-md5 (key = $salt)"] = ItemEnum.HMAC_MD5_SALT,
        ["hmac-md5 (key = $plaintext)"] = ItemEnum.HMAC_MD5_PLAINTEXT,
        ["hmac-sha1 (key = $salt)"] = ItemEnum.HMAC_SHA1_SALT,
        ["hmac-sha256 (key = $salt)"] = ItemEnum.HMAC_SHA256_SALT,
        ["hmac-sha256 (key = $plaintext)"] = ItemEnum.HMAC_SHA256_PLAINTEXT,
        ["hmac-sha512 (key = $salt)"] = ItemEnum.HMAC_SHA512_SALT,
        ["hmac-sha512 (key = $plaintext)"] = ItemEnum.HMAC_SHA512_PLAINTEXT,
        ["hmac-sha256($salt.$plaintext key = $secret)"] = ItemEnum.HMAC_SHA256_SALT_PLAINTEXT_SECRET,

        // PBKDF2
        ["pbkdf2-hmac-md5"] = ItemEnum.PBKDF2_HMAC_MD5,
        ["pbkdf2-hmac-sha1"] = ItemEnum.PBKDF2_HMAC_SHA1,
        ["pbkdf2-hmac-sha256"] = ItemEnum.PBKDF2_HMAC_SHA256,
        ["pbkdf2-hmac-sha512"] = ItemEnum.PBKDF2_HMAC_SHA512,

        // PHPASS
        ["phpass(md5($plaintext))/phpbb3md5"] = ItemEnum.PHPBB3_MD5,   // Hashcat Hash-Name "phpass, phpBB3 (MD5)"
        ["phpass, phpbb3 (md5), joomla >= 2.5.18 (md5), wordpress (md5)"] = ItemEnum.PHPASS_WORDPRESS_MD5, // Hashcat Hash-Name "phpass, WordPress (MD5), Joomla (MD5)"

        // CMS
        ["joomla < 2.5.18"] = ItemEnum.JOOMLA_BEFORE_2_5_18,
        ["vbulletin < v3.8.5"] = ItemEnum.VBULLETIN_BEFORE_3_8_5,
        ["vbulletin >= v3.8.5"] = ItemEnum.VBULLETIN_AFTER_3_8_5,
        ["mybb 1.2+, ipb2+ (invision power board)"] = ItemEnum.MYBB_IPB2,

        // BCRYPT
        ["bcrypt(md5($plaintext))"] = ItemEnum.BCRYPT_MD5,
        ["bcrypt(sha256($plaintext))"] = ItemEnum.BCRYPT_SHA256,
        ["bcrypt $2*$, blowfish (unix)"] = ItemEnum.BCRYPT_BLOWFISH_UNIX,

        // SHA3
        ["sha3-224"] = ItemEnum.SHA3_224,
        ["sha3-256"] = ItemEnum.SHA3_256,
        ["sha3-384"] = ItemEnum.SHA3_384,
        ["sha3-512"] = ItemEnum.SHA3_512,
        
        // KECCAK
        ["keccak-224"] = ItemEnum.KECCAK224,
        ["keccak-256"] = ItemEnum.KECCAK256,
        ["keccak-384"] = ItemEnum.KECCAK384,
        ["keccak-512"] = ItemEnum.KECCAK512,

        // ELECTRUM WALLET
        ["electrum wallet (salt-type 1-3)"] = ItemEnum.ELECTRUM_WALLET_1_3,
        ["electrum wallet (salt-type 4)"] = ItemEnum.ELECTRUM_WALLET_4,
        ["electrum wallet (salt-type 5)"] = ItemEnum.ELECTRUM_WALLET_5,

        // METAMASK WALLET
        ["metamask wallet"] = ItemEnum.METAMASK_WALLET,
        ["metamask wallet (short hash, plaintext check)"] = ItemEnum.METAMASK_WALLET_SHORT,
        
        // DJANGO
        ["django (sha-1)"] = ItemEnum.DJANGO_SHA1,
        ["django (pbkdf2-sha256)"] = ItemEnum.DJANGO_PBKDF2_SHA256,
        
        // ETHEREUM WALLET
        ["ethereum pre-sale wallet, pbkdf2-hmac-sha256"] = ItemEnum.ETHEREUM_PRE_SALE_WALLET_PBKDF2_HMAC_SHA256,
        ["ethereum wallet, scrypt, ethereum pre-sale wallet, pbkdf2-hmac-sha256, decimal ascii"] = ItemEnum.ETHEREUM_WALLET_SCRYPT,  // Hashcat Hash-Name "Ethereum Wallet, SCRYPT"
        ["ethereum wallet, pbkdf2-hmac-sha256, ethereum pre-sale wallet, pbkdf2-hmac-sha256, decimal ascii"] = ItemEnum.ETHEREUM_WALLET_PBKDF2_HMAC_SHA256,  // Hashcat Hash-Name "Ethereum Wallet, PBKDF2-HMAC-SHA256"
        
        // APPLE
        ["mac os x keychain"] = ItemEnum.APPLE_KEYCHAIN,
        ["apple secure notes"] = ItemEnum.APPLE_SECURE_NOTES,
        ["filevault 2, apple file system (apfs)"] = ItemEnum.APPLE_FILE_SYSTEM,
        
        // BITCOIN
        ["bitcoin/litecoin wallet.dat"] = ItemEnum.BITCOIN_LITTLECOIN_WALLET_DAT,
        
        // OTHERS
        ["jwt (json web token), base64 encoded string"] = ItemEnum.JWT, // Hashcat Hash-Name "JWT (JSON Web Token)"
        ["smf (simple machines forum)"] = ItemEnum.SMF,  // Hashcat Hash-Name "SMF (Simple Machines Forum) > v1.1"
        ["opencart"] = ItemEnum.OPENCART,
        ["yescrypt $y$"] = ItemEnum.YESCRYPT,    // Hashcat Hash-Name NOT "scrypt [Bridged: Scrypt-Yescrypt]"
        ["whirlpool"] = ItemEnum.WHIRLPOOL,
        ["authme sha256"] = ItemEnum.AUTHME_SHA256,
        ["password safe v3 pwsafe3"] = ItemEnum.PASSWORDSAFE_V3, // Hashcat Hash-Name "Password Safe v3"
        ["wpa-pmkid-pbkdf2"] = ItemEnum.WPA_PMKID_PBKDF2,
        ["ipmi2 rakp hmac-sha1"] = ItemEnum.IPMI2_RAKP_HMAC_SHA1,
        ["oscommerce, xt:commerce"] = ItemEnum.OSCOMMERCE_XTCOMMERCE,
        ["blockchain, my wallet, v2"] = ItemEnum.BLOCKCHAIN_MY_WALLET_V2,
        ["ruby on rails restful-authentication"] = ItemEnum.RUBY_ON_RAILS_RESTFUL_AUTHENTICATION,
    };
}