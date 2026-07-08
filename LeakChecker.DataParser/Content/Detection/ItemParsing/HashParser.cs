using System.Buffers.Text;
using System.Text.Json;
using LeakChecker.Common.Enums;

namespace LeakChecker.DataParser.Content.Detection.ItemParsing;

public static class HashParser
{
    private static readonly HttpClient Client = new();
    private const string BaseUrl = "https://hashes.com/en/api/identifier?hash=";

    public static async Task<ItemType?> TryParse(string token)
    {
        string requestUrl = BaseUrl + Uri.EscapeDataString(token);  //Return one best algorithm
        string requestUrlExtended = requestUrl + "&extended=true";  //Offers more possible algorithms sorted by its popularity

        var response = await Client.GetStringAsync(requestUrlExtended);

        var jsonDoc = JsonDocument.Parse(response);
        var root = jsonDoc.RootElement;
        
        bool isHash = root.GetProperty("success").GetBoolean();
        if (!isHash)
            return null;
        
        var algorithms = root.GetProperty("algorithms").EnumerateArray();
            
        string hashName = algorithms.First().ToString();
            
        if (hashName.Trim().StartsWith("Base64", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!Base64.IsValid(token, out _)) 
                return null;
        }
            
        ItemType hashType = HashTypeMap.GetValueOrDefault(hashName.Trim(), ItemType.Null);
            
        if (hashType != ItemType.Null)
            return hashType;
        
        return null;
    }

    private static readonly Dictionary<string, ItemType> HashTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // MD4
        ["MD4"] = ItemType.MD4,
        
        // MD5
        ["MD5"] = ItemType.MD5,
        ["half md5"] = ItemType.HALF_MD5,
        ["md5(md5($plaintext))"] = ItemType.MD5_MD5_PLAINTEXT,
        ["md5(sha1($plaintext))"] = ItemType.MD5_SHA1_PLAINTEXT,
        ["md5($plaintext.$salt)"] = ItemType.MD5_PLAINTEXT_SALT,
        ["md5($salt.$plaintext)"] = ItemType.MD5_SALT_PLAINTEXT,
        ["Apache $apr1$ MD5, md5apr1, MD5 (APR)"] = ItemType.MD5_APR1,
        ["md5(md5($plaintext).md5($salt))"] = ItemType.MD5_MD5_PLAINTEXT_MD5_SALT,
        ["md5($salt.md5($plaintext.$salt))"] = ItemType.MD5_SALT_MD5_PLAINTEXT_SALT,
        ["md5(sha1($salt).md5($plaintext))"] = ItemType.MD5_SHA1_SALT_MD5_PLAINTEXT,
        ["md5(strtoupper(md5($plaintext)))"] = ItemType.MD5_STRTOUPPER_MD5_PLAINTEXT,
        ["md5($salt.sha1($salt.$plaintext))"] = ItemType.MD5_SALT_SHA1_SALT_PLAINTEXT,
        ["md5crypt, md5 (unix), cisco-ios $1$ (md5), cisco-ios $1$ (md5)"] = ItemType.MD5_CRYPT,
        ["md5(sha1($plaintext).md5($plaintext).sha1($plaintext))"] = ItemType.MD5_SHA1_PLAINTEXT_MD5_PLAINTEXT_SHA1_PLAINTEXT,
        
        // SHA1
        ["SHA1"] = ItemType.SHA1,
        ["sha1(CX)"] = ItemType.SHA1_CX,
        ["sha1(md5($pass))"] = ItemType.SHA1_MD5_PLAINTEXT,
        ["sha1($salt.$plaintext)"] = ItemType.SHA1_SALT_PLAINTEXT,
        ["sha1($plaintext.$salt)"] = ItemType.SHA1_PLAINTEXT_SALT,
        ["sha1(utf16-le($plaintext))"] = ItemType.SHA1_UTF16LE_PLAINTEXT, // Hashcat Hash-Name "sha1(utf16le($plaintext))"
        ["sha1(md5($plaintext.$salt))"] = ItemType.SHA1_MD5_PLAINTEXT_SALT,
        ["sha1(utf16le($plaintext).$salt)"] = ItemType.SHA1_UTF16LE_PLAINTEXT_SALT,
        ["sha1($salt.utf16le($plaintext))"] = ItemType.SHA1_SALT_UTF16LE_PLAINTEXT,
        ["nsldap, SHA-1(Base64), Netscape LDAP SHA"] = ItemType.SHA1_BASE64_NSLDAP,
        ["nsldaps, SSHA-1(Base64), Netscape LDAP SSHA"] = ItemType.SSHA1_BASE64_NSLDAPS,
        ["sha1($salt.sha1($plaintext.$salt))"] = ItemType.SHA1_SALT_SHA1_PLAINTEXT_SALT,
        ["sha1.substr(0, 32)"] = ItemType.SHA1_SUBSTR32,
        
