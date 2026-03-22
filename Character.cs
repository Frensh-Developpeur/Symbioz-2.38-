
  namespace Apprentissage;

  public abstract class Character
  {
      public int Life;
      public string Name;

      public Character(int life, string name)
      {
          Life = life;
          Name = name;
      }

      public abstract void Attaque();
  }

  public class Monster : Character
  {
      public Monster(int life, string name) : base(life, name)
      {
      }

      public override void Attaque()
      {
          Console.WriteLine($"{Name} le monstre attaque !");
      }
  }
  
   public class Noodle : Character
  {
      public Noodle(int point, string name) : base(life, name)
      {
      }

      public override void Attaque()
      {
          Console.WriteLine($"{Name} le noodle attaque !");
      }
  }

  public class Program
  {
      public static void Main()
      {
          
          Character mob = new Monster(150, "Bouftou");
          Character nood = new Noodle(150, "Bouftou");

          List<Character> tousLesCombattants = new List<Character>();
          tousLesCombattants.Add(mob);
          tousLesCombattants.Add(nood);

          foreach (var target in tousLesCombattants)
          {
            target.Attaque();
          }
      }
  }