namespace Lexvor.API.Objects.Enums {
    public enum IDVerifyStatus {
		// No ID verify started
        NotStarted = 0,
		// ID submitted, no vouhced response yet.
        Pending = 1,
		// Documents not good or discrepancy found.
        ReverificationRequired = 2,
		// Vouched returned and data matches, no selfie
		ProvisionalVerfied = 9,
		// Vouched returned and data matches, with selfie
        Verified = 10
    }
}
