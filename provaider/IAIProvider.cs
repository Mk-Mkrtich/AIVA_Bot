namespace AVIA_Bot.Provaider;

public interface IAIProvider{
    string Name {get;}

    Task<string> ResponseAsync(List<(string role, string text)> messages);
}