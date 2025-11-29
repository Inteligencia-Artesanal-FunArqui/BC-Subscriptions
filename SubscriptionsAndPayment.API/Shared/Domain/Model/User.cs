namespace OsitoPolar.Subscriptions.Service.Shared.Domain.Model;

public class User
{
    public int Id { get; }
    public string Username { get; }
    public string Email { get; }

    public User(int id, string username, string email)
    {
        Id = id;
        Username = username;
        Email = email;
    }
}
