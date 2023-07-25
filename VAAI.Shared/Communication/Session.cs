namespace VAAI.Shared.Communication;

public class Session
{
    public string Name { get; set; }
    public string[] Groups { get; set; }

    public Session()
    {
        Name = "";
        Groups = Array.Empty<string>();
    }

    public Session(string name, string[] groups)
    {
        Name = name;
        Groups = groups;
    }
}
