using System;
using CmisSync.Lib;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace TestLibrary
{
    /// <summary></summary>
    [TestFixture]
    public class EncodingTest
    {
        [Test, Category("Fast")]
        public void ValidFileNameTest()
        {
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("abcdefghijklmnopqrstuvwxyz"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("ABCDEFGHIJKLMNOPQRSTUVWXYZ"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("1234567890"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("ÄÖÜäöüß"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("-_.,;#+"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("¦§$%&()[]{}"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("'´`"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("@~¹²³±×"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("¡¢£¤¥¨©ª«¬®¯°µ¶·¸º»¼¼¾¿"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝ"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFileName("Þàáâãäåæçèéê"));
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName("?"), "?");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName(":"), ":");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName("/"), "/");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName("\\"), "\\");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName("\""), "\"");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName("<"), "<");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName(">"), ">");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName("*"), "*");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName("|"), "|");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFileName("–€"), "Non Valid ISO 8859-1 Character accepted");
        }

        [Test, Category("Fast")]
        public void ValidFolderNameTest()
        {
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("abcdefghijklmnopqrstuvwxyz"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("ABCDEFGHIJKLMNOPQRSTUVWXYZ"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("1234567890"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("ÄÖÜäöüß"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("-_.,;#+"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("¦§$%&()[]{}"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("'´`"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("@~¹²³±×"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("¡¢£¤¥¨©ª«¬®¯°µ¶·¸º»¼¼¾¿"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝ"));
            Assert.IsFalse(CmisSync.Lib.Utils.IsInvalidFolderName("Þàáâãäåæçèéê"));
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName("?"), "?");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName(":"), ":");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName("/"), "/");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName("\\"), "\\");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName("\""), "\"");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName("<"), "<");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName(">"), ">");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName("*"), "*");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName("|"), "|");
            Assert.IsTrue(CmisSync.Lib.Utils.IsInvalidFolderName("–€"), "Non Valid ISO 8859-1 Character accepted");
        }
    }
}

