using Lexvor.API.Objects;

namespace Lexvor.Models.HomeViewModels {
    public class LoggedInPageViewModel {
        public Profile Profile { get; set; }
        public ApplicationUser User { get; set; }
    }
}
