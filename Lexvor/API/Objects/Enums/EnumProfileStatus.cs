namespace Lexvor.API.Objects.Enums {
	public enum ProfileStatus {
		// No Plaid Link provided
		Provisional = 1,
		// Plaid linked bank account
		Confirmed = 2,
		// No plaid link, but manually verified
		Trusted = 3
	}
}
