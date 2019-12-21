using UnityEngine;

namespace GFW.Unity
{
    public class NetCheck
    {
        public static bool IsWifi()
        {
            return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
        }

        public static bool IsAvailable()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }
}
