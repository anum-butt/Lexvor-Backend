using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.Data {
    public static class Roles {

        public const string Admin = "admin";
        public const string TrustedManager = "trustedmanager";
        public const string Manager = "manager";
        public const string Level2Support = "level2support";
        public const string Level1Support = "level1support";

        /// <summary>
        /// A trial user is a user that has not had a successful payment yet.
        /// </summary>
        public const string Trial = "trial";
        /// <summary>
        /// A normal paying user.
        /// </summary>
        public const string User = "user";
    }
}
