using System;

namespace Ninjadini.Console
{
    /// <summary>
    /// This allows you to have your own custom console overlay access challenge<br/>
    /// The build in access challenge ask for a password that you set in project settings.<br/>
    /// 1. First create a class that implements both IConsoleAccessChallenge and IConsoleExtension.<br/>
    /// 2. Add [Serializable] attribute to the class.<br/>
    /// 3. Go to project settings > NjConsole > Playmode Overlay > Add Access Challenge > add your new class<br/>
    /// Example code below ask for a simple math question to answer before letting you go to console.
    /// <code>
    /// [System.Serializable]
    /// public class MathsAccessChallenge : IConsoleAccessChallenge, IConsoleExtension
    /// {
    ///  static bool _passed; // should be store in player pref or something
    /// 
    ///  public bool ShowingChallenge { get; private set; }
    /// 
    /// void IConsoleModule.OnAdded(ConsoleModules console)
    /// {
    ///    _passed = false;
    ///    ShowingChallenge = false;
    /// }
    /// 
    /// public bool IsAccessChallengeRequired()
    /// {
    ///     return !_passed;
    /// }
    /// 
    /// public void ShowChallenge(Action callbackOnSuccess)
    /// {
    ///    var numA = UnityEngine.Random.Range(1, 100);
    ///    var numB = UnityEngine.Random.Range(1, 100);
    ///    ShowingChallenge = true;
    ///    // This could be anything, like your own sign in dialog. We are just using the text prompt from Console for simplicity.
    ///    ConsoleTextPrompt.Show(new ConsoleTextPrompt.Data()
    ///    {
    ///        Title = $"{numA} + {numB} = ?",
    ///        ResultCallback = (response) =>
    ///        {
    ///            if (response == null) // user pressed close btn
    ///            {
    ///                ShowingChallenge = false;
    ///                return true;
    ///            }
    ///            if (int.TryParse(response, out var responseInt) && responseInt == numA + numB)
    ///            {
    ///                ShowingChallenge = false;
    ///                _passed = true;
    ///                callbackOnSuccess();
    ///                return true;
    ///            }
    ///            return false;
    ///        }
    ///    });
    /// }
    ///}
    /// </code>
    /// </summary>
    public interface IConsoleAccessChallenge : IConsoleModule
    {
        bool IsAccessChallengeRequired();
        void ShowChallenge(Action callbackOnSuccess);
        
        public bool ShowingChallenge { get; }
    }
}