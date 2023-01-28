using System;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class Authenticator
    {
        public enum licenseType
        {
            Personal,
            Faction
        }

        private readonly string fKey;
        private readonly string ownerFaction;
        private readonly long ownerId;
        private readonly string pKey;
        private readonly DateTime today;

        public Authenticator(string pKey, string fKey, long ownerId, string ownerFaction)
        {
            this.pKey = pKey;
            this.fKey = fKey;
            this.ownerId = ownerId;
            this.ownerFaction = ownerFaction;
            today = DateTime.Now;
        }

        public static string WordKey()
        {
            return $"{(char)107}{(char)101}{(char)121}";
        }

        public static string WordLicense()
        {
            return $"{(char)108}{(char)105}{(char)99}{(char)101}{(char)110}{(char)115}{(char)101}";
        }

        private static string DeObfuscatedSalt(int placement)
        {
            switch (placement)
            {
                case 1:
                    return AuthConst.salt1.Replace(AuthConst.saltObfuscation1, "");
                case 2:
                    return AuthConst.salt2.Replace(AuthConst.saltObfuscation2, "");
                case 3:
                    return AuthConst.salt3.Replace(AuthConst.saltObfuscation3, "");
            }

            return string.Empty;
        }

        public bool IsAuthorized(out string message)
        {
            if (today.DateMayBeFaked())
            {
                message = Prompts.Areyoutryingtohackme;
                return false;
            }

            return PlayerIsAuthorized(pKey, out message) || FactionIsAuthorized(fKey, out message);
        }

        public long ToLong(string input)
        {
            return long.Parse(input);
        }


        private bool PlayerIsAuthorized(string encodedKey, out string message)
        {
            var keyParts = Decrypt(encodedKey).Split('|');
            DateTime licenseExpiry;
            if (keyParts.Count() != 2 || !ParseLicenseDate(keyParts[1], out licenseExpiry) || licenseExpiry < today)
            {
                message = $"{Prompts.Invalid} {WordKey()}";
                return false;
            }

            message = DisplayLicensePrompt(licenseType.Personal, DaysLeft(licenseExpiry));

            return VerifyOwnerId(ownerId, ToLong(keyParts[0]));
        }

        private bool FactionIsAuthorized(string encodedKey, out string message)
        {
            var keyParts = Decrypt(encodedKey).Split('|');

            DateTime licenseExpiry;
            if (keyParts.Count() != 2)
            {
                message = $"{WordKey()} {Prompts.IsNotFormattedProperly}";
                return false;
            }

            if (!ParseLicenseDate(keyParts[1], out licenseExpiry))
            {
                message = $"{Prompts.CouldNotParseDateFrom}: {keyParts[1]}";
                return false;
            }

            if (licenseExpiry < today)
            {
                message = $"{licenseExpiry} < {today}";
                return false;
            }

            if (keyParts.Count() != 2 || !ParseLicenseDate(keyParts[1], out licenseExpiry) || licenseExpiry < today)
            {
                message = $"{Prompts.Invalid} {WordKey()}";
                return false;
            }

            message = DisplayLicensePrompt(licenseType.Faction, DaysLeft(licenseExpiry));

            return VerifyFactionTag(ownerFaction, keyParts[0]);
        }

        private static bool ParseLicenseDate(string input, out DateTime licenseExpiry)
        {
            if (DateTime.TryParse(input, out licenseExpiry))
                return true;
            return false;
        }

        private static double DaysLeft(DateTime expiry)
        {
            return (expiry - DateTime.UtcNow).TotalDays;
        }

        private static string DisplayLicensePrompt(licenseType type, double daysLeft)
        {
            return
                $"{Prompts.DaysLeft} on {(type == licenseType.Personal ? ConfigName.PersonalKey.ToHumanReadableName() : ConfigName.FactionKey.ToHumanReadableName())}: {daysLeft}";
        }

        private static string Decrypt(string value)
        {
            var bytes = Convert.FromBase64String(value);
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] ^= GetNextRandom();

            return Encoding.Default.GetString(bytes);
        }

        private static string Encrypt(string value)
        {
            var bytes = Encoding.Default.GetBytes(value);
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] ^= GetNextRandom();

            return Convert.ToBase64String(bytes);
        }

        private static byte GetNextRandom()
        {
            var _seed = BitConverter.ToUInt32(Encoding.Default.GetBytes(Salt()), 0);
            _seed ^= _seed << AuthConst.shift1;
            _seed ^= _seed >> AuthConst.shift2;
            _seed ^= _seed << AuthConst.shift3;

            return (byte)(_seed & 0xFF);
        }

        private static string Salt()
        {
            return $"{DeObfuscatedSalt(1)}{DeObfuscatedSalt(2)}{DeObfuscatedSalt(3)}";
        }

        private static bool VerifyOwnerId(long ownerId, long keyId)
        {
            return ownerId == keyId;
        }

        private static bool VerifyFactionTag(string ownerTag, string keyTag)
        {
            return ownerTag == keyTag;
        }

        public static long OwnerId(IMyProgrammableBlock Me)
        {
            return Me.OwnerId;
        }

        public static string FactionTag(IMyProgrammableBlock Me)
        {
            return Me.GetOwnerFactionTag();
        }
    }
}