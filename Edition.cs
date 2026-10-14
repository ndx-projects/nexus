namespace NdxNexus
{
    // Twee edities uit dezelfde code:
    //   Intern  = "NDX Nexus"  vaste NDX-huisstijl in alle setups
    //   Open    = "Nexus"      wereldwijde versie; de maker bepaalt zelf de vormgeving
    internal static class Edition
    {
#if OPEN
        public const bool IsOpen = true;
        public const string Name = "Nexus";
        public const string LogName = "Nexus";
        public const string SplashKey = "open";
#else
        public const bool IsOpen = false;
        public const string Name = "NDX Nexus";
        public const string LogName = "NDXNexus";
        public const string SplashKey = "ndx";
#endif
        public const string ProjectExt = "nexus";
    }
}
