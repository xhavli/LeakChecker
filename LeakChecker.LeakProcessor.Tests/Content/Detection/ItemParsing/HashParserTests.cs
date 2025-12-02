using LeakChecker.Content;
using LeakChecker.Content.Detection.ItemParsing;

namespace LeakProcessor.Tests.Content.Detection.ItemParsing;

public class HashParserTests
{
    [Theory]

    // MD4
    [InlineData("afe04867ec7a3845145579a95f72eca7", ItemEnum.MD4)]
    
    // MD5
    [InlineData("8743b52063cd84097a65d1633f5c74f5", ItemEnum.MD5)]
    [InlineData("8743b52063cd8409", ItemEnum.HALF_MD5)]
    // [InlineData("a936af92b0ae20b1ff6c3347a72e5fbe", ItemEnum.MD5_MD5_PLAINTEXT)] // hashes.com mismatch Possible algorithms: MD5
    [InlineData("288496df99b33f8f75a7ce4837d1b480", ItemEnum.MD5_SHA1_PLAINTEXT)]
    // [InlineData("01dfae6e5d4d90d9892622325959afbe:7050461", ItemEnum.MD5_PLAINTEXT_SALT)]    // hashes.com mismatch Possible algorithms: MD5 
    // [InlineData("f0fda58630310a6dd91a7d8f0a4ceda2:4225637426", ItemEnum.MD5_SALT_PLAINTEXT)] // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    [InlineData("$apr1$71850310$gh9m4xcAn3MGxogwX/ztb.", ItemEnum.MD5_APR1)]
    // [InlineData("250920b3a5e31318806a032a4674df7e:1234", ItemEnum.MD5_MD5_PLAINTEXT_MD5_SALT)]   // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    // [InlineData("b4cb5c551a30f6c25d648560408df68a:1234", ItemEnum.MD5_SALT_MD5_PLAINTEXT_SALT)]  // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    // [InlineData("e69b7a7fe1bf2ad9ef116f79551ee919:baa038987e582431a6d", ItemEnum.MD5_SHA1_SALT_MD5_PLAINTEXT)]   // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    // [InlineData("b8c385461bb9f9d733d3af832cf60b27", ItemEnum.MD5_STRTOUPPER_MD5_PLAINTEXT)]   // hashes.com mismatch Possible algorithms: MD5...
    // [InlineData("799dc7d9aa4d3f404cc21a4936dbdcde:68617368636174", ItemEnum.MD5_SALT_SHA1_SALT_PLAINTEXT)]   // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    [InlineData("$1$28772684$iEwNOgGugqO9.bIz5sk8k/", ItemEnum.MD5_CRYPT)]
    // [InlineData("100b3a4fc1dc8d60d9bf40688d8b740a", ItemEnum.MD5_SHA1_PLAINTEXT_MD5_PLAINTEXT_SHA1_PLAINTEXT)]   // hashes.com mismatch Possible algorithms: MD5
    
    // SHA1
    [InlineData("b89eaac7e61417341b710b727768294d0e6a277b", ItemEnum.SHA1)]
    // [InlineData("92d85978d884eb1d99a51652b1139c8279fa8663", ItemEnum.SHA1_MD5_PLAINTEXT)]    // hashes.com mismatch Possible algorithms: SHA1...
    // [InlineData("cac35ec206d868b7d7cb0b55f31d9425b075082b:5363620024", ItemEnum.SHA1_SALT_PLAINTEXT)]    // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    // [InlineData("2fc5a684737ce1bf7b3b239df432416e0dd07357:2014", ItemEnum.SHA1_PLAINTEXT_SALT)]  // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    // [InlineData("b9798556b741befdbddcbf640d1dd59d19b1e193", ItemEnum.SHA1_UTF16LE_PLAINTEXT)]    // hashes.com mismatch Possible algorithms: SHA1...
    // [InlineData("53c724b7f34f09787ed3f1b316215fc35c789504:hashcat1", ItemEnum.SHA1_MD5_PLAINTEXT_SALT)]  // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    // [InlineData("c57f6ac1b71f45a07dbd91a59fa47c23abcd87c2:631225", ItemEnum.SHA1_UTF16LE_PLAINTEXT_SALT)]    // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    // [InlineData("5db61e4cd8776c7969cfd62456da639a4c87683a:8763434884872", ItemEnum.SHA1_SALT_UTF16LE_PLAINTEXT)] // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    [InlineData("{SHA}uJ6qx+YUFzQbcQtyd2gpTQ5qJ3s=", ItemEnum.SHA1_BASE64_NSLDAP)]
    [InlineData("{SSHA}AZKja92fbuuB9SpRlHqaoXxbTc43Mzc2MDM1Ng==", ItemEnum.SSHA1_BASE64_NSLDAPS)]
    // [InlineData("94520b02c04e79e08a75a84c2a6e3ed4e3874fe8:ThisIsATestSalt", ItemEnum.SHA1_SALT_SHA1_PLAINTEXT_SALT)] // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    // [InlineData("", ItemEnum.SHA1_SUBSTR32)] // Hashcat cant recognize this type of hash
    
