namespace SpotifyPlaylistQueryMod.Managers.Attributes;

public class DbRetryPolicyAttribute : FromKeyedServicesAttribute
{
    public const string ServiceKey = "dbRetry";
    public DbRetryPolicyAttribute() : base(ServiceKey) { }
}
