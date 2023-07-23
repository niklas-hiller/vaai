namespace VAAI.Shared.Communication;

public class Session
{
    public string Name { get; set; }
    public string[] Groups { get; set; }

    public Session()
    {
        Name = "";
        Groups = new string[0];
    }

    public Session(string name, string[] groups)
    {
        Name = name;
        Groups = groups;
    }
}
