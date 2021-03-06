using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexvor.Helpers {
	public class AllowedExtensionsAttribute : ValidationAttribute {
		private readonly string[] _Extensions;
		public AllowedExtensionsAttribute(string[] Extensions) {
			_Extensions = Extensions;
		}

		protected override ValidationResult IsValid(
		object value, ValidationContext validationContext) {
			if (value != null) {
				var file = value as IFormFile;
				var extension = Path.GetExtension(file.FileName);
				if (!(file == null)) {
					if (!_Extensions.Contains(extension.ToLower())) {
						return new ValidationResult(GetErrorMessage());
					}
				}
			}

			return ValidationResult.Success;
		}

		public string GetErrorMessage() {
			return $"This photo extension is not allowed!";
		}
	}
}
