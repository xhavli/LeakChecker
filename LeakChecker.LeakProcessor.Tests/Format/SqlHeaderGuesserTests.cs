using LeakChecker.Format;
using LeakChecker.Content;

namespace LeakProcessor.Tests.Format;

public class SqlHeaderGuesserTests
{
    // ---------------- IpV4 ----------------
    [Theory]
    [InlineData("ip", ItemEnum.IpV4)]
    [InlineData("ip4", ItemEnum.IpV4)]
    [InlineData("ipv4", ItemEnum.IpV4)]
    [InlineData("IpV4", ItemEnum.IpV4)]
    [InlineData("client_ip", ItemEnum.IpV4)]
    [InlineData("sourceIp", ItemEnum.IpV4)]
    [InlineData("destinationIp", ItemEnum.IpV4)]
    [InlineData("server_ip4_addr", ItemEnum.IpV4)]
    [InlineData("userIpV4", ItemEnum.IpV4)]
    [InlineData("device-ip4", ItemEnum.IpV4)]
    public void ShouldGuess_IpV4(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- IpV6 ----------------
    [Theory]
    [InlineData("ip6", ItemEnum.IpV6)]
    [InlineData("ipv6", ItemEnum.IpV6)]
    [InlineData("IpV6", ItemEnum.IpV6)]
    [InlineData("client_ip6", ItemEnum.IpV6)]
    [InlineData("sourceIp6", ItemEnum.IpV6)]
    [InlineData("destinationIPV6", ItemEnum.IpV6)]
    [InlineData("server_ip6_addr", ItemEnum.IpV6)]
    [InlineData("userIpv6", ItemEnum.IpV6)]
    [InlineData("device-ip6", ItemEnum.IpV6)]
    [InlineData("networkIpv6", ItemEnum.IpV6)]
    public void ShouldGuess_IpV6(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Mac ----------------
    [Theory]
    [InlineData("mac", ItemEnum.Mac)]
    [InlineData("macaddr", ItemEnum.Mac)]
    [InlineData("mac_address", ItemEnum.Mac)]
    [InlineData("macAddress", ItemEnum.Mac)]
    [InlineData("MACADDRESS", ItemEnum.Mac)]
    [InlineData("device-mac", ItemEnum.Mac)]
    [InlineData("clientMacAddr", ItemEnum.Mac)]
    [InlineData("wifi_mac", ItemEnum.Mac)]
    [InlineData("ethernetMac", ItemEnum.Mac)]
    [InlineData("user-mac-address", ItemEnum.Mac)]
    public void ShouldGuess_Mac(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- TimeStamp ----------------
    [Theory]
    [InlineData("ts", ItemEnum.TimeStamp)]
    [InlineData("time", ItemEnum.TimeStamp)]
    [InlineData("timestamp", ItemEnum.TimeStamp)]
    [InlineData("created", ItemEnum.TimeStamp)]
    [InlineData("updated", ItemEnum.TimeStamp)]
    [InlineData("insertdate", ItemEnum.TimeStamp)]
    [InlineData("date_time", ItemEnum.TimeStamp)]
    [InlineData("lastModified", ItemEnum.TimeStamp)]
    [InlineData("accountCreatedAt", ItemEnum.TimeStamp)]
    [InlineData("user_insert_date", ItemEnum.TimeStamp)]
    public void ShouldGuess_TimeStamp(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Location ----------------
    [Theory]
    [InlineData("location", ItemEnum.Location)]
    [InlineData("latitude", ItemEnum.Location)]
    [InlineData("longitude", ItemEnum.Location)]
    [InlineData("latlng", ItemEnum.Location)]
    [InlineData("address", ItemEnum.Location)]
    [InlineData("city", ItemEnum.Location)]
    [InlineData("country", ItemEnum.Location)]
    [InlineData("state", ItemEnum.Location)]
    [InlineData("region", ItemEnum.Location)]
    [InlineData("user_location", ItemEnum.Location)]
    public void ShouldGuess_Location(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- PhoneNumber ----------------
    [Theory]
    [InlineData("phone", ItemEnum.PhoneNumber)]
    [InlineData("phonenumber", ItemEnum.PhoneNumber)]
    [InlineData("phone_number", ItemEnum.PhoneNumber)]
    [InlineData("mobile", ItemEnum.PhoneNumber)]
    [InlineData("mobilephone", ItemEnum.PhoneNumber)]
    [InlineData("telephone", ItemEnum.PhoneNumber)]
    [InlineData("cell", ItemEnum.PhoneNumber)]
    [InlineData("userPhone", ItemEnum.PhoneNumber)]
    [InlineData("contact_phone", ItemEnum.PhoneNumber)]
    [InlineData("user-mobile-phone", ItemEnum.PhoneNumber)]
    public void ShouldGuess_PhoneNumber(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Email ----------------
    [Theory]
    [InlineData("email", ItemEnum.Email)]
    [InlineData("e-mail", ItemEnum.Email)]
    [InlineData("e_mail", ItemEnum.Email)]
    [InlineData("mail", ItemEnum.Email)]
    [InlineData("userEmail", ItemEnum.Email)]
    [InlineData("account-mail", ItemEnum.Email)]
    [InlineData("contact_email", ItemEnum.Email)]
    [InlineData("EMAIL", ItemEnum.Email)]
    [InlineData("primaryEmail", ItemEnum.Email)]
    [InlineData("customer_mail_addr", ItemEnum.Email)]
    public void ShouldGuess_Email(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Iban ----------------
    [Theory]
    [InlineData("iban", ItemEnum.Iban)]
    [InlineData("bankaccount", ItemEnum.Iban)]
    [InlineData("bank_account", ItemEnum.Iban)]
    [InlineData("bank-account", ItemEnum.Iban)]
    [InlineData("userIban", ItemEnum.Iban)]
    [InlineData("client_iban", ItemEnum.Iban)]
    [InlineData("paymentIban", ItemEnum.Iban)]
    [InlineData("IBAN", ItemEnum.Iban)]
    [InlineData("iban_number", ItemEnum.Iban)]
    [InlineData("recipientBankAccount", ItemEnum.Iban)]
    public void ShouldGuess_Iban(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Gender ----------------
    [Theory]
    [InlineData("gender", ItemEnum.Gender)]
    [InlineData("userGender", ItemEnum.Gender)]
    [InlineData("user_gender", ItemEnum.Gender)]
    [InlineData("GENDER", ItemEnum.Gender)]
    [InlineData("account-gender", ItemEnum.Gender)]
    public void ShouldGuess_Gender(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Name ----------------
    [Theory]
    [InlineData("name", ItemEnum.Name)]
    [InlineData("firstname", ItemEnum.Name)]
    [InlineData("lastname", ItemEnum.Name)]
    [InlineData("surname", ItemEnum.Name)]
    [InlineData("fullname", ItemEnum.Name)]
    [InlineData("givenname", ItemEnum.Name)]
    [InlineData("userNameFull", ItemEnum.Name)]
    [InlineData("contact_name", ItemEnum.Name)]
    [InlineData("customerFullName", ItemEnum.Name)]
    [InlineData("Name", ItemEnum.Name)]
    public void ShouldGuess_Name(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Username ----------------
    [Theory]
    [InlineData("username", ItemEnum.Username)]
    [InlineData("user-name", ItemEnum.Username)]
    [InlineData("user_name", ItemEnum.Username)]
    [InlineData("nickname", ItemEnum.Username)]
    [InlineData("nick", ItemEnum.Username)]
    [InlineData("alias", ItemEnum.Username)]
    [InlineData("displayname", ItemEnum.Username)]
    [InlineData("user_login", ItemEnum.Username)]
    [InlineData("userNicename", ItemEnum.Username)]
    [InlineData("accountNickname", ItemEnum.Username)]
    public void ShouldGuess_Username(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Password ----------------
    [Theory]
    [InlineData("password", ItemEnum.Password)]
    [InlineData("passwd", ItemEnum.Password)]
    [InlineData("pass", ItemEnum.Password)]
    [InlineData("pwd", ItemEnum.Password)]
    [InlineData("userpassword", ItemEnum.Password)]
    [InlineData("user_pass", ItemEnum.Password)]
    [InlineData("user-password", ItemEnum.Password)]
    [InlineData("accountPwd", ItemEnum.Password)]
    [InlineData("Password", ItemEnum.Password)]
    [InlineData("customer_passwd", ItemEnum.Password)]
    public void ShouldGuess_Password(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Hash ----------------
    [Theory]
    [InlineData("hash", ItemEnum.Hash)]
    [InlineData("md5", ItemEnum.Hash)]
    [InlineData("sha1", ItemEnum.Hash)]
    [InlineData("sha256", ItemEnum.Hash)]
    [InlineData("sha512", ItemEnum.Hash)]
    [InlineData("crc32", ItemEnum.Hash)]
    [InlineData("ripemd160", ItemEnum.Hash)]
    [InlineData("whirlpool", ItemEnum.Hash)]
    [InlineData("bcrypt", ItemEnum.Hash)]
    [InlineData("password_hash", ItemEnum.Hash)]
    public void ShouldGuess_Hash(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Salt ----------------
    [Theory]
    [InlineData("salt", ItemEnum.Salt)]
    [InlineData("user_salt", ItemEnum.Salt)]
    [InlineData("passwordSalt", ItemEnum.Salt)]
    [InlineData("SALT", ItemEnum.Salt)]
    [InlineData("account-salt", ItemEnum.Salt)]
    [InlineData("saltValue", ItemEnum.Salt)]
    [InlineData("hash_salt", ItemEnum.Salt)]
    [InlineData("userSaltKey", ItemEnum.Salt)]
    [InlineData("salt_key", ItemEnum.Salt)]
    [InlineData("securitySalt", ItemEnum.Salt)]
    public void ShouldGuess_Salt(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Token ----------------
    [Theory]
    [InlineData("token", ItemEnum.Token)]
    [InlineData("api_key", ItemEnum.Token)]
    [InlineData("auth", ItemEnum.Token)]
    [InlineData("secret", ItemEnum.Token)]
    [InlineData("access_token", ItemEnum.Token)]
    [InlineData("userToken", ItemEnum.Token)]
    [InlineData("auth_token", ItemEnum.Token)]
    [InlineData("TOKEN", ItemEnum.Token)]
    [InlineData("sessionSecret", ItemEnum.Token)]
    [InlineData("client_api_key", ItemEnum.Token)]
    public void ShouldGuess_Token(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Web ----------------
    [Theory]
    [InlineData("web", ItemEnum.Web)]
    [InlineData("webaddr", ItemEnum.Web)]
    [InlineData("webaddress", ItemEnum.Web)]
    [InlineData("url", ItemEnum.Web)]
    [InlineData("link", ItemEnum.Web)]
    [InlineData("site", ItemEnum.Web)]
    [InlineData("website", ItemEnum.Web)]
    [InlineData("page_url", ItemEnum.Web)]
    [InlineData("userWeb", ItemEnum.Web)]
    [InlineData("homepageLink", ItemEnum.Web)]
    public void ShouldGuess_Web(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Id ----------------
    [Theory]
    [InlineData("id", ItemEnum.Id)]
    [InlineData("uid", ItemEnum.Id)]
    [InlineData("bhid", ItemEnum.Id)]
    [InlineData("userid", ItemEnum.Id)]
    [InlineData("user-id", ItemEnum.Id)]
    [InlineData("user_id", ItemEnum.Id)]
    [InlineData("playerid", ItemEnum.Id)]
    [InlineData("player_id", ItemEnum.Id)]
    [InlineData("guid", ItemEnum.Id)]
    [InlineData("sessionId", ItemEnum.Id)]
    public void ShouldGuess_Id(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));

    // ---------------- Other ----------------
    [Theory]
    // ---------------- Lowercase ----------------
    [InlineData("rowversion", ItemEnum.Other)]
    [InlineData("archivedflag", ItemEnum.Other)]
    [InlineData("synctype", ItemEnum.Other)]
    [InlineData("recordstatus", ItemEnum.Other)]
    [InlineData("batchnumber", ItemEnum.Other)]
    [InlineData("errorcode", ItemEnum.Other)]
    [InlineData("retrycount", ItemEnum.Other)]
    [InlineData("rowchecksum", ItemEnum.Other)]
    [InlineData("dataloader", ItemEnum.Other)]
    [InlineData("processingflag", ItemEnum.Other)]
    [InlineData("exportformat", ItemEnum.Other)]
    [InlineData("sourceref", ItemEnum.Other)]
    [InlineData("commenttext", ItemEnum.Other)]
    [InlineData("changereason", ItemEnum.Other)]
    [InlineData("systemnote", ItemEnum.Other)]
    [InlineData("internalref", ItemEnum.Other)]
    [InlineData("loadstatus", ItemEnum.Other)]
    [InlineData("dataquality", ItemEnum.Other)]
    [InlineData("recordscount", ItemEnum.Other)]
    [InlineData("syncmarker", ItemEnum.Other)]
    // ---------------- snake_case ----------------
    [InlineData("row_created_by", ItemEnum.Other)]
    [InlineData("row_updated_by", ItemEnum.Other)]
    [InlineData("source_system", ItemEnum.Other)]
    [InlineData("record_type", ItemEnum.Other)]
    [InlineData("revision_number", ItemEnum.Other)]
    [InlineData("is_deleted", ItemEnum.Other)]
    [InlineData("is_archived", ItemEnum.Other)]
    [InlineData("sync_status", ItemEnum.Other)]
    [InlineData("audit_log_ref", ItemEnum.Other)]
    [InlineData("checksum_value", ItemEnum.Other)]
    [InlineData("legacy_record_ref", ItemEnum.Other)]
    [InlineData("process_flag", ItemEnum.Other)]
    [InlineData("etl_run_ref", ItemEnum.Other)]
    [InlineData("transaction_ref", ItemEnum.Other)]
    [InlineData("display_order", ItemEnum.Other)]
    [InlineData("sort_index", ItemEnum.Other)]
    [InlineData("cache_key", ItemEnum.Other)]
    [InlineData("meta_info", ItemEnum.Other)]
    [InlineData("description_text", ItemEnum.Other)]
    [InlineData("metadata_json", ItemEnum.Other)]
    // ---------------- camelCase ----------------
    [InlineData("rowCreatedBy", ItemEnum.Other)]
    [InlineData("rowUpdatedBy", ItemEnum.Other)]
    [InlineData("sourceSystem", ItemEnum.Other)]
    [InlineData("recordType", ItemEnum.Other)]
    [InlineData("revisionNumber", ItemEnum.Other)]
    [InlineData("isDeleted", ItemEnum.Other)]
    [InlineData("isArchived", ItemEnum.Other)]
    [InlineData("syncStatus", ItemEnum.Other)]
    [InlineData("auditLogRef", ItemEnum.Other)]
    [InlineData("checksumValue", ItemEnum.Other)]
    [InlineData("legacyRecordRef", ItemEnum.Other)]
    [InlineData("processFlag", ItemEnum.Other)]
    [InlineData("etlRunRef", ItemEnum.Other)]
    [InlineData("transactionRef", ItemEnum.Other)]
    [InlineData("displayOrder", ItemEnum.Other)]
    [InlineData("sortIndex", ItemEnum.Other)]
    [InlineData("cacheKey", ItemEnum.Other)]
    [InlineData("extraInfo", ItemEnum.Other)]
    [InlineData("descriptionText", ItemEnum.Other)]
    [InlineData("metadataJson", ItemEnum.Other)]
    // ---------------- PascalCase ----------------
    [InlineData("RowCreatedBy", ItemEnum.Other)]
    [InlineData("RowUpdatedBy", ItemEnum.Other)]
    [InlineData("SourceSystem", ItemEnum.Other)]
    [InlineData("RecordType", ItemEnum.Other)]
    [InlineData("RevisionNumber", ItemEnum.Other)]
    [InlineData("IsDeleted", ItemEnum.Other)]
    [InlineData("IsArchived", ItemEnum.Other)]
    [InlineData("SyncStatus", ItemEnum.Other)]
    [InlineData("AuditLogRef", ItemEnum.Other)]
    [InlineData("ChecksumValue", ItemEnum.Other)]
    [InlineData("LegacyRecordRef", ItemEnum.Other)]
    [InlineData("ProcessFlag", ItemEnum.Other)]
    [InlineData("EtlRunRef", ItemEnum.Other)]
    [InlineData("TransactionRef", ItemEnum.Other)]
    [InlineData("DisplayOrder", ItemEnum.Other)]
    [InlineData("SortIndex", ItemEnum.Other)]
    [InlineData("CacheKey", ItemEnum.Other)]
    [InlineData("ExtraInfo", ItemEnum.Other)]
    [InlineData("DescriptionText", ItemEnum.Other)]
    [InlineData("MetadataJson", ItemEnum.Other)]

    public void ShouldGuess_Other(string header, ItemEnum expected)
        => Assert.Equal(expected, SqlHeaderGuesser.GuessItem(header));
}