    // SHA2
    [InlineData("e4fa1555ad877bf0ec455483371867200eee89550a93eff2f95a6198", ItemEnum.SHA224)]
    [InlineData("127e6fbfe24a750e72930c220a8e138275656b8e5d8f48a98c3c92df2caba935", ItemEnum.SHA256)]
    // [InlineData("74ee1fae245edd6f27bf36efc3604942479fceefbadab5dc5c0b538c196eb0f1", ItemEnum.SHA256_MD5_PLAINTEXT)]  // hashes.com mismatch Possible algorithms: SHA256...
    [InlineData("c73d08de890479518ed60cf670d17faa26a4a71f995c1dcc978165399401a6c4:53743528", ItemEnum.SHA256_PLAINTEXT_SALT)]
    // [InlineData("eb368a2dfd38b405f014118c7d9747fcc97f4f0ee75c05963cd9da6ee65ef498:560407001617", ItemEnum.SHA256_SALT_PLAINTEXT)]    // hashes.com mismatch Possible algorithms: sha256($plaintext.$salt)...
    [InlineData("$5$rounds=5000$GX7BopJZJxPc/KEK$le16UF8I2Anb.rOrn22AUPWvzUETDGefUmAV8AZkGcD", ItemEnum.SHA256_CRYPT)]
    // [InlineData("755a8ce4e0cf0baee41d714aa35c9fca803106608f718f973eab006578285007:11265", ItemEnum.SHA256_SALT_PLAINTEXT_SALT)]  // hashes.com mismatch Possible algorithms: sha256($plaintext.$salt)...
    // [InlineData("bfede293ecf6539211a7305ea218b9f3f608953130405cda9eaba6fb6250f824:7218532375810603", ItemEnum.SHA256_SHA256_PLAINTEXT_SALT)] // hashes.com mismatch Possible algorithms: sha256($plaintext.$salt)...
    // [InlineData("07371af1ca1fca7c6941d2399f3610f1e392c56c6d73fddffe38f18c430a2817028dae1ef09ac683b62148a2c8757f42", ItemEnum.SHA384)]    // hashes.com mismatch Possible algorithms: SHA3-384
    [InlineData("82a9dda829eb7f8ffe9fbe49e45d47d2dad9664fbb7adf72492e3c81ebd3e29134d9bc12212bf83c6840f10e8246b9db54a4859b7ccd0123d86e5872c1e5082f", ItemEnum.SHA512)]
    [InlineData("$6$52450745$k5ka2p8bFuSmoVT1tzOyyuaREkkKBcCNqoDKzYiJL9RaE8yMnPgh2XzzF0NDrUhgrcLwg78xs1w5pJiypEdFX/", ItemEnum.SHA512_CRYPT)]
    [InlineData("e5c3ede3e49fb86592fb03f471c35ba13e8d89b8ab65142c9a8fdafb635fa2223c24e5558fd9313e8995019dcbec1fb584146b7bb12685c7765fc8c0d51379fd:6352283260", ItemEnum.SHA512_PLAINTEXT_SALT)]
    // [InlineData("976b451818634a1e2acba682da3fd6efa72adf8a7a08d7939550c244b237c72c7d42367544e826c0c83fe5c02f97c0373b6b1386cc794bf0d21d2df01bb9c08a:2613516180127", ItemEnum.SHA512_SALT_PLAINTEXT)]   // hashes.com mismatch Possible algorithms: sha256($plaintext.$salt)...
    [InlineData("{SSHA512}ALtwKGBdRgD+U0fPAy31C28RyKYx7+a8kmfksccsOeLknLHv2DBXYI7TDnTolQMBuPkWDISgZr2cHfnNPFjGZTEyNDU4OTkw", ItemEnum.SSHA512_BASE64)]
    
    // SHA3
    // [InlineData("412ef78534ba6ab0e9b1607d3e9767a25c1ea9d5e83176b4c2817a6c", ItemEnum.SHA3_224)]  // hashes.com mismatch Possible algorithms: SHA224...
    // [InlineData("d60fcf6585da4e17224f58858970f0ed5ab042c3916b76b0b828e62eaf636cbd", ItemEnum.SHA3_256)]  // hashes.com mismatch Possible algorithms: SHA256...
    [InlineData("07371af1ca1fca7c6941d2399f3610f1e392c56c6d73fddffe38f18c430a2817028dae1ef09ac683b62148a2c8757f42", ItemEnum.SHA3_384)]
    // [InlineData("7c2dc1d743735d4e069f3bda85b1b7e9172033dfdd8cd599ca094ef8570f3930c3f2c0b7afc8d6152ce4eaad6057a2ff22e71934b3a3dd0fb55a7fc84a53144e", ItemEnum.SHA3_512)]  // hashes.com mismatch Possible algorithms: SHA512...
    
    // KECCAK
    // [InlineData("e1dfad9bafeae6ef15f5bbb16cf4c26f09f5f1e7870581962fc84636", ItemEnum.KECCAK224)] // hashes.com mismatch Possible algorithms: SHA224...
    // [InlineData("203f88777f18bb4ee1226627b547808f38d90d3e106262b5de9ca943b57137b6", ItemEnum.KECCAK256)] // hashes.com mismatch Possible algorithms: SHA256...
    // [InlineData("5804b7ada5806ba79540100e9a7ef493654ff2a21d94d4f2ce4bf69abda5d94bf03701fe9525a15dfdc625bfbd769701", ItemEnum.KECCAK384)] // hashes.com mismatch Possible algorithms: SHA384...
    // [InlineData("2fbf5c9080f0a704de2e915ba8fdae6ab00bbc026b2c1c8fa07da1239381c6b7f4dfd399bf9652500da723694a4c719587dd0219cb30eabe61210a8ae4dc0b03", ItemEnum.KECCAK512)] // hashes.com mismatch Possible algorithms: SHA512...
    
