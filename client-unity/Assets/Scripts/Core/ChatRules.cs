namespace KingdomsEightBanners.Core
{
    public enum ChatChannel
    {
        World,
        Party,
        Whisper,
        Guild,
        System,
    }

    public enum ChatSendResult
    {
        Allowed,
        /// <summary>The channel has a minimum character level the sender has not reached.</summary>
        LevelTooLow,
        /// <summary>Players can never post in this channel (System).</summary>
        ReadOnlyChannel,
    }

    /// <summary>
    /// Who may post in which chat channel. The chat service must run this check on the server;
    /// the client only uses it to grey out the input and explain why.
    /// </summary>
    public static class ChatRules
    {
        /// <summary>World chat is open from this character level (limits spam from fresh accounts).</summary>
        public const int WorldChatMinLevel = 50;

        public static int MinLevel(ChatChannel channel)
        {
            return channel == ChatChannel.World ? WorldChatMinLevel : 1;
        }

        public static ChatSendResult CanSend(ChatChannel channel, int characterLevel)
        {
            if (channel == ChatChannel.System) return ChatSendResult.ReadOnlyChannel;
            if (characterLevel < MinLevel(channel)) return ChatSendResult.LevelTooLow;
            return ChatSendResult.Allowed;
        }
    }
}
