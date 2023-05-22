namespace CustomerApi.Services;

public class EmailAddressInUseException : Exception
{
    public string EmailAddress { get; }

    public EmailAddressInUseException(string emailAddress)
        : base(CreateMessage(emailAddress))
    {
        EmailAddress = emailAddress;
    }

    public EmailAddressInUseException(string emailAddress, Exception inner)
        : base(CreateMessage(emailAddress), inner)
    {
        EmailAddress = emailAddress;
    }

    private static string CreateMessage(string emailAddress)
    {
        return $"{emailAddress} is already in use";
    }
}