    // HMAC
    // [InlineData("bfd280436f45fa38eaacac3b00518f29:1234", ItemEnum.HMAC_MD5_SALT)]    // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    // [InlineData("fc741db0a2968c39d9c2a5cc75b05370:1234", ItemEnum.HMAC_MD5_PLAINTEXT)]   // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    // [InlineData("d89c92b4400b15c39e462a8caa939ab40c3aeeea:1234", ItemEnum.HMAC_SHA1_SALT)]   // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    // [InlineData("8efbef4cec28f228fa948daaf4893ac3638fbae81358ff9020be1d7a9a509fc6:1234", ItemEnum.HMAC_SHA256_SALT)] // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    // [InlineData("abaf88d66bf2334a4a8b207cc61a96fb46c3e38e882e6f6f886742f688b8588c:1234", ItemEnum.HMAC_SHA256_PLAINTEXT)]    // hashes.com mismatch Possible algorithms: sha256($plaintext.$salt)...
    // [InlineData("7cce966f5503e292a51381f238d071971ad5442488f340f98e379b3aeae2f33778e3e732fcc2f7bdc04f3d460eebf6f8cb77da32df25500c09160dd3bf7d2a6b:1234", ItemEnum.HMAC_SHA512_SALT)] // hashes.com mismatch Possible algorithms: sha512($plaintext.$salt)...
    // [InlineData("94cb9e31137913665dbea7b058e10be5f050cc356062a2c9679ed0ad6119648e7be620e9d4e1199220cd02b9efb2b1c78234fa1000c728f82bf9f14ed82c1976:1234", ItemEnum.HMAC_SHA512_PLAINTEXT)]    // hashes.com mismatch Possible algorithms: sha512($plaintext.$salt)...
    // [InlineData("", ItemEnum.HMAC_SHA256_SALT_PLAINTEXT_SECRET)] // Hashcat cant recognize this type of hash
    
    // PBKDF2
    [InlineData("md5:1000:MTg1MzA=:Lz84VOcrXd699Edsj34PP98+f4f3S0rTZ4kHAIHoAjs=", ItemEnum.PBKDF2_HMAC_MD5)]
    [InlineData("sha1:1000:MzU4NTA4MzIzNzA1MDQ=:19ofiY+ahBXhvkDsp0j2ww==", ItemEnum.PBKDF2_HMAC_SHA1)]
    [InlineData("sha256:1000:MTc3MTA0MTQwMjQxNzY=:PYjCU215Mi57AYPKva9j7mvF4Rc5bCnt", ItemEnum.PBKDF2_HMAC_SHA256)]
    [InlineData("sha512:1000:ODQyMDEwNjQyODY=:MKaHNWXUsuJB3IEwBHbm3w==", ItemEnum.PBKDF2_HMAC_SHA512)]
    
    // BCRYPT
    // [InlineData("$2a$05$/VT2Xs2dMd8GJKfrXhjYP.DkTjOVrY12yDN7/6I8ZV0q/1lEohLru", ItemEnum.BCRYPT_MD5)]    // hashes.com mismatch Possible algorithms: bcrypt $2*$, Blowfish (Unix)...
    // [InlineData("$2b$10$FxDtpTNaL303lLcWtd6LFO2U6Gc63VJ07qycHcfqbQQ71GhO/qSzu", ItemEnum.BCRYPT_SHA256)] // hashes.com mismatch Possible algorithms: bcrypt $2*$, Blowfish (Unix)...
    [InlineData("$2a$05$LhayLxezLhK1LhWvKxCyLOj0j1u.Kj0jZ0pEmm134uzrQlFvQJLF6", ItemEnum.BCRYPT_BLOWFISH_UNIX)]
    
    // LANMAN / NTLM
    // [InlineData("299bd128c1101fd6", ItemEnum.LM)]    // hashes.com mismatch Possible algorithms: MySQL323...
    [InlineData("b4b9b02e6f09a9bd760f388b67351e2b", ItemEnum.NTLM)]
    
    // MYSQL
    [InlineData("7196759210defdc0", ItemEnum.MYSQL323)]
    [InlineData("fcf7c1b8749cf99d88e5f34271d636178fb5d130", ItemEnum.MYSQL41_5)]
    
    // MSSQL
    [InlineData("0x010018102152f8f28c8499d8ef263c53f8be369d799f931b2fbe", ItemEnum.MSSQL2005)]
    [InlineData("0x02000102030434ea1b17802fd95ea6316bd61d2c94622ca3812793e8fb1672487b5c904a45a31b2ab4a78890d563d2fcf5663e46fe797d71550494be50cf4915d3f4d55ec375", ItemEnum.MSSQL2012_2014)]
    
    // PHPASS
    // [InlineData("$H$984478476IagS59wHZvyQMArzfx58u.", ItemEnum.PHPBB3_MD5)]  // hashes.com mismatch Possible algorithms: phpass, phpBB3 (MD5), Joomla >= 2.5.18 (MD5), WordPress (MD5) 
    [InlineData("$P$984478476IagS59wHZvyQMArzfx58u.", ItemEnum.PHPASS_WORDPRESS_MD5)]
    
