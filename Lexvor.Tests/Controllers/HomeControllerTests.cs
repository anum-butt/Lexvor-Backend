using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexvor.Tests.Controllers
{
    [TestClass]
    public class HomeControllerTests : BaseTest
    {
        [TestMethod]
        public void Settings_Save_Saves_Profile_ProfileSettings_Test() {
            // Saving the settings should save changes to both the Profile and ProfileSettings
        }

        [TestMethod]
        public void Settings_Save_Changes_Income_Salary_Budget_Test() {
            // When saving the user settings and the salary has been changed,
            // the system will reset the Income-Salary budget. 
            // Make sure that it does infact change and that no other budgets are affected.

        }
    }
}
