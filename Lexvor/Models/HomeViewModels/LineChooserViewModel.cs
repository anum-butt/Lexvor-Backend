using Lexvor.API.Objects;
using Lexvor.API.Objects.Enums;
using System.Collections.Generic;

namespace Lexvor.Models.HomeViewModels {
	public class LineChooserViewModel {
		public string BaseUrl { get; set; }
		public string QueryParameterName { get; set; }
		public EnumUrlSectionToReplaceType QueryParameterType { get; set; }
		public List<UserPlan> Plans { get; set; }

		public string GetRedirectUrl(UserPlan plan) {
			string valueToInsert = string.Empty;
			switch(QueryParameterType) {
				case EnumUrlSectionToReplaceType.PlanId: valueToInsert = plan.Id.ToString(); break;
				case EnumUrlSectionToReplaceType.MDN: valueToInsert = plan.MDN; break;
				case EnumUrlSectionToReplaceType.DeviceId: valueToInsert = plan.DeviceId.ToString(); break;
				default: valueToInsert = plan.Id.ToString(); break;
			}
			return $"{BaseUrl}?{QueryParameterName}={valueToInsert}&isFromLineChooserPageRequest=true";
		}
	}
}