        // SHA2
        ["sha224"] = ItemType.SHA224,   // Hashcat Hash-Name "SHA2-224"
        ["sha256"] = ItemType.SHA256,   // Hashcat Hash-Name "SHA2-256"
        ["sha256(md5($plaintext))"] = ItemType.SHA256_MD5_PLAINTEXT,
        ["sha256($plaintext.$salt)"] = ItemType.SHA256_PLAINTEXT_SALT,
        ["sha256($salt.$plaintext)"] = ItemType.SHA256_SALT_PLAINTEXT,
        ["sha256crypt $5$, sha256 (unix)"] = ItemType.SHA256_CRYPT,
        ["sha256($salt.$plaintext.$salt)"] = ItemType.SHA256_SALT_PLAINTEXT_SALT,
        ["sha256(sha256($plaintext).$salt)"] = ItemType.SHA256_SHA256_PLAINTEXT_SALT,
        ["sha384"] = ItemType.SHA384,   // Hashcat Hash-Name "SHA2-384"
        ["sha512"] = ItemType.SHA512,   // Hashcat Hash-Name "SHA2-512"
        ["sha512crypt $6$, sha512 (unix)"] = ItemType.SHA512_CRYPT,
        ["sha512($plaintext.$salt)"] = ItemType.SHA512_PLAINTEXT_SALT,
        ["sha512($salt.$plaintext)"] = ItemType.SHA512_SALT_PLAINTEXT,
        ["ssha-512(base64), ldap {ssha512}"] = ItemType.SSHA512_BASE64,
        
        // SHA3
        ["SHA3-224"] = ItemType.SHA3_224,
        ["SHA3-256"] = ItemType.SHA3_256,
        ["SHA3-384"] = ItemType.SHA3_384,
        ["SHA3-512"] = ItemType.SHA3_512,
        
        // KECCAK
        ["Keccak-224"] = ItemType.KECCAK224,
        ["Keccak-256"] = ItemType.KECCAK256,
        ["Keccak-384"] = ItemType.KECCAK384,
        ["Keccak-512"] = ItemType.KECCAK512,
        
        // HMAC
        ["HMAC-MD5 (key = $salt)"] = ItemType.HMAC_MD5_SALT,
        ["HMAC-MD5 (key = $plaintext)"] = ItemType.HMAC_MD5_PLAINTEXT,
        ["HMAC-SHA1 (key = $salt)"] = ItemType.HMAC_SHA1_SALT,
        ["HMAC-SHA256 (key = $salt)"] = ItemType.HMAC_SHA256_SALT,
        ["HMAC-SHA256 (key = $plaintext)"] = ItemType.HMAC_SHA256_PLAINTEXT,
        ["HMAC-SHA512 (key = $salt)"] = ItemType.HMAC_SHA512_SALT,
        ["HMAC-SHA512 (key = $plaintext)"] = ItemType.HMAC_SHA512_PLAINTEXT,
        ["HMAC-SHA256($salt.$plaintext key = $secret)"] = ItemType.HMAC_SHA256_SALT_PLAINTEXT_SECRET,
        
        // PBKDF2
        ["PBKDF2-HMAC-MD5"] = ItemType.PBKDF2_HMAC_MD5,
        ["PBKDF2-HMAC-SHA1"] = ItemType.PBKDF2_HMAC_SHA1,
        ["PBKDF2-HMAC-SHA256"] = ItemType.PBKDF2_HMAC_SHA256,
        ["PBKDF2-HMAC-SHA512"] = ItemType.PBKDF2_HMAC_SHA512,
        
