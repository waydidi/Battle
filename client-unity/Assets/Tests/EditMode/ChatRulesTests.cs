using KingdomsEightBanners.Core;
using NUnit.Framework;

namespace KingdomsEightBanners.Tests
{
    public class ChatRulesTests
    {
        [Test]
        public void WorldChat_RequiresLevel50()
        {
            Assert.AreEqual(ChatSendResult.LevelTooLow, ChatRules.CanSend(ChatChannel.World, 1));
            Assert.AreEqual(ChatSendResult.LevelTooLow, ChatRules.CanSend(ChatChannel.World, 49));
            Assert.AreEqual(ChatSendResult.Allowed, ChatRules.CanSend(ChatChannel.World, 50));
            Assert.AreEqual(ChatSendResult.Allowed, ChatRules.CanSend(ChatChannel.World, 68));
        }

        [Test]
        public void OtherPlayerChannels_HaveNoLevelGate()
        {
            Assert.AreEqual(ChatSendResult.Allowed, ChatRules.CanSend(ChatChannel.Party, 1));
            Assert.AreEqual(ChatSendResult.Allowed, ChatRules.CanSend(ChatChannel.Whisper, 1));
            Assert.AreEqual(ChatSendResult.Allowed, ChatRules.CanSend(ChatChannel.Guild, 1));
        }

        [Test]
        public void SystemChannel_IsReadOnly()
        {
            Assert.AreEqual(ChatSendResult.ReadOnlyChannel, ChatRules.CanSend(ChatChannel.System, 99));
        }
    }
}
