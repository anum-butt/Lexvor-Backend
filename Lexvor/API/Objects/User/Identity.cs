using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Lexvor.API.Objects.User {
    public class Identity {
        public Guid Id { get; set; }

        public IdentityDocument IdentityDocument { get; set; }

		[Required]
		public Profile Profile { get; set; }

        public string FirstName { get; set; }
		public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public double AuthenticityConfidence { get; set; }
        public string DocumentType { get; set; }
		[Required]
		public IndentitySource Source { get; set; }

		public Address Address { get; set; }
		public string Email { get; set; }
		[Required]
		public DateTime LastUpdated { get; set; }
    }

	public enum IndentitySource {
		Unknown,
		Vouched,
		Twilio,
		Plaid
	}
}
