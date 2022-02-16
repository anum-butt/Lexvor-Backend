using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexvor.Tests.Controllers
{
    [TestClass]
    public class AccountControllerTests : BaseTest
    {
        [TestMethod]
        public void Upgrading_Plan_Sets_Role_Test() {
            // Upgrading a plan will set the cooresponding role correctly.
        }

        [TestMethod]
        public void Downgrading_Plan_Removes_Correct_Role_Test() {
            // Downgrading a plan will remove the correct role.

        }
    }
}
