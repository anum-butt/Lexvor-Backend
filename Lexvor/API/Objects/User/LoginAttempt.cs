using System;
using System.ComponentModel.DataAnnotations;

namespace Lexvor.API.Objects
{
    public class LoginAttempt {
        [Key]
        public Guid Id { get; set; }

        public string AccountId { get; set; }

        public DateTime AttemptDate { get; set; }

        public string IP { get; set; }

        public string Email { get; set; }

        public bool Success { get; set; }
    }
}
