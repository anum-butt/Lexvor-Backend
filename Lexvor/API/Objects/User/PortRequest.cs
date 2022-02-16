using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TelispireSOAPService;

namespace Lexvor.API.Objects.User {
    public class PortRequest {
        public Guid Id { get; set; }

        [ForeignKey("PlanId")]
        public UserPlan Plan { get; set; }

        [DisplayName("Your Mobile Number")]
        public string MDN { get; set; }
        [DisplayName("Account Number (Prepaid: usually your mobile number, Contract: call your wirless provider)")]
        public string AccountNumber { get; set; }
        [DisplayName("Account Password (not every carrier has this, but the major ones do; T-Mobile, Verizon, etc.)")]
        public string Password { get; set; }

		
        [DisplayName("Name of your current service provider")]
        public string OSPName { get; set; }
        [DisplayName("First Name on Account")]
        public string FirstName { get; set; }
        [DisplayName("Middle Initial on Account")]
        public string MiddleInitial { get; set; }
        [DisplayName("Last Name on Account")]
        public string LastName { get; set; }
        [DisplayName("Billing Address on Account")]
        public string AddressLine1 { get; set; }
        [DisplayName("Address Line 2")]
        public string AddressLine2 { get; set; }
        [DisplayName("Billing City on Account")]
        public string City { get; set; }
        [DisplayName("Billing State on Account")]
        public string State { get; set; }
        [DisplayName("Billing Zip on Account")]
        public string Zip { get; set; }

        public PortStatus Status { get; set; }
        public string StatusDescription { get; set; }
        public bool CanBeSubmitted { get; set; }

        public DateTime LastUpdate { get; set; }
        public DateTime DateSubmitted { get; set; }
    }

    public enum PortStatus {
        /// <summary>
        /// Port is ready to be submitted.
        /// </summary>
        Ready = 1,
        /// <summary>
        /// Port is submitted by user and sent to Telispire. Pending completion. User has SIM in hand.
        /// </summary>
        Pending = 2,
        /// <summary>
        /// Port is complete
        /// </summary>
        Completed = 3,
        /// <summary>
        /// Port has an error that must be resolved.
        /// </summary>
        Error = 9,
        /// <summary>
        /// Port was cancelled by user or staff.
        /// </summary>
        Cancelled = 99
    }
}
