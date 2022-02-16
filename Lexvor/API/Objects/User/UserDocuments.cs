using Lexvor.API.Objects.Enums;
using Lexvor.Models.HomeViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.User {
	public class UserDocuments {
		public int Id { get; set; }
		[DisplayName("Document Name")]
		public string Name { get; set; }

		[ForeignKey("ProfileId")]
		public Profile Profile { get; set; }

		public Guid ProfileId { get; set; }
		[ForeignKey("UserPlanId")]
		public UserPlan UserPlan { get; set; }

		public Guid UserPlanId { get; set; }

		public DocumentType DocumentType { get; set; }
		public string URL { get; set; }
		public DateTime GeneratedOn { get; set; }
		public DateTime? ViewedOn { get; set; }
	}

	public class UserDocumentViewModel {
		public Profile Profile { get; set; }
        public SettingsViewModel SettingViewModel { get; set; }
		public List<UserDocumentView> UserDocumentsView { get; set; }
	}

	public class UserDocumentView {
		public int Id { get; set; }
		public string DocumentName { get; set; }
		public DocumentType DocumentType { get; set; }
		public string URL { get; set; }
		public DateTime GeneratedOn{get;set;}
		public DateTime? ViewedOn{get;set;}
	}
	
}