    // CMS
    // [InlineData("19e0e8d91c722e7091ca7a6a6fb0f4fa:54718031842521651757785603028777", ItemEnum.JOOMLA_BEFORE_2_5_18)] // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    // [InlineData("16780ba78d2d5f02f3202901c1b6d975:568", ItemEnum.VBULLETIN_BEFORE_3_8_5)]    // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    // [InlineData("bf366348c53ddcfbd16e63edfdd1eee6:181264250056774603641874043270", ItemEnum.VBULLETIN_AFTER_3_8_5)]  // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    // [InlineData("8d2129083ef35f4b365d5d87487e1207:47204", ItemEnum.MYBB_IPB2)]   // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    
    // METAMASK WALLET
    [InlineData("$metamask$h0c2mQBGgnhlJ4EWMhdAAZhHlFeZNVlAEwOHQHaEBhY=$q9de9oljOBLWBQRtk9Ugog==$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", ItemEnum.METAMASK_WALLET)]
    [InlineData("$metamask-short$jfGI3TXguhb8GPnKSXFrMzRk2NCEc131Gt5G3kZr5+s=$h+BoIf2CQ5BEjaIOShFE7g==$R95fzGt4UQ0uwrcrVYnIi4UcSlWn9wlmer+//526ZDwYAp50K82F1u1oacYcdjjhuEvbZnWk/uBG00UkgLLlOw==", ItemEnum.METAMASK_WALLET_SHORT)]
    
    // ELECTRUM WALLET
    [InlineData("$electrum$1*44358283104603165383613672586868*c43a6632d9f59364f74c395a03d8c2ea", ItemEnum.ELECTRUM_WALLET_1_3)]
    [InlineData("$electrum$4*03eae309d8bda5dcbddaae8145469193152763894b7260a6c4ba181b3ac2ed5653*8c594086a64dc87a9c1f8a69f646e31e8d3182c3c722def4427aa20684776ac26092c6f60bf2762e27adfa93fe1e952dcb8d6362224b9a371953aa3a2edb596ce5eb4c0879c4353f2cc515ec6c9e7a6defa26c5df346d18a62e9d40fcc606bc8c34322bf2212f77770a683788db0baf4cb43595c2a27fe5ff8bdcb1fd915bcd725149d8ee8f14c71635fecb04da5dde97584f4581ceb7d907dceed80ae5daa8352dda20b25fd6001e99a96b7cf839a36cd3f5656304e6998c18e03dd2fb720cb41386c52910c9cb83272c3d50f3a6ff362ab8389b0c21c75133c971df0a75b331796371b060b32fe1673f4a041d7ae08bbdeffb45d706eaf65f99573c07972701c97766b4d7a8a03bba0f885eb3845dfd9152286e1de1f93e25ce04c54712509166dda80a84c2d34652f68e6c01e662f8b1cc7c15103a4502c29332a4fdbdda470c875809e15aab3f2fcb061ee96992ad7e8ab9da88203e35f47d6e88b07a13b0e70ef76de3be20dc06facbddc1e47206b16b44573f57396265116b4d243e77d1c98bc2b28aa3ec0f8d959764a54ecdd03d8360ff2823577fe2183e618aac15b30c1d20986841e3d83c0bfabcedb7c27ddc436eb7113db927e0beae7522b04566631a090b214660152a4f4a90e19356e66ee7309a0671b2e7bfde82667538d193fc7e397442052c6c611b6bf0a04f629a1dc7fa9eb44bfad1bfc6a0bce9f0564c3b483737e447720b7fd038c9a961a25e9594b76bf8c8071c83fcacd689c7469f698ee4aee4d4f626a73e21ce4967e705e4d83e1145b4260330367d8341c84723a1b02567ffbab26aac3afd1079887b4391f05d09780fc65f8b4f68cd51391c06593919d7eafd0775f83045b8f5c2e59cef902ff500654ea29b7623c7594ab2cc0e05ffe3f10abc46c9c5dac824673c307dcbff5bc5f3774141ff99f6a34ec4dd8a58d154a1c72636a2422b8fafdef399dec350d2b91947448582d52291f2261d264d29399ae3c92dc61769a49224af9e7c98d74190f93eb49a44db7587c1a2afb5e1a4bec5cdeb8ad2aac9728d5ae95600c52e9f063c11cdb32b7c1d8435ce76fcf1fa562bd38f14bf6c303c70fb373d951b8a691ab793f12c0f3336d6191378bccaed32923bba81868148f029e3d5712a2fb9f610997549710716db37f7400690c8dfbed12ff0a683d8e4d0079b380e2fd856eeafb8c6eedfac8fb54dacd6bd8a96e9f8d23ea87252c1a7c2b53efc6e6aa1f0cc30fbaaf68ee7d46666afc15856669cd9baebf9397ff9f322cce5285e68a985f3b6aadce5e8f14e9f9dd16764bc4e9f62168aa265d8634ab706ed40b0809023f141c36717bd6ccef9ec6aa6bfd2d00bda9375c2fee9ebba49590a166*1b0997cf64bb2c2ff88cb87bcacd9729d404bd46db18117c20d94e67c946fedc", ItemEnum.ELECTRUM_WALLET_4)]
    [InlineData("$electrum$5*02170fee7c35f1ef3b229edc90fbd0793b688a0d6f41137a97aab2343d315cce16*94cf72d8f5d774932b414a3344984859e43721268d2eb35fa531de5a2fc7024b463c730a54f4f46229dd9fede5034b19ac415c2916e9c16b02094f845795df0c397ff76d597886b1f9e014ad1a8f64a3f617d9900aa645b3ba86f16ce542251fc22c41d93fa6bc118be96d9582917e19d2a299743331804cfc7ce2c035367b4cbcfb70adfb1e10a0f2795769f2165d8fd13daa8b45eeac495b5b63e91a87f63b42e483f84a881e49adecacf6519cb564694b42dd9fe80fcbc6cdb63cf5ae33f35255266f5c2524dd93d3cc15eba0f2ccdc3c109cc2d7e8f711b8b440f168caf8b005e8bcdfe694148e94a04d2a738f09349a96600bd8e8edae793b26ebae231022f24e96cb158db141ac40400a9e9ef099e673cfe017281537c57f82fb45c62bdb64462235a6eefb594961d5eb2c46537958e4d04250804c6e9f343ab7a0db07af6b8a9d1a6c5cfcd311b8fb8383ac9ed9d98d427d526c2f517fc97473bd87cb59899bd0e8fb8c57fa0f7e0d53daa57c972cf92764af4b1725a5fb8f504b663ec519731929b3caaa793d8ee74293eee27d0e208a60e26290bc546e6fa9ed865076e13febfea249729218c1b5752e912055fbf993fbac5df2cca2b37c5e0f9c30789858ceeb3c482a8db123966775aeed2eee2fc34efb160d164929f51589bff748ca773f38978bff3508d5a7591fb2d2795df983504a788071f469d78c88fd7899cabbc5804f458653d0206b82771a59522e1fa794d7de1536c51a437f5d6df5efd6654678e5794ca429b5752e1103340ed80786f1e9da7f5b39af628b2212e4d88cd36b8a7136d50a6b6e275ab406ba7c57cc70d77d01c4c16e9363901164fa92dc9e9b99219d5376f24862e775968605001e71b000e2c7123b4b43f3ca40db17efd729388782e46e64d43ccb947db4eb1473ff1a3836b74fe312cd1a33b73b8b8d80c087088932277773c329f2f66a01d6b3fc1e651c56959ebbed7b14a21b977f3acdedf1a0d98d519a74b50c39b3052d840106da4145345d86ec0461cddafacc2a4f0dd646457ad05bf04dcbcc80516a5c5ed14d2d639a70e77b686f19cbfb63f546d81ae19cc8ba35cce3f3b5b9602df25b678e14411fecec87b8347f5047513df415c6b1a3d39871a6bcb0f67d9cf8311596deae45fd1d84a04fd58f1fd55c5156b7309af09094c99a53674809cb87a45f95a2d69f9997a38085519cb4e056f9efd56672a2c1fe927d5ea8eec25b8aff6e56f9a2310f1a481daf407b8adf16201da267c59973920fd21bb087b88123ef98709839d6a3ee34efb8ccd5c15ed0e46cff3172682769531164b66c8689c35a26299dd26d09233d1f64f9667474141cf9c6a6de7f2bc52c3bb44cfe679ff4b912c06df406283836b3581773cb76d375304f46239da5996594a8d03b14c02f1b35a432dc44a96331242ae31174*33a7ee59d6d17ed1ee99dc0a71771227e6f3734b17ba36eb589bdced56244135", ItemEnum.ELECTRUM_WALLET_5)]
    