        // BCRYPT
        ["bcrypt(md5($plaintext))"] = ItemType.BCRYPT_MD5,
        ["bcrypt(sha256($plaintext))"] = ItemType.BCRYPT_SHA256,
        ["bcrypt $2*$, blowfish (unix)"] = ItemType.BCRYPT_BLOWFISH_UNIX,
        
        // LANMAN / NTLM
        ["LM"] = ItemType.LM,
        ["NTLM"] = ItemType.NTLM,
        
        // MYSQL
        ["MYSQL323"] = ItemType.MYSQL323,
        ["MYSQL4.1/mysql5"] = ItemType.MYSQL41_5,
        ["MySQL $A$ (sha256crypt)"] = ItemType.MYSQL_A_SHA256CRYPT,

        // MSSQL
        ["MSSQL (2005)"] = ItemType.MSSQL2005,
        ["MSSQL (2012, 2014)"] = ItemType.MSSQL2012_2014,
        
        // DJANGO
        ["Django (SHA-1)"] = ItemType.DJANGO_SHA1,
        ["Django (PBKDF2-SHA256)"] = ItemType.DJANGO_PBKDF2_SHA256,
        
        // PHPASS
        ["phpass(md5($plaintext))/phpbb3md5"] = ItemType.PHPBB3_MD5,   // Hashcat Hash-Name "phpass, phpBB3 (MD5)"
        ["phpass, phpBB3 (MD5), Joomla >= 2.5.18 (MD5), WordPress (MD5)"] = ItemType.PHPASS_WORDPRESS_MD5, // Hashcat Hash-Name "phpass, WordPress (MD5), Joomla (MD5)"
        
        // CMS
        ["Joomla < 2.5.18"] = ItemType.JOOMLA_BEFORE_2_5_18,
        ["vBulletin < v3.8.5"] = ItemType.VBULLETIN_BEFORE_3_8_5,
        ["vBulletin >= v3.8.5"] = ItemType.VBULLETIN_AFTER_3_8_5,
        ["MyBB 1.2+, IPB2+ (Invision Power Board)"] = ItemType.MYBB_IPB2,
        
        // METAMASK WALLET
        ["MetaMask Wallet"] = ItemType.METAMASK_WALLET,
        ["MetaMask Wallet (short hash, plaintext check)"] = ItemType.METAMASK_WALLET_SHORT,
        
        // ELECTRUM WALLET
        ["Electrum Wallet (Salt-Type 1-3)"] = ItemType.ELECTRUM_WALLET_1_3,
        ["Electrum Wallet (Salt-Type 4)"] = ItemType.ELECTRUM_WALLET_4,
        ["Electrum Wallet (Salt-Type 5)"] = ItemType.ELECTRUM_WALLET_5,
        
        // ETHEREUM WALLET
        ["Ethereum Wallet, SCRYPT"] = ItemType.ETHEREUM_WALLET_SCRYPT,
        ["Ethereum Wallet, PBKDF2-HMAC-SHA256"] = ItemType.ETHEREUM_WALLET_PBKDF2_HMAC_SHA256,
        ["Ethereum Pre-Sale Wallet, PBKDF2-HMAC-SHA256"] = ItemType.ETHEREUM_PRE_SALE_WALLET_PBKDF2_HMAC_SHA256,
        
        // BITCOIN
        // Other bitcoin hash types always fallback to Bitcoin Public Address 
        ["Bitcoin Public Address"] = ItemType.BITCOIN_PUBLIC_ADDRESS,   // Does not exit in Hashcat Hash-Name
        ["Litecoin Public Address"] = ItemType.LITECOIN_PUBLIC_ADDRESS,
        ["Bitcoin Cash Public Address"] = ItemType.BITCOIN_CASH_PUBLIC_ADDRESS, // Does not exit in Hashcat Hash-Name
        ["Bitcoin/Litecoin wallet.dat"] = ItemType.BITCOIN_LITTLECOIN_WALLET_DAT,
        
