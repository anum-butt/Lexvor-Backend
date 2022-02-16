using System.Collections.Generic;
using Lexvor.API;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lexvor.Models
{
    public class SelectListModel {
        public List<SelectListItem> YesNoList = SelectLists.YesNoList;
        public List<SelectListItem> YesNoNAList = SelectLists.YesNoNAList;
    }
}