    // ETHEREUM WALLET
    [InlineData("$ethereum$w*e94a8e49deac2d62206bf9bfb7d2aaea7eb06c1a378cfc1ac056cc599a569793c0ecc40e6a0c242dee2812f06b644d70f43331b1fa2ce4bd6cbb9f62dd25b443235bdb4c1ffb222084c9ded8c719624b338f17e0fd827b34d79801298ac75f74ed97ae16f72fccecf862d09a03498b1b8bd1d984fc43dd507ede5d4b6223a582352386407266b66c671077eefc1e07b5f42508bf926ab5616658c984968d8eec25c9d5197a4a30eed54c161595c3b4d558b17ab8a75ccca72b3d949919d197158ea5cfbc43ac7dd73cf77807dc2c8fe4ef1e942ccd11ec24fe8a410d48ef4b8a35c93ecf1a21c51a51a08f3225fbdcc338b1e7fdafd7d94b82a81d88c2e9a429acc3f8a5974eafb7af8c912597eb6fdcd80578bd12efddd99de47b44e7c8f6c38f2af3116b08796172eda89422e9ea9b99c7f98a7e331aeb4bb1b06f611e95082b629332c31dbcfd878aed77d300c9ed5c74af9cd6f5a8c4a261dd124317fb790a04481d93aec160af4ad8ec84c04d943a869f65f07f5ccf8295dc1c876f30408eac77f62192cbb25842470b4a5bdb4c8096f56da7e9ed05c21f61b94c54ef1c2e9e417cce627521a40a99e357dd9b7a7149041d589cbacbe0302db57ddc983b9a6d79ce3f2e9ae8ad45fa40b934ed6b36379b780549ae7553dbb1cab238138c05743d0103335325bd90e27d8ae1ea219eb8905503c5ad54fa12d22e9a7d296eee07c8a7b5041b8d56b8af290274d01eb0e4ad174eb26b23b5e9fb46ff7f88398e6266052292acb36554ccb9c2c03139fe72d3f5d30bd5d10bd79d7cb48d2ab24187d8efc3750d5a24980fb12122591455d14e75421a2074599f1cc9fdfc8f498c92ad8b904d3c4307f80c46921d8128*f3abede76ac15228f1b161dd9660bb9094e81b1b*d201ccd492c284484c7824c4d37b1593", ItemEnum.ETHEREUM_PRE_SALE_WALLET_PBKDF2_HMAC_SHA256)]
    [InlineData("$ethereum$s*262144*1*8*3436383737333838313035343736303637353530323430373235343034363130*8b58d9d15f579faba1cd13dd372faeb51718e7f70735de96f0bcb2ef4fb90278*8de566b919e6825a65746e266226316c1add8d8c3d15f54640902437bcffc8c3", ItemEnum.ETHEREUM_WALLET_SCRYPT)]
    [InlineData("$ethereum$p*262144*3238383137313130353438343737383736323437353437383831373034343735*06eae7ee0a4b9e8abc02c9990e3730827396e8531558ed15bb733faf12a44ce1*e6d5891d4f199d31ec434fe25d9ecc2530716bc3b36d5bdbc1fab7685dda3946", ItemEnum.ETHEREUM_WALLET_PBKDF2_HMAC_SHA256)]
    