        // BLOCKCHAIN
        ["Blockchain, My Wallet"] = ItemType.BLOCKCHAIN_MY_WALLET,
        ["Blockchain, My wallet, v2"] = ItemType.BLOCKCHAIN_MY_WALLET_V2,
        ["Blockchain, My Wallet, Second (SHA256)"] = ItemType.BLOCKCHAIN_MY_WALLET_SECOND_SHA256,
        
        // APPLE
        ["macOS v10.7"] = ItemType.MACOS_V107,
        ["macOS v10.8+ (PBKDF2-SHA512)"] = ItemType.MACOS_V108_PBKDF2_SHA512,
        ["Mac OS X Keychain"] = ItemType.APPLE_KEYCHAIN,
        ["Apple Secure Notes"] = ItemType.APPLE_SECURE_NOTES,
        ["iTunes backup < 10.0"] = ItemType.APPLE_ITUNES_BACKUP_BEFORE_10_0,
        ["iTunes backup >= 10.0"] = ItemType.APPLE_ITUNES_BACKUP_AFTER_10_0,
        ["Apple File System (APFS)"] = ItemType.APPLE_FILE_SYSTEM,
        
        // PDF
        ["PDF 1.1 - 1.3 (Acrobat 2 - 4)"] = ItemType.PDF_11_13_ACROBAT_2_4,
        ["PDF 1.1 - 1.3 (Acrobat 2 - 4), collider #1"] = ItemType.PDF_11_13_ACROBAT_2_4_COLLIDER_1,
        ["PDF 1.1 - 1.3 (Acrobat 2 - 4), collider #2"] = ItemType.PDF_11_13_ACROBAT_2_4_COLLIDER_2,
        ["PDF 1.4 - 1.6 (Acrobat 5 - 8)"] = ItemType.PDF_14_16_ACROBAT_5_8,
        ["PDF 1.7 Level 3 (Acrobat 9)"] = ItemType.PDF_17_LEVEL_3_ACROBAT_9,
        ["PDF 1.7 Level 8 (Acrobat 10 - 11)"] = ItemType.PDF_17_LEVEL_8_ACROBAT_10_11,
        ["PDF Unknown Type"] = ItemType.PDF_UNKNOWN_TYPE,
        
        // MS OFFICE
        ["MS Office 2007"] = ItemType.MS_OFFICE_2007,
        ["MS Office 2010"] = ItemType.MS_OFFICE_2010,
        ["MS Office 2013"] = ItemType.MS_OFFICE_2013,
        ["MS Office 2016 - SheetProtection"] = ItemType.MS_OFFICE_2016_SHEET_PROTECTION,
        ["MS Office <= 2003 $0/$1, MD5 + RC4"] = ItemType.MS_OFFICE_2003_MD5_RC4,   // Hashcat Hash-Name "MS Office ⇐ 2003 MD5 + RC4, oldoffice$0, oldoffice$1"
        ["MS Office <= 2003 $0/$1, MD5 + RC4, collider #1"] = ItemType.MS_OFFICE_2003_MD5_RC4_COLLIDER_1,
        ["MS Office <= 2003 $0/$1, MD5 + RC4, collider #2"] = ItemType.MS_OFFICE_2003_MD5_RC4_COLLIDER_2,
        ["MS Office <= 2003 $3/$4, SHA1 + RC4"] = ItemType.MS_OFFICE_2003_SHA1_RC4, // Hashcat Hash-Name "MS Office ⇐ 2003 SHA1 + RC4, oldoffice$3, oldoffice$4"
        ["MS Office <= 2003 $3, SHA1 + RC4, collider #1"] = ItemType.MS_OFFICE_2003_SHA1_RC4_COLLIDER_1,
        ["MS Office <= 2003 $3, SHA1 + RC4, collider #2"] = ItemType.MS_OFFICE_2003_SHA1_RC4_COLLIDER_2,
        
