namespace Danslejeu;

public interface IAttaquable
{
    public void recevoirDommage(int degats);
}
public abstract class Animal : IAttaquable
{
    public string Nom;
    public int Vie
    {
        get;
        private set;
    }

    public Animal(string nom, int vie)
    {
        Nom = nom;
        Vie = vie;
    }

    public abstract void CriDeLanimal();
    public void Mourir()
    {
        Console.WriteLine($"{Nom} est mort !");
    }

    public void recevoirDommage(int degats)
    {
        Console.WriteLine($"{degats} est mort !");
    }
}

public class Chat : Animal
{
    public Chat(string nom, int vie) : base(nom, vie){}
    public override void CriDeLanimal()
    {
        Console.WriteLine($"{Nom} fait Miaou !");
    }
}
public class Chien : Animal
{
    public Chien(string nom, int vie) : base(nom, vie){}
    public override void CriDeLanimal()
    {
        Console.WriteLine($"{Nom} fait Wouaf !");
    }
}

public class Jeu
{
    public static void Main()
    {
        Animal chien = new Chien("Francois", 10);
        Animal chat = new Chat("popo", 10);
        List<Animal> AllAnimals = new List<Animal>();
        AllAnimals.Add(chien);
        AllAnimals.Add(chat);
    
        foreach (var target in AllAnimals)
        {
            target.CriDeLanimal();
            target.Mourir();
        }

        
    }
}