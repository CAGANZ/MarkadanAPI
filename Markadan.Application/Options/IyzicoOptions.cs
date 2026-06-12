namespace Markadan.Application.Options;

public class IyzicoOptions
{
    public string ApiKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";
    public string CallbackUrl { get; set; } = "";
}
