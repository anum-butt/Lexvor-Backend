using Lexvor.API.Objects.Enums;
using Lexvor.API.Objects.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexvor.API.Objects
{
    public class LossClaim {
        public Guid Id { get; set; }
        public DateTime DateReported { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ProcessedOn { get; set; }
        public Profile Profile { get; set; }
		public Guid ProfileId { get; set; }
		public LossType LossType { get; set; }
        public UserDevice UserDevice { get; set; }
		public Guid UserDeviceId { get; set; }
		public Guid StockedDeviceId { get; set; }
		public IList<LossClaimUpload> Images { get; set; }
    }
}
