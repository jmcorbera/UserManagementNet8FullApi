namespace UserManagement.Application.Common.Options;

/// <summary>
/// Feature flags for the application. Bound from configuration section "FeatureFlags".
/// </summary>
public sealed class FeatureFlagsOptions
{
    public const string SectionName = "FeatureFlags";

    /// <summary>
    /// When true, registration uses OTP verification flow. When false, OTP-related registration is disabled.
    /// </summary>
    public bool EnableOtp { get; set; } = true;
}
