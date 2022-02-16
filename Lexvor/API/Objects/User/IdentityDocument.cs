using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Lexvor.API.Objects.User {
    public class IdentityDocument {
        public Guid Id { get; set; }
        public Profile Profile { get; set; }
        public string DocumentUrl { get; set; }
        [NotMapped]
        [DataType(DataType.Upload)]
        public IFormFile ImageUpload { get; set; }
    }
}
