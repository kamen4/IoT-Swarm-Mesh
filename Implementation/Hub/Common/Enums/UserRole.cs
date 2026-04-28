namespace Common.Enums;

/// <summary>Defines the privilege tiers available to registered Telegram bot users.</summary>
public enum UserRole
{
    /// <summary>Standard user who can send device control requests.</summary>
    User = 0,

    /// <summary>Elevated administrator who can also create, update, and delete devices and users.</summary>
    DedicatedAdmin = 1,

    /// <summary>Full administrator who has all DedicatedAdmin rights and can also grant or revoke the DedicatedAdmin role.</summary>
    Admin = 2,
}
