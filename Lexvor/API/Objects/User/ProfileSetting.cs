using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.User {
    public class ProfileSetting {
        public Guid Id { get; set; }

        [ForeignKey("ProfileId")]
        public Profile Profile { get; set; }
        public Guid ProfileId { get; set; }

        public string SettingName { get; set; }
        public string SettingValue { get; set; }

        public DateTime DateAdded { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
