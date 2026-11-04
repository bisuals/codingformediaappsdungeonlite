namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// The one place the editor windows read the product version and the links from. Bumping a
    /// release is a single edit here instead of a hunt through the UXML files.
    /// </summary>
    public static class WiseFelineEditorInfo
    {
        /// <summary>
        /// The version of the package, shown in the About window. Update this when a release is
        /// prepared.
        /// </summary>
        public const string Version = "1.51.0";

        /// <summary>The product page on the NoOpArmy site.</summary>
        public const string ProductUrl = "https://www.nooparmygames.com/";

        /// <summary>The online manual and API reference.</summary>
        public const string DocumentationUrl = "https://www.nooparmygames.com/WF-UtilityAI-Unity/";

        /// <summary>The Discord server where support questions are answered.</summary>
        public const string DiscordUrl = "https://discord.gg/FA8R7APZWR";
    }
}
