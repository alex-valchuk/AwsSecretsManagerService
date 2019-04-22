namespace AwsSecretsManagerService
{
    public class AwsSecret
    {
        public string ARN { get; set; }
        public string CreatedDate { get; set; }
        public string Name { get; set; }
        public string SecretString { get; set; }
        public string VersionId { get; set; }
        public string[] VersionStages { get; set; }
    }
}
