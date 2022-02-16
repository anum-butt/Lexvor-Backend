using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexvor.API.Objects
{
    public class LossClaimUpload
    {
        public Guid Id { get; set; }
        public Guid LossClaimId { get; set; }
        public LossClaim LossClaim { get; set; }
        public string ImageUrl { get; set; }
        [NotMapped]
        [DataType(DataType.Upload)]
        public IFormFile ImageUpload { get; set; }
    }
}
