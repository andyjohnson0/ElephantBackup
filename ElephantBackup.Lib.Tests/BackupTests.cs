using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ElephantBackup.Lib.Tests
{
    [TestClass]
    public class BackupTests
    {
        [TestMethod]
        public void SingleDir()
        {
            Assert.IsTrue(BackupTestHelper.DoBackup(numDirLevels: 1));
        }

        [TestMethod]
        public void SingleDir_ZeroLengthFiles()
        {
            Assert.IsTrue(BackupTestHelper.DoBackup(numDirLevels: 1, fileLen: 0));
        }

        [TestMethod]
        public void MultipleDir()
        {
            Assert.IsTrue(BackupTestHelper.DoBackup(numDirLevels: 3, numSubdirsPerDir: 5));
        }

        [TestMethod]
        public void DeepDirs()
        {
            // Back-up a deep folder hierarchy with a path length of approx 500 chars, which is
            // longer than Window's _MAX_Path value (260)
            Assert.IsTrue(BackupTestHelper.DoBackup(numDirLevels: 40, numSubdirsPerDir: 1));
        }    
    }
}
