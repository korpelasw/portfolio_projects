using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;


namespace Tasohyppelypeli2;

/// @author Samuli Korpela
/// @version 15.04.2024
/// <summary>
///  Tasohyppely pelin luominen
/// </summary>
public class Tasohyppelypeli2 : PhysicsGame
{
    /// <summary>
    /// Pelaajan nopeus
    /// </summary>
    private const double Nopeus = 200;
    
    /// <summary>
    /// Pelaajan hyppyvoima
    /// </summary>
    private const double Hyppynopeus = 700;
    
    /// <summary>
    /// Pelialueen yhden ruudun koko
    /// </summary>
    private const int RuudunKoko = 32;

    private bool hiddenGemi = false;
    
    /// <summary>
    /// Pelaaja
    /// </summary>
    private PlatformCharacter pelaaja1;

    /// <summary>
    /// Kentän numeron alustus
    /// </summary>
    private int kenttaNro = 1;
    
    /// <summary>
    /// Pistelaskurin alustus
    /// </summary>
    private IntMeter pisteLaskuri;
    
    private readonly Image pelaajanKuva = LoadImage("Mölli.png");
    private readonly Image gem1Kuva = LoadImage("Gemi1.png");
    private readonly Image gem2Kuva = LoadImage("Gemi2.png");
    private readonly Image gem3Kuva = LoadImage("Gemi3.png");
    private readonly Image gem4Kuva = LoadImage("Gemi4.png");
    
    private readonly SoundEffect maaliAani = LoadSoundEffect("maali.wav");

