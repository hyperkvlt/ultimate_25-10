using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Ninjadini.Console.Internal
{
    [DisplayName("Secret Pass")]
    [Serializable]
    public class SecretPassAccessChallenge : IConsoleAccessChallenge
    {
        public string passwordHash = "R7PnS8MDfneEtxtS3oQLNwKWhGYojI+GTd+oPQelBLY=";
        public TouchScreenKeyboardType keyboardType;
        public string hintMessage = "Speak, gamer, and enter...";
        [Tooltip("If this is turned on, it'll still ask for pass key in Unity editor")]
        public bool requiredInEditor = true;
#if !NJCONSOLE_DISABLE  

        void IConsoleModule.OnAdded(ConsoleModules console)
        {
            ShowingChallenge = false;
        }

        public bool IsAccessChallengeRequired()
        {
            if (string.IsNullOrEmpty(passwordHash))
            {
                return false;
            }
            if (!requiredInEditor && Application.isEditor)
            {
                return false;
            }
            if (!PlayerPrefs.HasKey(PassHashPrefKey))
            {
                return true;
            }
            var expectedHash = HashPassword(passwordHash, PlayerSalt);
            var savedHash = PlayerPrefs.GetString(PassHashPrefKey);
            return savedHash != expectedHash;
        }
        
        public void ShowChallenge(Action callbackOnSuccess)
        {
            if (ShowingChallenge)
            {
                return;
            }
            if (string.IsNullOrEmpty(passwordHash))
            {
                callbackOnSuccess?.Invoke();
                return;
            }

            ShowingChallenge = true;
            ConsoleTextPrompt.Show(new ConsoleTextPrompt.Data()
            {
                KeyboardType = keyboardType,
                IsPassword = true,
                ResultCallback = str =>
                {
                    if (str == null) // user pressed close btn
                    {
                        ShowingChallenge = false;
                        return true;
                    }
                    var success = ProcessEnteredPassword(str);
                    ShowingChallenge = !success;
                    if (success)
                    {
                        callbackOnSuccess?.Invoke();
                    }
                    return success;
                },
                Title = hintMessage
            });
        }

        public bool ShowingChallenge { get; private set; }

        bool ProcessEnteredPassword(string password)
        {
            var hash = HashPassword(password);
            if (hash.Equals(passwordHash))
            {
                var playerHash = HashPassword(hash, PlayerSalt);
                PlayerPrefs.SetString(PassHashPrefKey, playerHash);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        const string PassHashPrefKey = "Ninjadini.Console_passHash";
        static readonly byte[] AppSalt = Encoding.UTF8.GetBytes("Ninjadini Console is the best!");
        static readonly byte[] PlayerSalt = Encoding.UTF8.GetBytes("The best devs use Ninjadini Console");

        public static string HashPassword(string password)
        {
            return HashPassword(password, AppSalt);
        }
        
        static string HashPassword(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 123, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hash);
        }
#else
        public bool IsAccessChallengeRequired() => false;
        public void ShowChallenge(Action callbackOnSuccess)
        {
        }
        public bool ShowingChallenge => false;
#endif
    }
}