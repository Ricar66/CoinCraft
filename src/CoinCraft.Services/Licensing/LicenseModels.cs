namespace CoinCraft.Services.Licensing
{
    public enum LicenseState
    {
        Unknown,
        Inactive,
        Active,
        Revoked,
        Expired
    }

    public sealed class License
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string PurchaserUserId { get; set; } = string.Empty;
        public int RemainingInstallations { get; set; }
        public LicenseState State { get; set; } = LicenseState.Unknown;
    }

    public sealed class InstallationRecord
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MachineFingerprint { get; set; } = string.Empty;
        public string InstalledAtIso8601 { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public sealed class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public License? License { get; set; }
    }
}