        // KERBEROS
        ["Kerberos 5, etype 17, DB"] = ItemType.KERBEROS_5_ETYPE_17_DB,
        ["Kerberos 5, etype 18, DB"] = ItemType.KERBEROS_5_ETYPE_18_DB,
        ["Kerberos 5 TGS-REP etype 23"] = ItemType.KERBEROS_5_TGS_REP_ETYPE_23,
        ["Kerberos 5, etype 17, Pre-Auth"] = ItemType.KERBEROS_5_ETYPE_17_PRE_AUTH,
        ["Kerberos 5, etype 18, Pre-Auth"] = ItemType.KERBEROS_5_ETYPE_18_PRE_AUTH,
        ["Kerberos 5 AS-REQ Pre-Auth etype 23"] = ItemType.KERBEROS_5_AS_REQ_PRE_AUTH_ETYPE_23,
        ["Kerberos 5, etype 17, TGS-REP (AES128-CTS-HMAC-SHA1-96)"] = ItemType.KERBEROS_5_ETYPE_17_TGS_REP_AES128_CTS_HMAC_SHA1_96,
        ["Kerberos 5, etype 18, TGS-REP (AES256-CTS-HMAC-SHA1-96)"] = ItemType.KERBEROS_5_ETYPE_18_TGS_REP_AES256_CTS_HMAC_SHA1_96,
        
        // PKZIP
        ["PKZIP (Compressed)"] = ItemType.PKZIP_COMPRESSED,
        ["PKZIP (Uncompressed)"] = ItemType.PKZIP_UNCOMPRESSED,
        ["PKZIP (Mixed Multi-File)"] = ItemType.PKZIP_MIXED_MULTI_FILE,
        ["PKZIP (Compressed Multi-File)"] = ItemType.PKZIP_COMPRESSED_MULTI_FILE,
        ["PKZIP (Compressed Multi-File Checksum-Only)"] = ItemType.PKZIP_MIXED_MULTI_FILE_CHECKSUM_ONLY,
        
        // RSA/DSA/EC/OPENSSH
        ["RSA/DSA/EC/OpenSSH Private Keys ($0$)"] = ItemType.RSA_DSA_EC_OPENSSH_PRIVATE_KEYS_0,
        ["RSA/DSA/EC/OpenSSH Private Keys ($1, $3$)"] = ItemType.RSA_DSA_EC_OPENSSH_PRIVATE_KEYS_1_3,
        ["RSA/DSA/EC/OpenSSH Private Keys ($4$)"] = ItemType.RSA_DSA_EC_OPENSSH_PRIVATE_KEYS_4,
        ["RSA/DSA/EC/OpenSSH Private Keys ($5$)"] = ItemType.RSA_DSA_EC_OPENSSH_PRIVATE_KEYS_5,
        ["RSA/DSA/EC/OpenSSH Private Keys ($6$)"] = ItemType.RSA_DSA_EC_OPENSSH_PRIVATE_KEYS_6,
        
        // AXCRYPT
        ["AxCrypt"] = ItemType.AXCRYPT, // TODO this AxCrypt not detected properly
        ["AxCrypt in-memory SHA1"] = ItemType.AXCRYPT_INMEMORY_SHA1,
        