    public override void Begin()
    {
        Gravity = new Vector(0, -1000);
        
        LisaaNappaimet();
        SeuraavaKentta();
        
        Camera.Follow(pelaaja1);
        Camera.ZoomFactor = 1.2;
        Camera.StayInLevel = true;
        PeliMusat(1);
        MasterVolume = 0.5;
        MediaPlayer.Volume = 0.3;
        
    }
    
    
    /// <summary>
    /// Kentän luonti
    /// </summary>
    private void LuoKentta(string kenttaTiedostonNimi)
    {
        TileMap kentta = TileMap.FromLevelAsset(kenttaTiedostonNimi);
        kentta.SetTileMethod('#', LisaaTaso, "groundtile");
        kentta.SetTileMethod('*', LisaaGem);
        kentta.SetTileMethod('N', LisaaPelaaja);
        kentta.SetTileMethod('L', LisaaTaso, "lavatile");
        kentta.SetTileMethod('3', LuoVihu, 3);
        kentta.SetTileMethod('2', LuoVihu, 2);
        kentta.Execute(RuudunKoko, RuudunKoko);
        Level.CreateBorders();
        Level.Background.CreateGradient(Color.White, Color.SkyBlue);
    }
    
    
    /// <summary>
    /// Luo vihu ja sen liikkuminen
    /// </summary>
    /// <param name="paikka">Paikka</param>
    /// <param name="leveys">Leveys</param>
    /// <param name="korkeus">Korkeus</param>
    /// <param name="liikePituus">Liikemäärä</param>
    public void LuoVihu(Vector paikka, double leveys, double korkeus, int liikePituus)
    {
        
        PlatformCharacter vihu = new PlatformCharacter(leveys, korkeus);
        vihu.Shape = Shape.Circle;
        vihu.Position = paikka;
        vihu.Tag = "vihu";
        vihu.Image = LoadImage("Enemy1");
        Add(vihu);
        
        PathFollowerBrain pfb = new PathFollowerBrain(); // Luodaan polkua seuraava tekoäly vihulle
        List<Vector> reitti = new List<Vector>(); 
        reitti.Add(vihu.Position);
        Vector seuraavaPiste = vihu.Position + new Vector(liikePituus * RuudunKoko, 0); // Annetaan polulle piste mihin siirtyä
        reitti.Add(seuraavaPiste);
        pfb.Path = reitti; // Lisätään reitti tekoälylle
        pfb.Loop = true; // Laitetaan se looppaamaan
        vihu.Brain = pfb;
        
    }
    
    
    /// <summary>
    /// Lisää taso
    /// </summary>
    /// <param name="paikka">Paikka</param>
    /// <param name="leveys">Leveys</param>
    /// <param name="korkeus">Korkeus</param>
    /// <param name="tasonNimi">Kuvatiedoston nimi</param>
    private void LisaaTaso(Vector paikka, double leveys, double korkeus, string tasonNimi)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.Green;
        taso.Image = LoadImage(tasonNimi);
        if (tasonNimi == "lavatile")
        {
            taso.Tag = "lava";
        }
        Add(taso);
    }
    
    
    /// <summary>
    /// Lisää gemi
    /// </summary>
    /// <param name="paikka">Paikka</param>
    /// <param name="leveys">Leveys</param>
    /// <param name="korkeus">Korkeus</param>
    private void LisaaGem(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tahti = PhysicsObject.CreateStaticObject(leveys, korkeus);
        tahti.IgnoresCollisionResponse = true;
        tahti.Position = paikka;
        tahti.Tag = "gemi";
        if (kenttaNro == 1)
        {
            tahti.Image = gem1Kuva;
        }
        else if (kenttaNro == 2)
        {
            tahti.Image = gem2Kuva;
        }
        else
        {
            tahti.Image = gem3Kuva;
        }
        Add(tahti);
    }
    
    
    /// <summary>
    /// Lisää pelaajahahmo
    /// </summary>
    /// <param name="paikka">Paikka</param>
    /// <param name="leveys">Leveys</param>
    /// <param name="korkeus">Korkeus</param>
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja1 = new PlatformCharacter(leveys, korkeus);
        pelaaja1.Position = paikka;
        pelaaja1.Mass = 4.0;
        pelaaja1.Image = pelaajanKuva;
        pelaaja1.Tag = "pelaaja";
        pelaaja1.Weapon = new PlasmaCannon(20, 0);
        pelaaja1.Weapon.Ammo.Value = 1000;
        pelaaja1.Weapon.ProjectileCollision = AmmusOsui;
        AddCollisionHandler(pelaaja1, "gemi", TormaaTimanttiin);
        AddCollisionHandler(pelaaja1, "lava", OsuuLaavaan);
        AddCollisionHandler(pelaaja1, "vihu", OsuuVihuun);
        Add(pelaaja1);
        
        
    }
    
    
    /// <summary>
    /// Näppäimet
    /// </summary>
    private void LisaaNappaimet() // Luodaan kontrollit pelaajahahmolle
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja1, -Nopeus);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja1, Nopeus);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja1, Hyppynopeus);
        Keyboard.Listen(Key.Space, ButtonState.Down, AmmuAseella, "Ammu", pelaaja1);
        
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }
    
    
    public void SeuraavaKentta() // Luo kentän
    {
        ClearAll();
        
        if(kenttaNro == 1) LuoKentta("kentta1");
        else if (kenttaNro == 2)
        {
            LuoKentta("kentta2");
            int kentanPisteet = LaskePisteet(pisteLaskuri.Value);
            MessageDisplay.Add("Tason 1 pisteesi olivat " + kentanPisteet);
        }
        else if (kenttaNro == 3)
        {
            LuoKentta("kentta3");
            int kentanPisteet = LaskePisteet(pisteLaskuri.Value);
            MessageDisplay.Add("Tason 2 pisteesi olivat " + kentanPisteet);
        }
        else
        {
            MessageDisplay.Add("Voitit pelin!!!");
            int loppuPisteet = LaskePisteet(pisteLaskuri.Value);
            MessageDisplay.Add("Loppupisteesi olivat " + loppuPisteet);
            
        }
        
        LuoPistelaskuri();
        LisaaNappaimet();
        Camera.Follow(pelaaja1);
    }
    
    
    /// <summary>
    /// Pelihahmon liikuttaminen
    /// </summary>
    /// <param name="hahmo">Hahmo</param>
    /// <param name="nopeus">Nopeus</param>
    private void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
    }
    
    
    /// <summary>
    /// Pelihahmon hyppääminen
    /// </summary>
    /// <param name="hahmo">Hahmo</param>
    /// <param name="nopeus">Nopeus</param>
    private void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
    }
    
    
    /// <summary>
    /// Pelihahmo koskettaa timanttia
    /// </summary>
    /// <param name="pelaaja">Pelaaja</param>
    /// <param name="gemi">Gemi</param>
    private void TormaaTimanttiin(PhysicsObject pelaaja, PhysicsObject gemi) // Vaihdetaan kenttää jos pelaaja saa timantin
    {
        if (kenttaNro == 3)
        {
            maaliAani.Play();
            MessageDisplay.Add("Löysit viimeisen timantin!");
            pisteLaskuri.Value += 500;
            LaskePisteet(pisteLaskuri.Value);
            kenttaNro++;
            ClearAll();
            MediaPlayer.Stop();
            SeuraavaKentta();
            
        }
        else
        {
            maaliAani.Play();
            MessageDisplay.Add("Löysit timantin!");
            pisteLaskuri.Value += 100;
            MessageDisplay.Add("Loppupisteesi olivat " + LaskePisteet(pisteLaskuri.Value));
            gemi.Destroy();
            kenttaNro++;
            PeliMusat(kenttaNro);
            MediaPlayer.Volume = 0.3;
            SeuraavaKentta();
        }
        
    }
    
    
    /// <summary>
    /// Pelihahmo koskettaa laavaa
    /// </summary>
    /// <param name="pelaaja">Pelaaja</param>
    /// <param name="lava">Lava</param>
    private void OsuuLaavaan(PhysicsObject pelaaja, PhysicsObject lava) // Tuhoa pelaaja, jos osuu laavaan
    {
        pelaaja.Destroy();
        MediaPlayer.Stop();
        Kuolema();
    }
    
    
    /// <summary>
    /// Pelihahmo koskettaa vihua
    /// </summary>
    /// <param name="pelaaja">Pelaaja</param>
    /// <param name="vihu">Vihu</param>
    private void OsuuVihuun(PhysicsObject pelaaja, PhysicsObject vihu)
    {
        pelaaja.Destroy();
        MediaPlayer.Stop();
        Kuolema();
    }
    
    /// <summary>
    /// Pelihahmolla ampuminen
    /// </summary>
    /// <param name="pelaaja">Pelaaja</param>
    public void AmmuAseella(PlatformCharacter pelaaja)
    {
        PhysicsObject ammus = pelaaja.Weapon.Shoot();
    }
    
    
    /// <summary>
    /// Ammuksen osumisen käsittely
    /// </summary>
    /// <param name="ammus">Ammus</param>
    /// <param name="kohde">Kohde</param>
    private void AmmusOsui(PhysicsObject ammus, PhysicsObject kohde)
    {
        ammus.Destroy();
        if ((string)kohde.Tag == "vihu")
        {
            kohde.Destroy();
            pisteLaskuri.Value += 10;
        }
    }
    
    
    public void Kuolema() // Kun pelaaja kuolee
    {
        ClearAll();
        MessageDisplay.Add("Hävisit pelin!");
        int loppuPisteet = LaskePisteet(pisteLaskuri.Value);
        MessageDisplay.Add("Loppupisteesi olivat " + loppuPisteet);
    }
    

    private void LuoPistelaskuri()  // Luodaan pistelaskuri ruudun oikeaan laitaan
    {
        pisteLaskuri = new IntMeter(0);               
      
        Label pisteNaytto = new Label(); 
        pisteNaytto.X = Screen.Left + 100;
        pisteNaytto.Y = Screen.Top - 100;
        pisteNaytto.TextColor = Color.Black;
        pisteNaytto.Color = Color.White;
        pisteNaytto.Title = "Points ";

        pisteNaytto.BindTo(pisteLaskuri);
        Add(pisteNaytto);
        
    }
    
    
    /// <summary>
    /// Peli pisteiden lasku
    /// </summary>
    /// <param name="pisteet">TasonPisteet</param>
    int LaskePisteet(int pisteet)
    {
        int loppuPisteet = 0;
        loppuPisteet += pisteet;
        
        return loppuPisteet;
    }
    
    
    /// <summary>
    /// Pelin musiikit
    /// </summary>
    /// <param name="kentta">Kentta</param>
    private void PeliMusat(int kentta)  // Tuodaan peliin musiikkia taulukon avulla. Musiikki muuttuu kentän numeron mukaan
    {
        string[] musat = new string[3];
        musat[0] = "musa";
        musat[1] = "musa2";
        musat[2] = "musa3";
        for (int i = 0; i < musat.Length; i++)
        {
            MediaPlayer.Play(musat[kentta-1]);
        }
        
    }
    

}



