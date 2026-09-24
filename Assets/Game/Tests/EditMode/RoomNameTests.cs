using Game.Scripts.Infrastructure.Implementations.UI.Popups.RoomPopup;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class RoomNameTests
    {
        [TestCase("AbCd1234", true)]
        [TestCase("12345678", true)]
        [TestCase("abcdefgh", true)]
        [TestCase("", false)]
        [TestCase("Abc1234", false)]
        [TestCase("Abcd12345", false)]
        [TestCase("Abcd123!", false)]
        [TestCase("АБВГ1234", false)]
        [TestCase("Abcd 123", false)]
        public void RequiresExactlyEightAsciiLettersOrDigits(string value, bool expected)
        {
            Assert.That(RoomName.IsValid(value), Is.EqualTo(expected));
        }

        [Test]
        public void PasteRemovesUnsupportedCharactersAndLimitsLength()
        {
            Assert.That(RoomName.Normalize(" AбB-cD_12!34567"), Is.EqualTo("ABcD1234"));
        }
    }
}