        // OTHERS
        ["JWT (Json Web Token)"] = ItemType.JWT,
        ["PHPS"] = ItemType.PHPS,
        ["RAR5"] = ItemType.RAR5,
        ["RAR3-hp (*0* only)"] = ItemType.RAR3_HP,
        ["RAR3-p (Compressed)"] = ItemType.RAR3_P_COMPRESSED,
        ["RAR3-p (Uncompressed)"] = ItemType.RAR3_P_UNCOMPRESSED,
        ["7-Zip"] = ItemType.SEVENZIP,
        ["WinZip"] = ItemType.WINZIP,
        ["scrypt"] = ItemType.SCRYPT,
        ["Drupal7"] = ItemType.DRUPAL7,
        ["Radmin2"] = ItemType.RADMIN2,
        ["Radmin3"] = ItemType.RADMIN3,
        ["OpenCart"] = ItemType.OPENCART,
        ["Whirlpool"] = ItemType.WHIRLPOOL,
        ["BitLocker"] = ItemType.BITLOCKER,
        ["NetNTLMv2"] = ItemType.NET_NTLM_V2,
        ["AIX {ssha1}"] = ItemType.AIX_SSHA1,
        ["FileVault 2"] = ItemType.FILEVAULT_2,
        ["Yescrypt $y$"] = ItemType.YESCRYPT,    // Hashcat Hash-Name IS NOT "scrypt [Bridged: Scrypt-Yescrypt]"
        ["AuthMe sha256"] = ItemType.AUTHME_SHA256,
        ["Ansible Vault"] = ItemType.ANSIBLE_VAULT,
        ["Mozilla key3.db"] = ItemType.MOZILLA_KEY3_DB,
        ["Mozilla key4.db"] = ItemType.MOZILLA_KEY4_DB,
        ["GOST R 34.11-94"] = ItemType.GOST_R_34_11_94,
        ["MultiBit HD (scrypt)"] = ItemType.MULTIBIT_HD_SCRYPT,
        ["MultiBit Classic .key (MD5)"] = ItemType.MULTIBIT_CLASSIC_KEY_MD5,
        ["MultiBit Classic .wallet (scrypt)"] = ItemType.MULTIBIT_CLASSIC_WALLET_SCRYPT,
        ["Base64 Encoded String"] = ItemType.BASE64,
        ["Password Safe v3 pwsafe3"] = ItemType.PASSWORD_SAFE_V3, // Hashcat Hash-Name "Password Safe v3"
        ["Android Backup"] = ItemType.ANDROID_BACKUP,
        ["Android FDE (Samsung DEK)"] = ItemType.ANDROID_FDE_SAMSUNG_DEK,
        ["WPA-PMKID-PBKDF2"] = ItemType.WPA_PMKID_PBKDF2,
        ["WPA-PBKDF2-PMKID+EAPOL"] = ItemType.WPA_PBKDF2_PMKID_EAPOL,
        ["IPMI2 RAKP HMAC-SHA1"] = ItemType.IPMI2_RAKP_HMAC_SHA1,
        ["osCommerce, xt:Commerce"] = ItemType.OSCOMMERCE_XTCOMMERCE,
        ["Cisco-IOS $9$ (scrypt)"] = ItemType.CISCO_IOS_9_SCRYPT,
        ["Cisco-IOS $8$ (PBKDF2-SHA256)"] = ItemType.CISCO_IOS_8_PBKDF2_SHA256,
        ["Atlassian (PBKDF2-HMAC-SHA1)"] = ItemType.ATLASSIAN_PBKDF2_HMAC_SHA1,
        ["Exodus Desktop Wallet (scrypt)"] = ItemType.EXODUS_DESKTOP_WALLET_SCRYPT,
        ["BestCrypt v3 Volume Encryption"] = ItemType.BESTCRYPT_V3_VOLUME_ENCRYPTION,
        ["SMF (Simple Machines Forum) > v1.1"] = ItemType.SMF_AFTER_1_1,
        ["Ruby on Rails Restful-Authentication"] = ItemType.RUBY_ON_RAILS_RESTFUL_AUTHENTICATION,
        ["GPG (AES-128/AES-256 (SHA-1($plaintext)))"] = ItemType.GPG_AES_128_AES256_SHA1_PLAINTEXT,
        ["KeePass 1 (AES/Twofish) and KeePass 2 (AES)"] = ItemType.KEEPASS1_AES_TWOFISH_KEEPASS2_AES,
        ["Telegram Desktop >= v2.1.14 (PBKDF2-HMAC-SHA512)"] = ItemType.TELEGRAM_DESKTOP_AFTER_V2114_PBKDF_HMAC_SHA1,
        ["PKCS#8 Private Keys (PBKDF2-HMAC-SHA1 + 3DES/AES)"] = ItemType.PKCS8_PRIVATE_KEYS_PBKDF2_HMAC_SHA1_3DES_AES,
        ["PKCS#8 Private Keys (PBKDF2-HMAC-SHA256 + 3DES/AES)"] = ItemType.PKCS8_PRIVATE_KEYS_PBKDF2_HMAC_SHA256_3DES_AES,
    };
}