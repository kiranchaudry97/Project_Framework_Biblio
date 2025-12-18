using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Biblio_Models.Data;
using Biblio_Models.Entiteiten;

var services = new ServiceCollection();

services.AddDbContext<BiblioDbContext>(opt => opt.UseInMemoryDatabase("BiblioTestDb"));

var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<BiblioDbContext>();

if (!db.Boeken.Any())
{
    var boek = new Boek { Titel = "Test Boek", Auteur = "Auteur", Isbn = "123" };
    db.Boeken.Add(boek);
    var lid = new Lid { Voornaam = "Jan", AchterNaam = "Jansen" };
    db.Leden.Add(lid);
    db.SaveChanges();

    var lenen = new Lenen { BoekId = boek.Id, LidId = lid.Id, StartDate = DateTime.Today.AddDays(-7), DueDate = DateTime.Today.AddDays(7), ReturnedAt = null };
    db.Leningens.Add(lenen);
    db.SaveChanges();
}

var activeLoansPerBook = db.Leningens.Where(l => l.ReturnedAt == null).GroupBy(l => l.BoekId).Select(g => new { BoekId = g.Key, Count = g.Count() }).ToList();
var violations = activeLoansPerBook.Where(x => x.Count > 1).ToList();

if (violations.Any())
{
    Console.WriteLine("Data validation failed: multiple active loans for same book:");
    foreach (var v in violations) Console.WriteLine($" BoekId={v.BoekId} Count={v.Count}");
}
else
{
    Console.WriteLine("Data validation passed: no multiple active loans detected.");
}

var sample = db.Leningens.Include(l => l.Boek).Include(l => l.Lid).FirstOrDefault();
if (sample != null)
{
    Console.WriteLine($"Sample loan: Id={sample.Id}, Boek={sample.Boek?.Titel}, Lid={sample.Lid?.Voornaam} {sample.Lid?.AchterNaam}, Start={sample.StartDate:d}, Due={sample.DueDate:d}, ReturnedAt={sample.ReturnedAt}");
}

Console.WriteLine("Done.");