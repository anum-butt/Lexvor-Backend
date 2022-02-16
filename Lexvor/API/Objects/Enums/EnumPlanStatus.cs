/// <summary>
/// Pending = plan has started creation, but has not gone through payment.
/// Paid = plan has been paid for, and is waiting ID and Bank Verification.
/// OnHold = Fraud Flag
/// PaymentHold = Plan is in danger of being locked due to non-payment
/// Active = Validated account that has been successfully charged.
/// Locked = Plan has been turned off due to non-payment; service inactive, phone locked.
/// Cancelled = Plan was cancelled by user
/// </summary>
public enum PlanStatus {
    /// <summary>
    /// Plan has started creation, but has not gone through payment.
    /// </summary>
    Pending = 0,
    /// <summary>
    /// Validated account that has been successfully charged
    /// </summary>
    Active = 1,
    /// <summary>
    /// Fraud Flag
    /// </summary>
    OnHold = 2,
    /// <summary>
    /// Plan is in danger of being locked due to non-payment
    /// </summary>
    PaymentHold = 3,
    /// <summary>
    /// User selected a backordered Device, but has otherwise met activation criteria.
    /// </summary>
    DevicePending = 4,
    /// <summary>
    /// Plan has been paid for, and is waiting ID and Bank Verification
    /// </summary>
    Paid = 5,
    /// <summary>
    /// Plan has been turned off due to non-payment; service inactive, phone locked
    /// </summary>
    Locked = 98,
    /// <summary>
    /// Plan was cancelled by user
    /// </summary>
    Cancelled = 99,

	Suspend = 98
}