    // DJANGO
    [InlineData("sha1$fe76b$02d5916550edf7fc8c886f044887f4b1abf9b013", ItemEnum.DJANGO_SHA1)]
    [InlineData("pbkdf2_sha256$20000$H0dPx8NeajVu$GiC4k5kqbbR9qWBlsRgDywNqC2vd9kqfk7zdorEnNas=", ItemEnum.DJANGO_PBKDF2_SHA256)]
    
    // APPLE
    [InlineData("$keychain$*74cd1efd49e54a8fdc8750288801e09fa26a33b1*66001ad4e0498dc7*5a084b7314971b728cb551ac40b2e50b7b5bd8b8496b902efe7af07538863a45394ead8399ec581681f7416003c49cc7", ItemEnum.APPLE_KEYCHAIN)]
    [InlineData("$ASN$*1*20000*80771171105233481004850004085037*d04b17af7f6b184346aad3efefe8bec0987ee73418291a41", ItemEnum.APPLE_SECURE_NOTES)]
    // [InlineData("$fvde$2$16$58778104701476542047675521040224$20000$39602e86b7cea4a34f4ff69ff6ed706d68954ee474de1d2a9f6a6f2d24d172001e484c1d4eaa237d", ItemEnum.APPLE_FILE_SYSTEM)]   // hashes.com mismatch Possible algorithms: FileVault 2...
    [InlineData("$itunes_backup$*9*b8e3f3a970239b22ac199b622293fe4237b9d16e74bad2c3c3568cd1bd3c471615a6c4f867265642*10000*4542263740587424862267232255853830404566**", ItemEnum.APPLE_ITUNES_BACKUP_BEFORE_10_0)]
    [InlineData("$itunes_backup$*10*8b715f516ff8e64442c478c2d9abb046fc6979ab079007d3dbcef3ddd84217f4c3db01362d88fa68*10000*2353363784073608264337337723324886300850*10000000*425b4bb4e200b5fd4c66979c9caca31716052063", ItemEnum.APPLE_ITUNES_BACKUP_AFTER_10_0)]
    
    // MS OFFICE
    [InlineData("$office$*2007*20*128*16*411a51284e0d0200b131a8949aaaa5cc*117d532441c63968bee7647d9b7df7d6*df1d601ccf905b375575108f42ef838fb88e1cde", ItemEnum.MS_OFFICE_2007)]
    [InlineData("$office$*2010*100000*128*16*77233201017277788267221014757262*b2d0ca4854ba19cf95a2647d5eee906c*e30cbbb189575cafb6f142a90c2622fa9e78d293c5b0c001517b3f5b82993557", ItemEnum.MS_OFFICE_2010)]
    [InlineData("$office$*2013*100000*256*16*7dd611d7eb4c899f74816d1dec817b3b*948dc0b2c2c6c32f14b5995a543ad037*0b7ee0e48e935f937192a59de48a7d561ef2691d5c8a3ba87ec2d04402a94895", ItemEnum.MS_OFFICE_2013)]
    [InlineData("$oldoffice$1*04477077758555626246182730342136*b1b72ff351e41a7c68f6b45c4e938bd6*0d95331895e99f73ef8b6fbc4a78ac1a", ItemEnum.MS_OFFICE_2003_MD5_RC4)]
    // [InlineData("$oldoffice$0*55045061647456688860411218030058*e7e24d163fbd743992d4b8892bf3f2f7*493410dbc832557d3fe1870ace8397e2", ItemEnum.MS_OFFICE_2003_MD5_RC4_COLLIDER_1)]  // hashes.com mismatch Possible algorithms: MS Office <= 2003 $0/$1, MD5 + RC4...
    // [InlineData("$oldoffice$0*55045061647456688860411218030058*e7e24d163fbd743992d4b8892bf3f2f7*493410dbc832557d3fe1870ace8397e2:91b2e062b9", ItemEnum.MS_OFFICE_2003_MD5_RC4_COLLIDER_2)]   // hashes.com API not detect and WEB mismatch Possible algorithms: MS Office <= 2003 $0/$1, MD5 + RC4...
    // [InlineData("$oldoffice$3*83328705222323020515404251156288*2855956a165ff6511bc7f4cd77b9e101*941861655e73a09c40f7b1e9dfd0c256ed285acd", ItemEnum.MS_OFFICE_2003_SHA1_RC4)]    // hashes.com mismatch Possible algorithms: MS Office <= 2003 $0/$1, MD5 + RC4...
    // [InlineData("$oldoffice$3*83328705222323020515404251156288*2855956a165ff6511bc7f4cd77b9e101*941861655e73a09c40f7b1e9dfd0c256ed285acd", ItemEnum.MS_OFFICE_2003_SHA1_RC4_COLLIDER_1)] // hashes.com mismatch Possible algorithms: MS Office <= 2003 $0/$1, MD5 + RC4...
    // [InlineData("$oldoffice$3*83328705222323020515404251156288*2855956a165ff6511bc7f4cd77b9e101*941861655e73a09c40f7b1e9dfd0c256ed285acd:b8f63619ca", ItemEnum.MS_OFFICE_2003_SHA1_RC4_COLLIDER_2)]  // hashes.com API not detect and WEB mismatch Possible algorithms: MS Office <= 2003 $0/$1, MD5 + RC4...
    
