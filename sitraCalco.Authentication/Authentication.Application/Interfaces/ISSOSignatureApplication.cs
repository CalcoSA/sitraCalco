namespace Authentication.Application.Interfaces
{
    public interface ISSOSignatureApplication
    {
        bool IsValid(string userLogin, long ts, string sig);
    }
}