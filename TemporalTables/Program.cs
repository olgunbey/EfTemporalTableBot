// See https://aka.ms/new-console-template for more information

using Microsoft.EntityFrameworkCore;

AppDbContext context = new();

Urun eklenecekurun = new Urun() { Barkod = 5, UrunAd = "E", Fiyat = 200 };

Dictionary<int, DateTime> dateTimes = new Dictionary<int, DateTime>();

Urun urunsorgu = await context.Uruns.FirstOrDefaultAsync(p => p.Barkod == eklenecekurun.Barkod);

if (eklenecekurun.Fiyat != urunsorgu.Fiyat) //burada historaltable'ye eski fiyatlı olan veri eklenir
{
    urunsorgu.Fiyat = eklenecekurun.Fiyat;
    await context.SaveChangesAsync();
    DateTime eklenenverihistoraltablesperiodend = context.Uruns.TemporalAll().Where(p => p.Barkod == eklenecekurun.Barkod).      //burada en son historytable'ye gönderilecek verinin periodEnd'ini yakaladık.
    OrderBy(p => EF.Property<DateTime>(p, "PeriodEnd")).
    Select(p => EF.Property<DateTime>(p, "PeriodEnd"))
    .First();


    dateTimes.Add(eklenecekurun.Barkod, eklenenverihistoraltablesperiodend);



    foreach (var item in dateTimes) //bütün eski ürünlerin fiyatlarıyla kıyaslar, hangisine göre fiyatı yükseldi, hangisine göre fiyatı düştü belli eder.
    {
        var eklenenveri = await context.Uruns.TemporalContainedIn(item.Value.AddHours(-1), DateTime.UtcNow).Where(p => p.Barkod == eklenecekurun.Barkod)
       .OrderBy(p => EF.Property<DateTime>(p, "PeriodEnd"))
       .Select(p => new
       {
           p.Barkod,
           p.Fiyat,
           PeriodEnd = EF.Property<DateTime>(p, "PeriodEnd")
       }

       ).ToListAsync();
        foreach (var items in eklenenveri)
        {
           
           if(context.Uruns.Where(p => p.Barkod==eklenecekurun.Barkod && p.Fiyat<items.Fiyat).Any())
            {
                Console.WriteLine(urunsorgu.Barkod+$" barkodlu ürünün yeni fiyatı eski {items.Fiyat} 'a göre düştü");
            }
           else
            {
                Console.WriteLine(urunsorgu.Barkod + $" barkodlu ürünün yeni fiyatı eski {items.Fiyat} 'a göre yükseldi");
            }
        }
      
    }


}
else
{
    context.Uruns.Add(eklenecekurun);
    await context.SaveChangesAsync();
}

Console.WriteLine("");
















//historal table'ye eklenecek verinin periodend'ini bulmamız lazım.
//var veriler = context.Uruns.TemporalAll().Select(p => new
//{
//    p.UrunAd,
//    p.Fiyat,
//    PeriodEnd = EF.Property<DateTime>(p, "PeriodEnd")
//}).ToList();

//foreach (var item in veriler)
//{
//    Console.WriteLine(item.UrunAd + " " + item.Fiyat + " " + item.PeriodEnd);
//}

public class Urun
{
    public int Barkod { get; set; }
    public string? UrunAd { get; set; }
    public decimal Fiyat { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<Urun> Uruns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Urun>().HasKey(x => x.Barkod);
        modelBuilder.Entity<Urun>().ToTable("Urunler", builder => builder.IsTemporal());
        modelBuilder.Entity<Urun>().Property(x => x.Barkod).ValueGeneratedNever();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Data Source=OLGUNPC\\SQLEXPRESS; Initial Catalog=UrunTemporalTable;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
    }
}