    // AXCRYPT
    [InlineData("$axcrypt$*1*10000*aaf4a5b4a7185551fea2585ed69fe246*45c616e901e48c6cac7ff14e8cd99113393be259c595325e", ItemEnum.AXCRYPT)]
    [InlineData("$axcrypt_sha1$b89eaac7e61417341b710b727768294d0e6a277b", ItemEnum.AXCRYPT_INMEMORY_SHA1)]
    
    // BITCOIN
    [InlineData("$bitcoin$96$d011a1b6a8d675b7a36d0cd2efaca32a9f8dc1d57d6d01a58399ea04e703e8bbb44899039326f7a00f171a7bbc854a54$16$1563277210780230$158555$96$628835426818227243334570448571536352510740823233055715845322741625407685873076027233865346542174$66$625882875480513751851333441623702852811440775888122046360561760525", ItemEnum.BITCOIN_LITTLECOIN_WALLET_DAT)]
    
    // OTHERS
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyIzNDM2MzQyMCI6NTc2ODc1NDd9.f1nXZ3V_Hrr6ee-AFCTLaHRnrkiKmio2t3JqwL32guY", ItemEnum.JWT)]
    [InlineData("$rar5$16$74575567518807622265582327032280$15$f8b4064de34ac02ecabfe9abdf93ed6a$8$9843834ed0f7c754", ItemEnum.RAR5)]
    [InlineData("$7z$0$19$0$salt$8$f6196259a7326e3f0000000000000000$185065650$112$98$f3bc2a88062c419a25acd40c0c2d75421cf23263f69c51b13f9b1aada41a8a09f9adeae45d67c60b56aad338f20c0dcc5eb811c7a61128ee0746f922cdb9c59096869f341c7a9cb1ac7bb7d771f546b82cf4e6f11a5ecd4b61751e4d8de66dd6e2dfb5b7d1022d2211e2d66ea1703f96", ItemEnum.SEVENZIP)]
    [InlineData("$zip2$*0*3*0*e3222d3b65b5a2785b192d31e39ff9de*1320*e*19648c3e063c82a9ad3ef08ed833*3135c79ecb86cd6f48fc*$/zip2$", ItemEnum.WINZIP)]
    [InlineData("SCRYPT:1024:1:1:MDIwMzMwNTQwNDQyNQ==:5FW+zWivLxgCWj7qLiQbeC8zaNQ+qdO0NUinvqyFcfo=", ItemEnum.SCRYPT)]
    [InlineData("$S$C33783772bRXEx1aCsvY.dqgaaSu76XmVlKrW9Qu8IQlvxHlmzLf", ItemEnum.DRUPAL7)]
    // [InlineData("6e36dcfc6151272c797165fce21e68e7c7737e40:472433673", ItemEnum.OPENCART)]    // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    [InlineData("{ssha1}06$bJbkFGJAB30L2e23$dCESGOsP7jaIIAJ1QAcmaGeG.kr", ItemEnum.AIX_SSHA1)]
    [InlineData("admin::N46iSNekpT:08ca45b7d7ea58ee:88dcbe4446168966a153a0064958dac6:5c7830315c7830310000000000000b45c67103d07d7b95acd12ffa11230e0000000052920b85f78d013c31cdb3b92f5d765c783030", ItemEnum.NET_NTLM_V2)]
    // [InlineData("", ItemEnum.YESCRYPT)]  // Hashcat cant recognize this type of hash
    // [InlineData("7ca8eaaaa15eaa4c038b4c47b9313e92da827c06940e69947f85bc0fbef3eb8fd254da220ad9e208b6b28f6bb9be31dd760f1fdb26112d83f87d96b416a4d258", ItemEnum.WHIRLPOOL)] // hashes.com mismatch Possible algorithms: SHA512...
    [InlineData("$fvde$1$16$84286044060108438487434858307513$20000$f1620ab93192112f0a23eea89b5d4df065661f974b704191", ItemEnum.FILEVAULT_2)]
    [InlineData("$SHA$7218532375810603$bfede293ecf6539211a7305ea218b9f3f608953130405cda9eaba6fb6250f824", ItemEnum.AUTHME_SHA256)]
    // [InlineData("", ItemEnum.PASSWORDSAFE_V3)]   // Too complicated hash example
    [InlineData("38421854118412625768408160477112384218541184126257684081604771129b6258eb22fc8b9d08e04e6450f72b98725d7d4fcad6fb6aec4ac2a79d0c6ff738421854118412625768408160477112", ItemEnum.ANDROID_FDE_SAMSUNG_DEK)]
    [InlineData("2582a8281bf9d4308d6f5731d0e61c61*4604ba734d4e*89acf0e761f4*ed487162465a774bfba60eb603a39f3a", ItemEnum.WPA_PMKID_PBKDF2)]
    [InlineData("b7c2d6f13a43dce2e44ad120a9cd8a13d0ca23f0414275c0bbe1070d2d1299b1c04da0f1a0f1e4e2537300263a2200000000000000000000140768617368636174:472bdabe2d5d4bffd6add7b3ba79a291d104a9ef", ItemEnum.IPMI2_RAKP_HMAC_SHA1)]
    // [InlineData("374996a5e8a5e57fd97d893f7df79824:36", ItemEnum.OSCOMMERCE_XTCOMMERCE)]  // hashes.com mismatch Possible algorithms: md5($plaintext.$salt)...
    [InlineData("$blockchain$288$5420055827231730710301348670802335e45a6f5f631113cb1148a6e96ce645ac69881625a115fd35256636d0908217182f89bdd53256a764e3552d3bfe68624f4f89bb6de60687ff1ebb3cbf4e253ee3bea0fe9d12d6e8325ddc48cc924666dc017024101b7dfb96f1f45cfcf642c45c83228fe656b2f88897ced2984860bf322c6a89616f6ea5800aadc4b293ddd46940b3171a40e0cca86f66f0d4a487aa3a1beb82569740d3bc90bc1cb6b4a11bc6f0e058432cc193cb6f41e60959d03a84e90f38e54ba106fb7e2bfe58ce39e0397231f7c53a4ed4fd8d2e886de75d2475cc8fdc30bf07843ed6e3513e218e0bb75c04649f053a115267098251fd0079272ec023162505725cc681d8be12507c2d3e1c9520674c68428df1739944b8ac", ItemEnum.BLOCKCHAIN_MY_WALLET)]
    [InlineData("$blockchain$v2$5000$288$06063152445005516247820607861028813ccf6dcc5793dc0c7a82dcd604c5c3e8d91bea9531e628c2027c56328380c87356f86ae88968f179c366da9f0f11b09492cea4f4d591493a06b2ba9647faee437c2f2c0caaec9ec795026af51bfa68fc713eaac522431da8045cc6199695556fc2918ceaaabbe096f48876f81ddbbc20bec9209c6c7bc06f24097a0e9a656047ea0f90a2a2f28adfb349a9cd13852a452741e2a607dae0733851a19a670513bcf8f2070f30b115f8bcb56be2625e15139f2a357cf49d72b1c81c18b24c7485ad8af1e1a8db0dc04d906935d7475e1d3757aba32428fdc135fee63f40b16a5ea701766026066fb9fb17166a53aa2b1b5c10b65bfe685dce6962442ece2b526890bcecdeadffbac95c3e3ad32ba57c9e", ItemEnum.BLOCKCHAIN_MY_WALLET_V2)]
    [InlineData("$9$2MJBozw/9R3UsU$2lFhcKvpghcyw8deP25GOfyZaagyUOGBymkryvOdfo6", ItemEnum.CISCO_IOS_SCRYPT)]
    [InlineData("$8$TnGX/fE4KGHOVU$pEhnEvxrvaynpi8j4f.EMHr6M.FzU8xnZnBr/tJdFWk", ItemEnum.CISCO_IOS_PBKDF2_SHA256)]
    // [InlineData("ecf076ce9d6ed3624a9332112b1cd67b236fdd11:17782686", ItemEnum.SMF_AFTER_1_1)]    // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    [InlineData("$krb5pa$23$user$realm$salt$4e751db65422b2117f7eac7b721932dc8aa0d9966785ecd958f971f622bf5c42dc0c70b532363138363631363132333238383835", ItemEnum.KERBEROS_5_AS_REQ_PRE_AUTH_ETYPE_23)]
    // [InlineData("d7d5ea3e09391da412b653ae6c8d7431ec273ea2:238769868762:8962783556527653675", ItemEnum.RUBY_ON_RAILS_RESTFUL_AUTHENTICATION)] // hashes.com mismatch Possible algorithms: sha1($plaintext.$salt)...
    
    // [InlineData("", ItemEnum.)]
    public async Task TryParse_ShouldDetectHashAndReturnEnum(string hash, ItemEnum expected)
    {
        var (ok, result) = await HashParser.TryParse(hash);

        Assert.True(ok, $"Hash should be detected: {hash}");
        Assert.Equal(expected, result);
    }

    // INVALID HASHES
    [Theory]
    [InlineData("")]
    [InlineData("5)")]  //hashes.com marked as Base64
    [InlineData("not_a_hash")]
    [InlineData("12345")]
    [InlineData("hello world")]
    [InlineData("johndoe@example.com")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("!!!@@@###")]
    public async Task TryParse_ShouldRejectInvalidHashes(string input)
    {
        var (ok, result) = await HashParser.TryParse(input);
    
        Assert.False(ok, $"Should reject non-hash input: {input}");
        Assert.Equal(ItemEnum.Null, result);
    }
}